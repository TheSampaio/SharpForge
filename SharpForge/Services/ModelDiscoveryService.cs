using System.Text.Json;
using SharpForge.Configuration;
using SharpForge.Models;

namespace SharpForge.Services;

/// <summary>
/// Discovers complete Real-ESRGAN model pairs from the configured models directory.
/// </summary>
internal sealed class ModelDiscoveryService
{
    private readonly AppPaths _paths;

    /// <summary>
    /// Initializes a model discovery service.
    /// </summary>
    /// <param name="paths">The application runtime paths.</param>
    public ModelDiscoveryService(AppPaths paths)
    {
        _paths = paths;
    }

    /// <summary>
    /// Finds models that contain both a parameter file and a binary weights file.
    /// </summary>
    /// <returns>Available models ordered alphabetically.</returns>
    public IReadOnlyCollection<UpscaleModel> GetAvailableModels()
    {
        Dictionary<string, UpscaleModel> models = new(StringComparer.OrdinalIgnoreCase);

        AddDirectModels(models);
        AddModelsFromDirectory(models, _paths.ImportedModelsDirectory);
        AddModelsFromDirectory(models, _paths.BuiltInModelsDirectory);

        return models.Values
            .OrderBy(model => model.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Adds validated direct-inference checkpoints when their optional backend is installed.
    /// </summary>
    /// <param name="models">The destination model index.</param>
    private void AddDirectModels(Dictionary<string, UpscaleModel> models)
    {
        string markerPath = Path.Combine(_paths.OptionalBackendDirectory, ".complete");
        if (!Directory.Exists(_paths.ImportedModelsDirectory) ||
            !File.Exists(markerPath) ||
            !File.Exists(_paths.OptionalBackendPythonExecutable) ||
            !File.Exists(_paths.PyTorchWorkerScript))
        {
            return;
        }

        foreach (string metadataPath in Directory.EnumerateFiles(
            _paths.ImportedModelsDirectory,
            "*.sharpforge.json",
            SearchOption.TopDirectoryOnly))
        {
            try
            {
                ImportedModelMetadata? metadata = JsonSerializer.Deserialize<ImportedModelMetadata>(
                    File.ReadAllText(metadataPath));
                if (metadata is null ||
                    metadata.SchemaVersion != 1 ||
                    string.IsNullOrWhiteSpace(metadata.Name) ||
                    string.IsNullOrWhiteSpace(metadata.ModelFileName) ||
                    string.IsNullOrWhiteSpace(metadata.Architecture) ||
                    metadata.Scale < 1 ||
                    metadata.InputChannels != 3 ||
                    metadata.OutputChannels != 3)
                {
                    continue;
                }

                string modelPath = Path.GetFullPath(
                    Path.Combine(_paths.ImportedModelsDirectory, metadata.ModelFileName));
                string expectedDirectory = Path.GetFullPath(_paths.ImportedModelsDirectory)
                    .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                FileInfo modelFile = new(modelPath);
                if (!modelPath.StartsWith(expectedDirectory, StringComparison.OrdinalIgnoreCase) ||
                    !modelFile.Exists ||
                    modelFile.Length == 0)
                {
                    continue;
                }

                models.TryAdd(
                    metadata.Name,
                    new UpscaleModel(
                        metadata.Name,
                        _paths.ImportedModelsDirectory,
                        UpscaleBackend.PyTorchCuda,
                        modelPath));
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
            {
                // Invalid or stale metadata must never create a model-list placeholder.
            }
        }
    }

    /// <summary>
    /// Determines whether a parameter file and matching weight file form a usable NCNN model pair.
    /// </summary>
    /// <param name="parameterPath">The NCNN parameter file path.</param>
    /// <param name="weightsPath">The NCNN binary weights file path.</param>
    /// <returns><see langword="true"/> when both files exist and the parameter graph has the required input and output blobs.</returns>
    public static bool IsUsableModelPair(string parameterPath, string weightsPath)
    {
        FileInfo parameterFile = new(parameterPath);
        FileInfo weightsFile = new(weightsPath);

        if (!parameterFile.Exists || parameterFile.Length == 0 || !weightsFile.Exists || weightsFile.Length == 0)
        {
            return false;
        }

        try
        {
            using StreamReader reader = new(parameterPath);
            string? magic = reader.ReadLine()?.Trim();
            _ = reader.ReadLine();
            string graph = reader.ReadToEnd();

            return string.Equals(magic, "7767517", StringComparison.Ordinal) &&
                graph.Contains(" data", StringComparison.Ordinal) &&
                graph.Contains(" output", StringComparison.Ordinal);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>
    /// Adds every usable model pair found in one directory without replacing an existing name.
    /// </summary>
    /// <param name="models">The destination model index.</param>
    /// <param name="directoryPath">The directory to inspect.</param>
    private static void AddModelsFromDirectory(
        Dictionary<string, UpscaleModel> models,
        string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            return;
        }

        foreach (string parameterPath in Directory.EnumerateFiles(directoryPath, "*.param", SearchOption.TopDirectoryOnly))
        {
            string modelName = Path.GetFileNameWithoutExtension(parameterPath);
            string weightsPath = Path.Combine(directoryPath, $"{modelName}.bin");

            if (!models.ContainsKey(modelName) && IsUsableModelPair(parameterPath, weightsPath))
            {
                models.Add(
                    modelName,
                    new UpscaleModel(
                        modelName,
                        directoryPath,
                        UpscaleBackend.Ncnn,
                        string.Empty));
            }
        }
    }
}
