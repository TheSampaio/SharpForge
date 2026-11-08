using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using SharpForge.Configuration;

namespace SharpForge.Services;

/// <summary>
/// Encodes high-quality BC7 DDS textures and previews BC7 or legacy BGRA DDS files.
/// </summary>
internal static class DdsImageService
{
    private const uint DdsMagic = 0x20534444;
    private const uint HeaderSize = 124;
    private const uint PixelFormatSize = 32;
    private const uint PixelFormatFlags = 0x00000041;
    private const uint RedMask = 0x00FF0000;
    private const uint GreenMask = 0x0000FF00;
    private const uint BlueMask = 0x000000FF;
    private const uint AlphaMask = 0xFF000000;

    /// <summary>
    /// Converts an image to a maximum-quality BC7 DDS texture with alpha and a full mip chain.
    /// </summary>
    /// <param name="sourcePath">The source image path.</param>
    /// <param name="destinationPath">The destination DDS path.</param>
    /// <param name="gpuId">The preferred DirectCompute adapter index.</param>
    /// <param name="cancellationToken">A token that requests compression cancellation.</param>
    /// <returns>A task that completes after the compressed texture is installed atomically.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the DirectXTex compressor is unavailable.</exception>
    /// <exception cref="InvalidOperationException">Thrown when both GPU and CPU compression fail.</exception>
    /// <exception cref="IOException">Thrown when temporary or destination files cannot be accessed.</exception>
    public static async Task SaveAsync(
        string sourcePath,
        string destinationPath,
        int gpuId,
        CancellationToken cancellationToken)
    {
        string texconvPath = GetTexconvPath();
        string temporaryDirectory = CreateTemporaryDirectory();
        string temporaryDestinationPath = $"{destinationPath}.{Guid.NewGuid():N}.tmp";

        try
        {
            IReadOnlyCollection<string> commonArguments =
            [
                "-nologo",
                "-y",
                "-ft",
                "DDS",
                "-f",
                "BC7_UNORM",
                "-bc",
                "x",
                "-aw",
                "1.0",
                "-sepalpha",
                "-m",
                "0",
                "-o",
                temporaryDirectory,
                sourcePath
            ];

            try
            {
                await RunTexconvAsync(
                    texconvPath,
                    ["-gpu", gpuId.ToString(System.Globalization.CultureInfo.InvariantCulture), .. commonArguments],
                    cancellationToken);
            }
            catch (InvalidOperationException)
            {
                DeleteGeneratedFiles(temporaryDirectory);
                await RunTexconvAsync(
                    texconvPath,
                    ["-nogpu", .. commonArguments],
                    cancellationToken);
            }

            string generatedPath = FindGeneratedFile(temporaryDirectory, ".dds");
            File.Move(generatedPath, temporaryDestinationPath, overwrite: true);
            File.Move(temporaryDestinationPath, destinationPath, overwrite: true);
        }
        finally
        {
            TryDeleteFile(temporaryDestinationPath);
            TryDeleteDirectory(temporaryDirectory);
        }
    }

    /// <summary>
    /// Loads a BC7 DDS through DirectXTex or a legacy uncompressed BGRA DDS directly.
    /// </summary>
    /// <param name="sourcePath">The source DDS path.</param>
    /// <returns>A detached bitmap that the caller owns and must dispose.</returns>
    /// <exception cref="InvalidDataException">Thrown when the DDS layout cannot be decoded.</exception>
    /// <exception cref="IOException">Thrown when source or temporary files cannot be accessed.</exception>
    public static Bitmap Load(string sourcePath)
    {
        try
        {
            return LoadUncompressed(sourcePath);
        }
        catch (InvalidDataException)
        {
            return LoadCompressed(sourcePath);
        }
    }

