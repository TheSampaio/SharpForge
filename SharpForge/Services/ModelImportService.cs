using System.Diagnostics;
using System.Text.Json;
using SharpForge.Configuration;
using SharpForge.Models;

namespace SharpForge.Services;

/// <summary>
/// Imports validated NCNN pairs and PyTorch checkpoints for their compatible inference backend.
/// </summary>
internal sealed class ModelImportService
{
    private const string ConverterExecutableName = "pth2ncnn.exe";
    private const string ConverterPythonRelativePath = @"Tools\python.exe";
    private const string ConverterCacheVersion = "1.39-sharpforge-1";
    private const string ProcessErrorMarker = "SHARPFORGE_ERROR:";
    private const string ArchitectureMarker = "SHARPFORGE_ARCHITECTURE:";
    private static readonly JsonSerializerOptions MetadataJsonOptions = new() { WriteIndented = true };
    private readonly AppPaths _paths;
    private readonly OptionalBackendInstaller _optionalBackendInstaller;

    /// <summary>
    /// Initializes a model import service.
    /// </summary>
    /// <param name="paths">The application runtime and user-data paths.</param>
    /// <param name="optionalBackendInstaller">The optional CUDA backend installer.</param>
    public ModelImportService(
        AppPaths paths,
        OptionalBackendInstaller optionalBackendInstaller)
    {
        _paths = paths;
        _optionalBackendInstaller = optionalBackendInstaller;
    }

