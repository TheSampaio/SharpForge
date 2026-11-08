using System.Diagnostics;
using System.Globalization;
using SharpForge.Configuration;
using SharpForge.Models;

namespace SharpForge.Services;

/// <summary>
/// Runs Real-ESRGAN through its NCNN Vulkan command-line executable.
/// </summary>
internal sealed class RealEsrganUpscaler
{
    private readonly AppPaths _paths;

    /// <summary>
    /// Initializes a Real-ESRGAN upscaler.
    /// </summary>
    /// <param name="paths">The application runtime paths.</param>
    public RealEsrganUpscaler(AppPaths paths)
    {
        _paths = paths;
    }

    /// <summary>
    /// Gets a value indicating whether the NCNN Vulkan executable is available.
    /// </summary>
    public bool IsAvailable =>
        File.Exists(_paths.UpscalerExecutable);

    /// <summary>
    /// Upscales one image with the NCNN Vulkan runtime.
    /// </summary>
    /// <param name="inputPath">The source image path.</param>
    /// <param name="outputPath">The destination image path.</param>
    /// <param name="options">The active upscaling settings.</param>
    /// <param name="cancellationToken">A token that requests process cancellation.</param>
    /// <returns>The result of the external upscaling process.</returns>
    public async Task<UpscaleResult> UpscaleAsync(
        string inputPath,
        string outputPath,
        UpscaleOptions options,
        CancellationToken cancellationToken)
    {
        bool requiresDdsConversion = string.Equals(options.OutputFormat, "dds", StringComparison.OrdinalIgnoreCase);
        string processOutputPath = requiresDdsConversion
            ? $"{outputPath}.{Guid.NewGuid():N}.png"
            : outputPath;
        ProcessStartInfo startInfo = CreateStartInfo(inputPath, processOutputPath, options, requiresDdsConversion ? "png" : options.OutputFormat);

        using Process process = new() { StartInfo = startInfo };

        try
        {
            if (!process.Start())
            {
                return Failure(inputPath, outputPath, "Real-ESRGAN could not be started.");
            }

            Task<string> standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> standardError = process.StandardError.ReadToEndAsync(cancellationToken);

            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                await StopProcessAsync(process);
                TryDeleteIncompleteOutput(processOutputPath);
                TryDeleteIncompleteOutput(outputPath);
                return new UpscaleResult(inputPath, outputPath, UpscaleStatus.Canceled);
            }

            string output = await standardOutput;
            string error = await standardError;

            if (process.ExitCode == 0)
            {
                if (requiresDdsConversion)
                {
                    try
                    {
                        await DdsImageService.SaveAsync(
                            processOutputPath,
                            outputPath,
                            options.GpuId,
                            cancellationToken);
                    }
                    catch (Exception exception) when (exception is ArgumentException or IOException or System.Runtime.InteropServices.ExternalException)
                    {
                        TryDeleteIncompleteOutput(outputPath);
                        return Failure(inputPath, outputPath, $"DDS conversion failed: {exception.Message}");
                    }
                    finally
                    {
                        TryDeleteIncompleteOutput(processOutputPath);
                    }
                }

                return new UpscaleResult(inputPath, outputPath, UpscaleStatus.Completed);
            }

            string details = string.IsNullOrWhiteSpace(error) ? output : error;
            TryDeleteIncompleteOutput(processOutputPath);
            TryDeleteIncompleteOutput(outputPath);
            return Failure(inputPath, outputPath, details.Trim());
        }
        catch (OperationCanceledException)
        {
            await StopProcessAsync(process);
            TryDeleteIncompleteOutput(processOutputPath);
            TryDeleteIncompleteOutput(outputPath);
            return new UpscaleResult(inputPath, outputPath, UpscaleStatus.Canceled);
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            TryDeleteIncompleteOutput(processOutputPath);
            TryDeleteIncompleteOutput(outputPath);
            return Failure(inputPath, outputPath, exception.Message);
        }
    }

    /// <summary>
    /// Creates a process configuration with individually escaped command-line arguments.
    /// </summary>
    /// <param name="inputPath">The source image path.</param>
    /// <param name="outputPath">The destination image path.</param>
    /// <param name="options">The active upscaling settings.</param>
    /// <param name="processOutputFormat">The image format written directly by the NCNN process.</param>
    /// <returns>A configured process start descriptor.</returns>
    private ProcessStartInfo CreateStartInfo(
        string inputPath,
        string outputPath,
        UpscaleOptions options,
        string processOutputFormat)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = _paths.UpscalerExecutable,
            WorkingDirectory = Path.GetDirectoryName(_paths.UpscalerExecutable) ?? AppContext.BaseDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        AddArgument(startInfo, "-i", inputPath);
        AddArgument(startInfo, "-o", outputPath);
        AddArgument(startInfo, "-m", options.ModelDirectory);
        AddArgument(startInfo, "-n", options.ModelName);
        AddArgument(startInfo, "-f", processOutputFormat);
        AddArgument(startInfo, "-t", options.TileSize.ToString(CultureInfo.InvariantCulture));
        AddArgument(startInfo, "-g", options.GpuId.ToString(CultureInfo.InvariantCulture));
        AddArgument(
            startInfo,
            "-j",
            FormattableString.Invariant($"{options.LoadThreads}:{options.ProcessThreads}:{options.SaveThreads}"));

        return startInfo;
    }

    /// <summary>
    /// Appends an option and value as separate process arguments.
    /// </summary>
    /// <param name="startInfo">The process descriptor to update.</param>
    /// <param name="option">The command-line option.</param>
    /// <param name="value">The option value.</param>
    private static void AddArgument(ProcessStartInfo startInfo, string option, string value)
    {
        startInfo.ArgumentList.Add(option);
        startInfo.ArgumentList.Add(value);
    }

    /// <summary>
    /// Attempts to stop the process and all child processes during cancellation.
    /// </summary>
    /// <param name="process">The process to stop.</param>
    private static async Task StopProcessAsync(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(CancellationToken.None);
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            // The process exited between the state check and the kill request.
        }
    }

    /// <summary>
    /// Removes an output file that may have been left incomplete by a failed process.
    /// </summary>
    /// <param name="outputPath">The incomplete output path.</param>
    private static void TryDeleteIncompleteOutput(string outputPath)
    {
        try
        {
            File.Delete(outputPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Cleanup is best-effort; the original process error remains more useful to the user.
        }
    }

    /// <summary>
    /// Creates a failed upscaling result with a useful fallback message.
    /// </summary>
    /// <param name="inputPath">The source image path.</param>
    /// <param name="outputPath">The destination image path.</param>
    /// <param name="details">The error details returned by the process.</param>
    /// <returns>A failed upscaling result.</returns>
    private static UpscaleResult Failure(string inputPath, string outputPath, string details)
    {
        string message = string.IsNullOrWhiteSpace(details)
            ? "Real-ESRGAN exited with an unknown error."
            : details;

        return new UpscaleResult(inputPath, outputPath, UpscaleStatus.Failed, message);
    }
}