    /// <summary>
    /// Converts a compressed DDS to a detached bitmap for application preview.
    /// </summary>
    /// <param name="sourcePath">The source compressed DDS path.</param>
    /// <returns>A detached decoded bitmap.</returns>
    private static Bitmap LoadCompressed(string sourcePath)
    {
        string texconvPath = GetTexconvPath();
        string temporaryDirectory = CreateTemporaryDirectory();

        try
        {
            RunTexconvAsync(
                texconvPath,
                ["-nologo", "-y", "-ft", "PNG", "-m", "1", "-o", temporaryDirectory, sourcePath],
                CancellationToken.None).GetAwaiter().GetResult();
            string generatedPath = FindGeneratedFile(temporaryDirectory, ".png");
            using FileStream stream = new(generatedPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using Image image = Image.FromStream(stream);
            return new Bitmap(image);
        }
        catch (Exception exception) when (
            exception is FileNotFoundException or InvalidOperationException or ArgumentException)
        {
            throw new InvalidDataException("The compressed DDS texture could not be decoded.", exception);
        }
        finally
        {
            TryDeleteDirectory(temporaryDirectory);
        }
    }

    /// <summary>
    /// Loads the legacy uncompressed 32-bit BGRA layout produced by earlier Sharp Forge versions.
    /// </summary>
    /// <param name="sourcePath">The source DDS path.</param>
    /// <returns>A detached decoded bitmap.</returns>
    private static Bitmap LoadUncompressed(string sourcePath)
    {
        using FileStream stream = new(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using BinaryReader reader = new(stream);

        uint magic = reader.ReadUInt32();
        uint headerSize = reader.ReadUInt32();
        _ = reader.ReadUInt32();
        int height = reader.ReadInt32();
        int width = reader.ReadInt32();
        int pitch = reader.ReadInt32();
        _ = reader.ReadUInt32();
        _ = reader.ReadUInt32();
        for (int index = 0; index < 11; index++)
        {
            _ = reader.ReadUInt32();
        }

        uint pixelFormatSize = reader.ReadUInt32();
        uint pixelFormatFlags = reader.ReadUInt32();
        uint fourCc = reader.ReadUInt32();
        uint bitCount = reader.ReadUInt32();
        uint redMask = reader.ReadUInt32();
        uint greenMask = reader.ReadUInt32();
        uint blueMask = reader.ReadUInt32();
        uint alphaMask = reader.ReadUInt32();
        _ = reader.ReadUInt32();
        _ = reader.ReadUInt32();
        _ = reader.ReadUInt32();
        _ = reader.ReadUInt32();
        _ = reader.ReadUInt32();

        bool valid = magic == DdsMagic &&
            headerSize == HeaderSize &&
            pixelFormatSize == PixelFormatSize &&
            (pixelFormatFlags & PixelFormatFlags) == PixelFormatFlags &&
            fourCc == 0 &&
            bitCount == 32 &&
            redMask == RedMask &&
            greenMask == GreenMask &&
            blueMask == BlueMask &&
            alphaMask == AlphaMask &&
            width > 0 &&
            height > 0 &&
            pitch >= width * 4;
        if (!valid)
        {
            throw new InvalidDataException("The DDS texture is not an uncompressed 32-bit BGRA image.");
        }

        Bitmap bitmap = new(width, height, PixelFormat.Format32bppArgb);
        try
        {
            ReadPixels(reader, bitmap, pitch);
            return bitmap;
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Reads DDS rows into a bitmap while honoring its native stride direction.
    /// </summary>
    /// <param name="reader">The source reader positioned at the pixel data.</param>
    /// <param name="bitmap">The destination bitmap.</param>
    /// <param name="sourcePitch">The byte length of each source row.</param>
    private static void ReadPixels(BinaryReader reader, Bitmap bitmap, int sourcePitch)
    {
        Rectangle bounds = new(0, 0, bitmap.Width, bitmap.Height);
        BitmapData data = bitmap.LockBits(bounds, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        int rowLength = bitmap.Width * 4;

        try
        {
            for (int y = 0; y < bitmap.Height; y++)
            {
                byte[] row = reader.ReadBytes(sourcePitch);
                if (row.Length != sourcePitch)
                {
                    throw new InvalidDataException("The DDS pixel data is incomplete.");
                }

                IntPtr rowPointer = GetRowPointer(data, y, bitmap.Height);
                Marshal.Copy(row, 0, rowPointer, rowLength);
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    /// <summary>
    /// Resolves a logical top-to-bottom row pointer for positive or negative bitmap strides.
    /// </summary>
    /// <param name="data">The locked bitmap data.</param>
    /// <param name="rowIndex">The logical row index.</param>
    /// <param name="height">The bitmap height.</param>
    /// <returns>The native pointer for the requested row.</returns>
    private static IntPtr GetRowPointer(BitmapData data, int rowIndex, int height)
    {
        int nativeRow = data.Stride >= 0 ? rowIndex : height - 1 - rowIndex;
        return IntPtr.Add(data.Scan0, nativeRow * Math.Abs(data.Stride));
    }

    /// <summary>
    /// Runs DirectXTex with captured diagnostics and process-tree cancellation.
    /// </summary>
    /// <param name="texconvPath">The converter executable path.</param>
    /// <param name="arguments">The individually escaped converter arguments.</param>
    /// <param name="cancellationToken">A token that requests process cancellation.</param>
    /// <returns>A task that completes after successful conversion.</returns>
    private static async Task RunTexconvAsync(
        string texconvPath,
        IEnumerable<string> arguments,
        CancellationToken cancellationToken)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = texconvPath,
            WorkingDirectory = Path.GetDirectoryName(texconvPath) ?? AppContext.BaseDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = new() { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException("DirectXTex texconv could not be started.");
        }

        Task<string> outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            }

            throw;
        }

        string output = await outputTask.ConfigureAwait(false);
        string error = await errorTask.ConfigureAwait(false);
        if (process.ExitCode != 0)
        {
            string details = string.IsNullOrWhiteSpace(error) ? output : error;
            string message = details
                .Split(["\r\n", "\n", "\r"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .LastOrDefault() ?? "DirectXTex reported an unknown compression error.";
            throw new InvalidOperationException(message);
        }
    }

    /// <summary>
    /// Returns the bundled converter path after verifying its availability.
    /// </summary>
    /// <returns>The absolute DirectXTex texconv path.</returns>
    private static string GetTexconvPath()
    {
        string texconvPath = AppPaths.CreateDefault().TexconvExecutable;
        if (!File.Exists(texconvPath))
        {
            throw new FileNotFoundException(
                "The DirectXTex DDS compressor is unavailable. Run Build\\Setup.bat and rebuild Sharp Forge.",
                texconvPath);
        }

        return texconvPath;
    }

    /// <summary>
    /// Creates a unique DDS conversion directory in the system temporary location.
    /// </summary>
    /// <returns>The created temporary directory path.</returns>
    private static string CreateTemporaryDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), $"SharpForge-DDS-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>
    /// Finds the single output generated by DirectXTex.
    /// </summary>
    /// <param name="directoryPath">The converter output directory.</param>
    /// <param name="extension">The required output extension.</param>
    /// <returns>The generated file path.</returns>
    private static string FindGeneratedFile(string directoryPath, string extension)
    {
        string[] files = Directory
            .EnumerateFiles(directoryPath, $"*{extension}", SearchOption.TopDirectoryOnly)
            .ToArray();
        return files.Length == 1
            ? files[0]
            : throw new InvalidDataException("DirectXTex did not produce exactly one expected output file.");
    }

    /// <summary>
    /// Removes files generated before a GPU compression fallback.
    /// </summary>
    /// <param name="directoryPath">The temporary conversion directory.</param>
    private static void DeleteGeneratedFiles(string directoryPath)
    {
        foreach (string filePath in Directory.EnumerateFiles(directoryPath, "*", SearchOption.TopDirectoryOnly))
        {
            File.Delete(filePath);
        }
    }

    /// <summary>
    /// Deletes a temporary file while preserving the original conversion result.
    /// </summary>
    /// <param name="path">The temporary file path.</param>
    private static void TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Cleanup is best-effort; the original conversion result is more useful.
        }
    }

    /// <summary>
    /// Deletes a temporary directory while preserving the original conversion result.
    /// </summary>
    /// <param name="path">The temporary directory path.</param>
    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Cleanup is best-effort; the original conversion result is more useful.
        }
    }
}
