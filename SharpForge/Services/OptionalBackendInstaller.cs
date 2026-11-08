using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using SharpForge.Configuration;

namespace SharpForge.Services;

/// <summary>
/// Downloads and prepares the optional per-user PyTorch CUDA inference backend.
/// </summary>
internal sealed class OptionalBackendInstaller : IDisposable
{
    private const string UvDownloadUrl = "https://github.com/astral-sh/uv/releases/download/0.11.27/uv-x86_64-pc-windows-msvc.zip";
    private const string UvSha256 = "b7e32288ce0e289dbe94d2cac7adbb008f74f0e038542a2d9969dd50eb7056ee";
    private const string PythonVersion = "3.11.13";
    private const string RuntimeMarkerName = ".complete";
    private static readonly HttpClient DownloadClient = CreateDownloadClient();
    private readonly SemaphoreSlim _installLock = new(1, 1);
    private readonly AppPaths _paths;

    /// <summary>
    /// Initializes the optional backend installer.
    /// </summary>
    /// <param name="paths">The application runtime and per-user data paths.</param>
    public OptionalBackendInstaller(AppPaths paths)
    {
        _paths = paths;
    }

    /// <summary>
    /// Gets a value indicating whether the complete pinned backend is available.
    /// </summary>
    public bool IsInstalled =>
        File.Exists(Path.Combine(_paths.OptionalBackendDirectory, RuntimeMarkerName)) &&
        File.Exists(_paths.OptionalBackendPythonExecutable) &&
        File.Exists(_paths.PyTorchWorkerScript);

    /// <summary>
    /// Downloads Python, PyTorch CUDA, and Spandrel into the current user's application data.
    /// </summary>
    /// <param name="progress">An optional reporter for installation stage descriptions.</param>
    /// <param name="cancellationToken">A token that requests installation cancellation.</param>
    /// <returns>A task that completes when the backend passes its self-check.</returns>
    /// <exception cref="InvalidDataException">Thrown when a downloaded archive fails integrity validation.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a setup process fails or the backend self-check is unsuccessful.</exception>
    /// <exception cref="HttpRequestException">Thrown when a required download fails.</exception>
    public async Task InstallAsync(
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        await _installLock.WaitAsync(cancellationToken);
        try
        {
            if (IsInstalled)
            {
                return;
            }

            ResetIncompleteRuntime();
            Directory.CreateDirectory(_paths.OptionalBackendDirectory);

            string archivePath = Path.Combine(_paths.OptionalBackendDirectory, "uv.zip");
            string toolsDirectory = Path.Combine(_paths.OptionalBackendDirectory, "tools");
            string pythonDirectory = Path.Combine(_paths.OptionalBackendDirectory, "python");
            string cacheDirectory = Path.Combine(_paths.OptionalBackendDirectory, "cache");
            string environmentDirectory = Path.Combine(_paths.OptionalBackendDirectory, "venv");

            progress?.Report("Downloading the optional CUDA backend installer...");
            await DownloadFileAsync(UvDownloadUrl, archivePath, progress, cancellationToken);
            ValidateSha256(archivePath, UvSha256);

            progress?.Report("Preparing the isolated Python environment...");
            Directory.CreateDirectory(toolsDirectory);
            ZipFile.ExtractToDirectory(archivePath, toolsDirectory, overwriteFiles: true);
            string uvPath = Directory
                .EnumerateFiles(toolsDirectory, "uv.exe", SearchOption.AllDirectories)
                .FirstOrDefault()
                ?? throw new InvalidDataException("The verified uv archive does not contain uv.exe.");

            Dictionary<string, string> environment = new(StringComparer.OrdinalIgnoreCase)
            {
                ["UV_PYTHON_INSTALL_DIR"] = pythonDirectory,
                ["UV_CACHE_DIR"] = cacheDirectory,
                ["UV_NO_PROGRESS"] = "1"
            };

            await RunProcessAsync(
                uvPath,
                _paths.OptionalBackendDirectory,
                ["venv", environmentDirectory, "--python", PythonVersion, "--python-preference", "only-managed"],
                environment,
                cancellationToken);

            progress?.Report("Downloading PyTorch CUDA and Spandrel. This can take several minutes...");
            await RunProcessAsync(
                uvPath,
                _paths.OptionalBackendDirectory,
                [
                    "pip",
                    "install",
                    "--python",
                    _paths.OptionalBackendPythonExecutable,
                    "--index",
                    "https://download.pytorch.org/whl/cu128",
                    "--default-index",
                    "https://pypi.org/simple",
                    "torch==2.7.0",
                    "torchvision==0.22.0",
                    "spandrel==0.4.2",
                    "Pillow==11.3.0"
                ],
                environment,
                cancellationToken);

            progress?.Report("Validating CUDA access and the Spandrel runtime...");
            await RunProcessAsync(
                _paths.OptionalBackendPythonExecutable,
                _paths.OptionalBackendDirectory,
                [_paths.PyTorchWorkerScript, "check"],
                environment,
                cancellationToken);

            await File.WriteAllTextAsync(
                Path.Combine(_paths.OptionalBackendDirectory, RuntimeMarkerName),
                "PyTorch 2.7.0+cu128; torchvision 0.22.0; Spandrel 0.4.2; Python 3.11.13",
                cancellationToken);

            TryDeleteFile(archivePath);
            TryDeleteDirectory(cacheDirectory);
        }
        catch
        {
            ResetIncompleteRuntime();
            throw;
        }
        finally
        {
            _installLock.Release();
        }
    }

