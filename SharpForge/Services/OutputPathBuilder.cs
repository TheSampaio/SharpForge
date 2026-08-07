using SharpForge.Models;

namespace SharpForge.Services;

/// <summary>
/// Builds deterministic output paths for upscaled images.
/// </summary>
internal static class OutputPathBuilder
{
    /// <summary>
    /// Creates an output path from a source path and the active batch settings.
    /// </summary>
    /// <param name="inputPath">The source image path.</param>
    /// <param name="options">The active upscaling settings.</param>
    /// <returns>The absolute path where the upscaled image will be written.</returns>
    /// <exception cref="ArgumentException">Thrown when the input path has no parent directory.</exception>
    public static string Build(string inputPath, UpscaleOptions options)
    {
        string fullInputPath = Path.GetFullPath(inputPath);
        string? sourceDirectory = Path.GetDirectoryName(fullInputPath);

        if (string.IsNullOrEmpty(sourceDirectory))
        {
            throw new ArgumentException("The input path must have a parent directory.", nameof(inputPath));
        }

        string baseName = Path.GetFileNameWithoutExtension(fullInputPath);
        string extension = options.OutputFormat.TrimStart('.').ToLowerInvariant();

        if (options.UseSuffix)
        {
            return Path.Combine(sourceDirectory, $"{baseName} ({options.ModelName}).{extension}");
        }

        string outputDirectory = Path.Combine(sourceDirectory, $"_Upscaled ({options.ModelName})");
        return Path.Combine(outputDirectory, $"{baseName}.{extension}");
    }
}
