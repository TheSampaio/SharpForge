using System.Diagnostics;
using System.Text;
using System.Text.Json;
using SharpForge.Configuration;
using SharpForge.Models;

namespace SharpForge.Services;

/// <summary>
/// Runs Spandrel-compatible models through a persistent PyTorch CUDA worker.
/// </summary>
internal sealed class PyTorchUpscaler : IDisposable
{
    private const int MaximumDiagnosticLength = 16384;
    private readonly AppPaths _paths;
    private readonly OptionalBackendInstaller _installer;
    private readonly SemaphoreSlim _workerLock = new(1, 1);
    private readonly StringBuilder _diagnostics = new();
    private Process? _worker;
    private string? _loadedModelPath;
    private int _loadedGpuId = -1;

    /// <summary>
    /// Initializes the optional PyTorch upscaler.
    /// </summary>
    /// <param name="paths">The application runtime paths.</param>
    /// <param name="installer">The optional backend installation state.</param>
    public PyTorchUpscaler(AppPaths paths, OptionalBackendInstaller installer)
    {
        _paths = paths;
        _installer = installer;
    }

    /// <summary>
    /// Gets a value indicating whether the optional backend is installed.
    /// </summary>
    public bool IsAvailable => _installer.IsInstalled;

    /// <summary>
    /// Upscales one image through the persistent CUDA worker.
    /// </summary>
    /// <param name="inputPath">The source image path.</param>
    /// <param name="outputPath">The destination image path.</param>
    /// <param name="options">The active upscaling settings.</param>
    /// <param name="cancellationToken">A token that requests process cancellation.</param>
    /// <returns>The completed, failed, or canceled job result.</returns>
    public async Task<UpscaleResult> UpscaleAsync(
        string inputPath,
        string outputPath,
        UpscaleOptions options,
        CancellationToken cancellationToken)
    {
        if (!IsAvailable)
        {
            return Failure(inputPath, outputPath, "The optional PyTorch CUDA backend is not installed.");
        }

        if (string.IsNullOrWhiteSpace(options.ModelPath) || !File.Exists(options.ModelPath))
        {
            return Failure(inputPath, outputPath, "The selected PyTorch model file is unavailable.");
        }

        bool requiresDdsConversion = string.Equals(options.OutputFormat, "dds", StringComparison.OrdinalIgnoreCase);
        string processOutputPath = requiresDdsConversion
            ? $"{outputPath}.{Guid.NewGuid():N}.png"
            : outputPath;

        try
        {
            await _workerLock.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return new UpscaleResult(inputPath, outputPath, UpscaleStatus.Canceled);
        }

        try
        {
            await EnsureWorkerAsync(options.ModelPath, options.GpuId, cancellationToken);
            Process worker = _worker
                ?? throw new InvalidOperationException("The PyTorch CUDA worker did not start.");
            string request = JsonSerializer.Serialize(new
            {
                input = inputPath,
                output = processOutputPath,
                tileSize = options.TileSize,
                format = requiresDdsConversion ? "png" : options.OutputFormat
            });

            await worker.StandardInput.WriteLineAsync(request.AsMemory(), cancellationToken);
            await worker.StandardInput.FlushAsync(cancellationToken);
            string? responseLine = await worker.StandardOutput.ReadLineAsync(cancellationToken);
            if (responseLine is null)
            {
                throw new InvalidOperationException(GetWorkerExitMessage());
            }

            using JsonDocument response = JsonDocument.Parse(responseLine);
            string status = response.RootElement.GetProperty("status").GetString() ?? string.Empty;
            if (!string.Equals(status, "completed", StringComparison.Ordinal))
            {
                string message = response.RootElement.TryGetProperty("message", out JsonElement messageElement)
                    ? messageElement.GetString() ?? "The PyTorch worker reported an unknown error."
                    : "The PyTorch worker reported an unknown error.";
                TryDeleteIncompleteOutput(processOutputPath);
                TryDeleteIncompleteOutput(outputPath);
                return Failure(inputPath, outputPath, message);
            }

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
                finally
                {
                    TryDeleteIncompleteOutput(processOutputPath);
                }
            }

            return new UpscaleResult(inputPath, outputPath, UpscaleStatus.Completed);
        }
        catch (OperationCanceledException)
        {
            StopWorker();
            TryDeleteIncompleteOutput(processOutputPath);
            TryDeleteIncompleteOutput(outputPath);
            return new UpscaleResult(inputPath, outputPath, UpscaleStatus.Canceled);
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or IOException or JsonException or
            System.ComponentModel.Win32Exception or System.Runtime.InteropServices.ExternalException)
        {
            StopWorker();
            TryDeleteIncompleteOutput(processOutputPath);
            TryDeleteIncompleteOutput(outputPath);
            return Failure(inputPath, outputPath, exception.Message);
        }
        finally
        {
            _workerLock.Release();
        }
    }

    /// <summary>
    /// Starts a worker when the requested model or GPU differs from the loaded state.
    /// </summary>
    /// <param name="modelPath">The checkpoint to load.</param>
    /// <param name="gpuId">The CUDA device index.</param>
    /// <param name="cancellationToken">A token that requests startup cancellation.</param>
    /// <returns>A task that completes after the worker reports readiness.</returns>
    private async Task EnsureWorkerAsync(
        string modelPath,
        int gpuId,
        CancellationToken cancellationToken)
    {
        if (_worker is { HasExited: false } &&
            string.Equals(_loadedModelPath, modelPath, StringComparison.OrdinalIgnoreCase) &&
            _loadedGpuId == gpuId)
        {
            return;
        }

        StopWorker();
        ProcessStartInfo startInfo = new()
        {
            FileName = _paths.OptionalBackendPythonExecutable,
            WorkingDirectory = _paths.OptionalBackendDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(_paths.PyTorchWorkerScript);
        startInfo.ArgumentList.Add("serve");
        startInfo.ArgumentList.Add(modelPath);
        startInfo.ArgumentList.Add("--gpu");
        startInfo.ArgumentList.Add(gpuId.ToString(System.Globalization.CultureInfo.InvariantCulture));

        _diagnostics.Clear();
        Process process = new() { StartInfo = startInfo };
        process.ErrorDataReceived += CaptureDiagnostic;
        if (!process.Start())
        {
            process.Dispose();
            throw new InvalidOperationException("The PyTorch CUDA worker could not be started.");
        }

        _worker = process;
        process.BeginErrorReadLine();
        string? readyLine = await process.StandardOutput.ReadLineAsync(cancellationToken);
        if (readyLine is null)
        {
            throw new InvalidOperationException(GetWorkerExitMessage());
        }

        using JsonDocument ready = JsonDocument.Parse(readyLine);
        string status = ready.RootElement.GetProperty("status").GetString() ?? string.Empty;
        if (!string.Equals(status, "ready", StringComparison.Ordinal))
        {
            string message = ready.RootElement.TryGetProperty("message", out JsonElement messageElement)
                ? messageElement.GetString() ?? "The PyTorch model could not be loaded."
                : "The PyTorch model could not be loaded.";
            throw new InvalidOperationException(message);
        }

        _loadedModelPath = modelPath;
        _loadedGpuId = gpuId;
    }

    /// <summary>
    /// Captures bounded worker diagnostics from standard error.
    /// </summary>
    /// <param name="sender">The worker process.</param>
    /// <param name="arguments">The received diagnostic line.</param>
    private void CaptureDiagnostic(object sender, DataReceivedEventArgs arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments.Data))
        {
            return;
        }

        lock (_diagnostics)
        {
            if (_diagnostics.Length < MaximumDiagnosticLength)
            {
                _diagnostics.AppendLine(arguments.Data);
            }
        }
    }

    /// <summary>
    /// Returns the most useful available worker termination message.
    /// </summary>
    /// <returns>A diagnostic message for a prematurely terminated worker.</returns>
    private string GetWorkerExitMessage()
    {
        lock (_diagnostics)
        {
            string details = _diagnostics.ToString().Trim();
            return string.IsNullOrWhiteSpace(details)
                ? "The PyTorch CUDA worker exited unexpectedly."
                : details;
        }
    }

    /// <summary>
    /// Stops and releases the persistent worker process.
    /// </summary>
    private void StopWorker()
    {
        Process? worker = _worker;
        _worker = null;
        _loadedModelPath = null;
        _loadedGpuId = -1;
        if (worker is null)
        {
            return;
        }

        try
        {
            if (!worker.HasExited)
            {
                worker.Kill(entireProcessTree: true);
                worker.WaitForExit(5000);
            }
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            // The worker exited between the state check and the kill request.
        }
        finally
        {
            worker.Dispose();
        }
    }

    /// <summary>
    /// Removes an output that may have been left incomplete by a failed worker.
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
            // Cleanup is best-effort; the worker error remains more useful to the user.
        }
    }

    /// <summary>
    /// Creates a failed result with a useful fallback message.
    /// </summary>
    /// <param name="inputPath">The source image path.</param>
    /// <param name="outputPath">The destination image path.</param>
    /// <param name="details">The worker error details.</param>
    /// <returns>A failed upscaling result.</returns>
    private static UpscaleResult Failure(string inputPath, string outputPath, string details)
    {
        string message = string.IsNullOrWhiteSpace(details)
            ? "The PyTorch CUDA worker exited with an unknown error."
            : details;
        return new UpscaleResult(inputPath, outputPath, UpscaleStatus.Failed, message);
    }

    /// <summary>
    /// Stops the worker and releases synchronization resources.
    /// </summary>
    public void Dispose()
    {
        StopWorker();
        _workerLock.Dispose();
        _installer.Dispose();
    }
}