    /// <summary>
    /// Creates the shared HTTP client used for verified backend downloads.
    /// </summary>
    /// <returns>A client configured with a product user agent and long timeout.</returns>
    private static HttpClient CreateDownloadClient()
    {
        HttpClient client = new()
        {
            Timeout = TimeSpan.FromMinutes(30)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("SharpForge/1.5");
        return client;
    }

    /// <summary>
    /// Downloads one file while reporting coarse percentage updates when its length is known.
    /// </summary>
    /// <param name="sourceUrl">The HTTPS source URL.</param>
    /// <param name="destinationPath">The destination file path.</param>
    /// <param name="progress">The optional progress reporter.</param>
    /// <param name="cancellationToken">A token that requests download cancellation.</param>
    /// <returns>A task that completes when the destination file is closed.</returns>
    private static async Task DownloadFileAsync(
        string sourceUrl,
        string destinationPath,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await DownloadClient.GetAsync(
            sourceUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        long? totalLength = response.Content.Headers.ContentLength;
        await using Stream source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using FileStream destination = new(
            destinationPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);
        byte[] buffer = new byte[81920];
        long downloaded = 0;
        int lastPercentage = -10;

        while (true)
        {
            int bytesRead = await source.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0)
            {
                break;
            }

            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            downloaded += bytesRead;
            if (totalLength is > 0)
            {
                int percentage = (int)(downloaded * 100 / totalLength.Value);
                if (percentage >= lastPercentage + 10)
                {
                    lastPercentage = percentage;
                    progress?.Report($"Downloading the optional CUDA backend installer... {percentage}%");
                }
            }
        }
    }

    /// <summary>
    /// Validates a downloaded file against a pinned SHA-256 digest.
    /// </summary>
    /// <param name="path">The downloaded file path.</param>
    /// <param name="expectedHash">The expected lower-case hexadecimal digest.</param>
    /// <exception cref="InvalidDataException">Thrown when the digest does not match.</exception>
    private static void ValidateSha256(string path, string expectedHash)
    {
        using FileStream stream = File.OpenRead(path);
        string actualHash = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        if (!string.Equals(actualHash, expectedHash, StringComparison.Ordinal))
        {
            throw new InvalidDataException("The optional backend installer failed its SHA-256 integrity check.");
        }
    }

    /// <summary>
    /// Runs one backend setup process with cancellation and captured diagnostics.
    /// </summary>
    /// <param name="executablePath">The executable path.</param>
    /// <param name="workingDirectory">The process working directory.</param>
    /// <param name="arguments">The individually escaped arguments.</param>
    /// <param name="environment">The process-specific environment variables.</param>
    /// <param name="cancellationToken">A token that requests process cancellation.</param>
    /// <returns>A task that completes when the process exits successfully.</returns>
    private static async Task RunProcessAsync(
        string executablePath,
        string workingDirectory,
        IEnumerable<string> arguments,
        IReadOnlyDictionary<string, string> environment,
        CancellationToken cancellationToken)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = executablePath,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        foreach ((string name, string value) in environment)
        {
            startInfo.Environment[name] = value;
        }

        using Process process = new() { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException($"Could not start {Path.GetFileName(executablePath)}.");
        }

        Task<string> outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(CancellationToken.None);
            }

            throw;
        }

        string output = await outputTask;
        string error = await errorTask;
        if (process.ExitCode != 0)
        {
            string details = string.IsNullOrWhiteSpace(error) ? output : error;
            string lastLine = details
                .Split(["\r\n", "\n", "\r"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .LastOrDefault() ?? "Unknown setup error.";
            throw new InvalidOperationException(lastLine);
        }
    }

    /// <summary>
    /// Removes an incomplete runtime version owned by Sharp Forge.
    /// </summary>
    private void ResetIncompleteRuntime()
    {
        if (Directory.Exists(_paths.OptionalBackendDirectory) && !IsInstalled)
        {
            Directory.Delete(_paths.OptionalBackendDirectory, recursive: true);
        }
    }

    /// <summary>
    /// Deletes a temporary file while preserving the successful installation result.
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
            // Cache cleanup is best-effort after a successful installation.
        }
    }

    /// <summary>
    /// Deletes a temporary directory while preserving the successful installation result.
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
            // Cache cleanup is best-effort after a successful installation.
        }
    }

    /// <summary>
    /// Releases the installer synchronization resource.
    /// </summary>
    public void Dispose()
    {
        _installLock.Dispose();
    }
}
