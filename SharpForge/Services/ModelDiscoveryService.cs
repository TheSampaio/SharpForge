using SharpForge.Configuration;

namespace SharpForge.Services;

/// <summary>
/// Discovers complete Real-ESRGAN model pairs from the configured models directory.
/// </summary>
internal sealed class ModelDiscoveryService
{
    private const string DefaultModelName = "realesrgan-x4plus";
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
    /// <returns>Available model names ordered alphabetically.</returns>
    public IReadOnlyCollection<string> GetAvailableModels()
    {
        if (!Directory.Exists(_paths.ModelsDirectory))
        {
            return [DefaultModelName];
        }

        string[] modelNames = Directory
            .EnumerateFiles(_paths.ModelsDirectory, "*.param", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name is not null && File.Exists(Path.Combine(_paths.ModelsDirectory, $"{name}.bin")))
            .Cast<string>()
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return modelNames.Length > 0 ? modelNames : [DefaultModelName];
    }
}