    /// <summary>
    /// Imports a model on a background thread and reports major conversion stages.
    /// </summary>
    /// <param name="sourcePath">A PyTorch checkpoint or one file from an NCNN model pair.</param>
    /// <param name="progress">An optional progress reporter for user-facing stage descriptions.</param>
    /// <param name="installOptionalBackend">Whether the user authorized downloading the optional CUDA backend.</param>
    /// <param name="cancellationToken">A token that requests cancellation.</param>
    /// <returns>The imported and usable model.</returns>
    /// <exception cref="ArgumentException">Thrown when the selected file type or model architecture is unsupported.</exception>
    /// <exception cref="FileNotFoundException">Thrown when a selected file or its required companion file is missing.</exception>
    /// <exception cref="InvalidDataException">Thrown when conversion does not produce a usable NCNN model pair.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the bundled converter is unavailable or exits with an error.</exception>
    public Task<UpscaleModel> ImportAsync(
        string sourcePath,
        IProgress<string>? progress,
        bool installOptionalBackend,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => ImportCoreAsync(sourcePath, progress, installOptionalBackend, cancellationToken),
            cancellationToken);
    }

    /// <summary>
    /// Selects the appropriate import workflow for the source file extension.
    /// </summary>
    /// <param name="sourcePath">The selected source model path.</param>
    /// <param name="progress">The optional progress reporter.</param>
    /// <param name="installOptionalBackend">Whether the user authorized the optional backend installation.</param>
    /// <param name="cancellationToken">A token that requests cancellation.</param>
    /// <returns>The imported model.</returns>
    private async Task<UpscaleModel> ImportCoreAsync(
        string sourcePath,
        IProgress<string>? progress,
        bool installOptionalBackend,
        CancellationToken cancellationToken)
    {
        string fullSourcePath = Path.GetFullPath(sourcePath);
        if (!File.Exists(fullSourcePath))
        {
            throw new FileNotFoundException("The selected model file does not exist.", fullSourcePath);
        }

        string extension = Path.GetExtension(fullSourcePath).ToLowerInvariant();
        return extension switch
        {
            ".pth" => await ImportPytorchModelAsync(
                fullSourcePath,
                progress,
                installOptionalBackend,
                cancellationToken),
            ".safetensors" => await ImportDirectModelAsync(
                fullSourcePath,
                progress,
                installOptionalBackend,
                cancellationToken),
            ".param" or ".bin" => ImportNcnnModel(fullSourcePath, progress),
            _ => throw new ArgumentException(
                "Select a supported .pth/.safetensors checkpoint or a matching .param/.bin NCNN model pair.",
                nameof(sourcePath))
        };
    }

    /// <summary>
    /// Selects NCNN conversion or direct CUDA inference from an inspected PyTorch checkpoint.
    /// </summary>
    /// <param name="sourcePath">The trusted PyTorch checkpoint path.</param>
    /// <param name="progress">The optional progress reporter.</param>
    /// <param name="installOptionalBackend">Whether the user authorized the optional backend installation.</param>
    /// <param name="cancellationToken">A token that requests cancellation.</param>
    /// <returns>The imported model.</returns>
    private async Task<UpscaleModel> ImportPytorchModelAsync(
        string sourcePath,
        IProgress<string>? progress,
        bool installOptionalBackend,
        CancellationToken cancellationToken)
    {
        progress?.Report("Inspecting the model architecture...");
        string converterDirectory = EnsureWritableConverter(progress);
        string pythonPath = Path.Combine(converterDirectory, ConverterPythonRelativePath);
        string inspectorOutput = await RunProcessCaptureAsync(
            pythonPath,
            converterDirectory,
            [_paths.ModelInspectorScript, sourcePath],
            cancellationToken);
        string architecture = GetArchitecture(inspectorOutput);

        if (string.Equals(architecture, "ESRGAN", StringComparison.Ordinal) ||
            string.Equals(architecture, "RRDB", StringComparison.Ordinal))
        {
            return await ConvertPytorchModelAsync(sourcePath, progress, cancellationToken);
        }

        return await ImportDirectModelAsync(
            sourcePath,
            progress,
            installOptionalBackend,
            cancellationToken,
            architecture);
    }

    /// <summary>
    /// Validates a checkpoint with Spandrel and installs it for direct CUDA inference.
    /// </summary>
    /// <param name="sourcePath">The model checkpoint path.</param>
    /// <param name="progress">The optional progress reporter.</param>
    /// <param name="installOptionalBackend">Whether the user authorized the optional backend installation.</param>
    /// <param name="cancellationToken">A token that requests cancellation.</param>
    /// <param name="inspectedArchitecture">The architecture reported by the lightweight inspector, when available.</param>
    /// <returns>The validated and installed direct-inference model.</returns>
    /// <exception cref="OptionalBackendRequiredException">Thrown when validation requires an uninstalled optional backend.</exception>
    private async Task<UpscaleModel> ImportDirectModelAsync(
        string sourcePath,
        IProgress<string>? progress,
        bool installOptionalBackend,
        CancellationToken cancellationToken,
        string? inspectedArchitecture = null)
    {
        if (!_optionalBackendInstaller.IsInstalled)
        {
            if (!installOptionalBackend)
            {
                throw new OptionalBackendRequiredException(inspectedArchitecture ?? "Spandrel");
            }

            await _optionalBackendInstaller.InstallAsync(progress, cancellationToken);
        }

        progress?.Report("Validating the model with the optional CUDA backend...");
        string validationOutput = await RunProcessCaptureAsync(
            _paths.OptionalBackendPythonExecutable,
            _paths.OptionalBackendDirectory,
            [_paths.PyTorchWorkerScript, "validate", sourcePath],
            cancellationToken);
        ImportedModelMetadata metadata = ParseDirectModelMetadata(
            validationOutput,
            Path.GetFileName(sourcePath),
            GetSafeModelName(Path.GetFileNameWithoutExtension(sourcePath)));

        progress?.Report("Installing the validated CUDA model...");
        return InstallDirectModel(sourcePath, metadata);
    }

    /// <summary>
    /// Copies a validated NCNN model pair into the user model directory.
    /// </summary>
    /// <param name="selectedPath">Either file from the selected model pair.</param>
    /// <param name="progress">The optional progress reporter.</param>
    /// <returns>The imported model.</returns>
    private UpscaleModel ImportNcnnModel(string selectedPath, IProgress<string>? progress)
    {
        string sourceDirectory = Path.GetDirectoryName(selectedPath)
            ?? throw new ArgumentException("The selected model must have a parent directory.", nameof(selectedPath));
        string modelName = GetSafeModelName(Path.GetFileNameWithoutExtension(selectedPath));
        string parameterPath = Path.Combine(sourceDirectory, $"{modelName}.param");
        string weightsPath = Path.Combine(sourceDirectory, $"{modelName}.bin");

        if (!File.Exists(parameterPath) || !File.Exists(weightsPath))
        {
            throw new FileNotFoundException(
                $"Both {modelName}.param and {modelName}.bin must be stored in the same directory.");
        }

        if (!ModelDiscoveryService.IsUsableModelPair(parameterPath, weightsPath))
        {
            throw new InvalidDataException("The selected files are not a usable NCNN model pair.");
        }

        progress?.Report("Installing the validated NCNN model...");
        return InstallModelPair(modelName, parameterPath, weightsPath);
    }

    /// <summary>
    /// Normalizes a supported PyTorch checkpoint and converts it to NCNN.
    /// </summary>
    /// <param name="sourcePath">The PyTorch checkpoint path.</param>
    /// <param name="progress">The optional progress reporter.</param>
    /// <param name="cancellationToken">A token that requests cancellation.</param>
    /// <returns>The imported NCNN model.</returns>
    private async Task<UpscaleModel> ConvertPytorchModelAsync(
        string sourcePath,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        string converterDirectory = EnsureWritableConverter(progress);
        string modelName = GetSafeModelName(Path.GetFileNameWithoutExtension(sourcePath));
        string operationId = Guid.NewGuid().ToString("N");
        string normalizedBaseName = $"sharpforge-{operationId}";
        string normalizedPath = Path.Combine(converterDirectory, $"{normalizedBaseName}.pth");
        string converterOutputDirectory = Path.Combine(converterDirectory, normalizedBaseName);

        try
        {
            progress?.Report("Inspecting the model architecture...");
            string pythonPath = Path.Combine(converterDirectory, ConverterPythonRelativePath);
            await RunProcessAsync(
                pythonPath,
                converterDirectory,
                [_paths.ModelNormalizerScript, sourcePath, normalizedPath],
                cancellationToken);

            progress?.Report("Converting the model to NCNN. This can take a minute...");
            string converterPath = Path.Combine(converterDirectory, ConverterExecutableName);
            await RunProcessAsync(
                converterPath,
                converterDirectory,
                [normalizedPath],
                cancellationToken);

            string parameterPath = Path.Combine(converterOutputDirectory, "x4.param");
            string weightsPath = Path.Combine(converterOutputDirectory, "x4.bin");
            if (!ModelDiscoveryService.IsUsableModelPair(parameterPath, weightsPath))
            {
                throw new InvalidDataException("The converter did not produce a usable NCNN model pair.");
            }

            progress?.Report("Installing the converted model...");
            return InstallModelPair(modelName, parameterPath, weightsPath);
        }
        finally
        {
            TryDeleteFile(normalizedPath);
            TryDeleteDirectory(converterOutputDirectory);
        }
    }

    /// <summary>
    /// Copies the read-only bundled converter to a writable per-user cache when required.
    /// </summary>
    /// <param name="progress">The optional progress reporter.</param>
    /// <returns>The writable converter directory.</returns>
    private string EnsureWritableConverter(IProgress<string>? progress)
    {
        string bundledExecutable = Path.Combine(_paths.ModelConverterDirectory, ConverterExecutableName);
        string bundledPython = Path.Combine(_paths.ModelConverterDirectory, ConverterPythonRelativePath);
        if (!File.Exists(bundledExecutable) ||
            !File.Exists(bundledPython) ||
            !File.Exists(_paths.ModelNormalizerScript) ||
            !File.Exists(_paths.ModelInspectorScript))
        {
            throw new InvalidOperationException(
                "The PyTorch model converter is not installed. Run Build\\Setup.bat and rebuild Sharp Forge.");
        }

        string localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string cacheDirectory = Path.Combine(
            localApplicationData,
            "SharpForge",
            "ModelConverter",
            ConverterCacheVersion);
        string markerPath = Path.Combine(cacheDirectory, ".complete");
        string cachedExecutable = Path.Combine(cacheDirectory, ConverterExecutableName);
        string cachedPython = Path.Combine(cacheDirectory, ConverterPythonRelativePath);
        string converterTemporaryDirectory = Path.Combine(cacheDirectory, "Tools", "Temp");

        if (File.Exists(markerPath) && File.Exists(cachedExecutable) && File.Exists(cachedPython))
        {
            Directory.CreateDirectory(converterTemporaryDirectory);
            return cacheDirectory;
        }

        progress?.Report("Preparing the model converter for first use...");
        Directory.CreateDirectory(cacheDirectory);
        CopyDirectory(_paths.ModelConverterDirectory, cacheDirectory);
        Directory.CreateDirectory(converterTemporaryDirectory);
        File.WriteAllText(markerPath, ConverterCacheVersion);
        return cacheDirectory;
    }

    /// <summary>
    /// Installs a model pair with temporary files so discovery never sees a partial model.
    /// </summary>
    /// <param name="modelName">The destination model name.</param>
    /// <param name="parameterPath">The source parameter file.</param>
    /// <param name="weightsPath">The source weights file.</param>
    /// <returns>The installed model.</returns>
    private UpscaleModel InstallModelPair(string modelName, string parameterPath, string weightsPath)
    {
        Directory.CreateDirectory(_paths.ImportedModelsDirectory);
        string destinationParameterPath = Path.Combine(_paths.ImportedModelsDirectory, $"{modelName}.param");
        string destinationWeightsPath = Path.Combine(_paths.ImportedModelsDirectory, $"{modelName}.bin");
        string temporaryParameterPath = $"{destinationParameterPath}.{Guid.NewGuid():N}.tmp";
        string temporaryWeightsPath = $"{destinationWeightsPath}.{Guid.NewGuid():N}.tmp";

        try
        {
            File.Copy(parameterPath, temporaryParameterPath, overwrite: true);
            File.Copy(weightsPath, temporaryWeightsPath, overwrite: true);
            File.Move(temporaryWeightsPath, destinationWeightsPath, overwrite: true);
            File.Move(temporaryParameterPath, destinationParameterPath, overwrite: true);
        }
        finally
        {
            TryDeleteFile(temporaryParameterPath);
            TryDeleteFile(temporaryWeightsPath);
        }

        if (!ModelDiscoveryService.IsUsableModelPair(destinationParameterPath, destinationWeightsPath))
        {
            throw new InvalidDataException("The installed NCNN model pair failed validation.");
        }

        return new UpscaleModel(
            modelName,
            _paths.ImportedModelsDirectory,
            UpscaleBackend.Ncnn,
            string.Empty);
    }

    /// <summary>
    /// Installs a direct-inference checkpoint and its validated metadata atomically.
    /// </summary>
    /// <param name="sourcePath">The validated source checkpoint.</param>
    /// <param name="metadata">The metadata returned by Spandrel validation.</param>
    /// <returns>The installed CUDA model.</returns>
    private UpscaleModel InstallDirectModel(
        string sourcePath,
        ImportedModelMetadata metadata)
    {
        Directory.CreateDirectory(_paths.ImportedModelsDirectory);
        string destinationModelPath = Path.Combine(
            _paths.ImportedModelsDirectory,
            metadata.ModelFileName);
        string metadataPath = Path.Combine(
            _paths.ImportedModelsDirectory,
            $"{metadata.Name}.sharpforge.json");
        string temporaryModelPath = $"{destinationModelPath}.{Guid.NewGuid():N}.tmp";
        string temporaryMetadataPath = $"{metadataPath}.{Guid.NewGuid():N}.tmp";

        try
        {
            File.Copy(sourcePath, temporaryModelPath, overwrite: true);
            File.WriteAllText(
                temporaryMetadataPath,
                JsonSerializer.Serialize(metadata, MetadataJsonOptions));
            File.Move(temporaryModelPath, destinationModelPath, overwrite: true);
            File.Move(temporaryMetadataPath, metadataPath, overwrite: true);
        }
        finally
        {
            TryDeleteFile(temporaryModelPath);
            TryDeleteFile(temporaryMetadataPath);
        }

        return new UpscaleModel(
            metadata.Name,
            _paths.ImportedModelsDirectory,
            UpscaleBackend.PyTorchCuda,
            destinationModelPath);
    }

    /// <summary>
    /// Parses and validates the JSON response produced by the Spandrel worker.
    /// </summary>
    /// <param name="processOutput">The worker standard output.</param>
    /// <param name="sourceFileName">The original checkpoint file name.</param>
    /// <param name="modelName">The safe destination model name.</param>
    /// <returns>Validated persistent metadata for model discovery.</returns>
    private static ImportedModelMetadata ParseDirectModelMetadata(
        string processOutput,
        string sourceFileName,
        string modelName)
    {
        string jsonLine = processOutput
            .Split(["\r\n", "\n", "\r"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .LastOrDefault()
            ?? throw new InvalidDataException("The optional backend returned no model metadata.");
        using JsonDocument document = JsonDocument.Parse(jsonLine);
        JsonElement root = document.RootElement;
        string status = root.GetProperty("status").GetString() ?? string.Empty;
        if (!string.Equals(status, "ok", StringComparison.Ordinal))
        {
            string message = root.TryGetProperty("message", out JsonElement messageElement)
                ? messageElement.GetString() ?? "The checkpoint is not supported by Spandrel."
                : "The checkpoint is not supported by Spandrel.";
            throw new InvalidDataException(message);
        }

        string architecture = root.GetProperty("architecture").GetString()
            ?? throw new InvalidDataException("The model architecture was not reported.");
        int scale = root.GetProperty("scale").GetInt32();
        int inputChannels = root.GetProperty("inputChannels").GetInt32();
        int outputChannels = root.GetProperty("outputChannels").GetInt32();
        string extension = Path.GetExtension(sourceFileName).ToLowerInvariant();
        string installedFileName = $"{modelName}{extension}";
        return new ImportedModelMetadata(
            1,
            modelName,
            installedFileName,
            architecture,
            scale,
            inputChannels,
            outputChannels);
    }

    /// <summary>
    /// Extracts the architecture marker printed by the lightweight checkpoint inspector.
    /// </summary>
    /// <param name="output">The inspector standard output.</param>
    /// <returns>The reported architecture identifier.</returns>
    private static string GetArchitecture(string output)
    {
        string? architecture = output
            .Split(["\r\n", "\n", "\r"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.StartsWith(ArchitectureMarker, StringComparison.Ordinal)
                ? line[ArchitectureMarker.Length..].Trim()
                : null)
            .LastOrDefault(value => !string.IsNullOrWhiteSpace(value));
        return architecture ?? "UNKNOWN";
    }

    /// <summary>
    /// Runs a converter process and turns non-zero exit codes into actionable errors.
    /// </summary>
    /// <param name="executablePath">The executable to start.</param>
    /// <param name="workingDirectory">The process working directory.</param>
    /// <param name="arguments">The individual command-line arguments.</param>
    /// <param name="cancellationToken">A token that requests cancellation.</param>
    /// <returns>A task that completes after the process exits successfully.</returns>
    private static async Task RunProcessAsync(
        string executablePath,
        string workingDirectory,
        IEnumerable<string> arguments,
        CancellationToken cancellationToken)
    {
        _ = await RunProcessCaptureAsync(
            executablePath,
            workingDirectory,
            arguments,
            cancellationToken);
    }

    /// <summary>
    /// Runs a model utility process and returns its standard output.
    /// </summary>
    /// <param name="executablePath">The executable to start.</param>
    /// <param name="workingDirectory">The process working directory.</param>
    /// <param name="arguments">The individual command-line arguments.</param>
    /// <param name="cancellationToken">A token that requests cancellation.</param>
    /// <returns>The complete standard output text.</returns>
    private static async Task<string> RunProcessCaptureAsync(
        string executablePath,
        string workingDirectory,
        IEnumerable<string> arguments,
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

        using Process process = new() { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException($"Could not start {Path.GetFileName(executablePath)}.");
        }

        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> standardError = process.StandardError.ReadToEndAsync(cancellationToken);

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

        string output = await standardOutput;
        string error = await standardError;
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(GetProcessErrorMessage(error, output));
        }

        return output;
    }

    /// <summary>
    /// Extracts a concise actionable message from converter process output.
    /// </summary>
    /// <param name="standardError">The captured standard error text.</param>
    /// <param name="standardOutput">The captured standard output text.</param>
    /// <returns>The tagged error or the last meaningful output line.</returns>
    private static string GetProcessErrorMessage(string standardError, string standardOutput)
    {
        string combined = string.Join(Environment.NewLine, standardError, standardOutput);
        string[] lines = combined.Split(
            ["\r\n", "\n", "\r"],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        string? taggedError = lines
            .Select(line =>
            {
                int markerIndex = line.IndexOf(ProcessErrorMarker, StringComparison.Ordinal);
                return markerIndex >= 0
                    ? line[(markerIndex + ProcessErrorMarker.Length)..].Trim()
                    : null;
            })
            .LastOrDefault(message => !string.IsNullOrWhiteSpace(message));
        if (!string.IsNullOrWhiteSpace(taggedError))
        {
            return taggedError;
        }

        string? lastLine = lines.LastOrDefault();
        if (string.IsNullOrWhiteSpace(lastLine) || lastLine.StartsWith("Traceback", StringComparison.OrdinalIgnoreCase))
        {
            return "The model converter exited without an actionable error message.";
        }

        foreach (string prefix in new[] { "ValueError:", "RuntimeError:", "Exception:" })
        {
            if (lastLine.StartsWith(prefix, StringComparison.Ordinal))
            {
                return lastLine[prefix.Length..].Trim();
            }
        }

        return lastLine;
    }

    /// <summary>
    /// Recursively copies a converter directory into a writable cache.
    /// </summary>
    /// <param name="sourceDirectory">The source directory.</param>
    /// <param name="destinationDirectory">The destination directory.</param>
    private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
    {
        foreach (string directory in Directory.EnumerateDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(sourceDirectory, directory);
            Directory.CreateDirectory(Path.Combine(destinationDirectory, relativePath));
        }

        foreach (string file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(sourceDirectory, file);
            string destinationPath = Path.Combine(destinationDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            File.Copy(file, destinationPath, overwrite: true);
        }
    }

    /// <summary>
    /// Produces a file-system-safe NCNN model name from a selected file name.
    /// </summary>
    /// <param name="candidate">The proposed model name.</param>
    /// <returns>A safe, non-empty model name.</returns>
    private static string GetSafeModelName(string candidate)
    {
        char[] invalidCharacters = Path.GetInvalidFileNameChars();
        string safeName = new(candidate
            .Select(character => invalidCharacters.Contains(character) ? '_' : character)
            .ToArray());
        safeName = safeName.Trim().TrimEnd('.');

        if (string.IsNullOrWhiteSpace(safeName))
        {
            throw new ArgumentException("The selected file does not have a usable model name.", nameof(candidate));
        }

        return safeName;
    }

    /// <summary>
    /// Deletes a temporary file when it exists and ignores cleanup-only errors.
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
            // A cleanup failure must not hide the conversion result or its original error.
        }
    }

    /// <summary>
    /// Deletes a temporary directory when it exists and ignores cleanup-only errors.
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
            // A cleanup failure must not hide the conversion result or its original error.
        }
    }
}
