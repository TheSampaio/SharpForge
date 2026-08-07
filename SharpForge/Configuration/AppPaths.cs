namespace SharpForge.Configuration;

/// <summary>
/// Stores the absolute paths required by the application at runtime.
/// </summary>
/// <param name="UpscalerExecutable">The Real-ESRGAN executable path.</param>
/// <param name="ModelsDirectory">The directory that contains Real-ESRGAN model files.</param>
internal sealed record AppPaths(string UpscalerExecutable, string ModelsDirectory)
{
    /// <summary>
    /// Creates paths relative to the application executable.
    /// </summary>
    /// <returns>The runtime paths used by Sharp Forge.</returns>
    public static AppPaths CreateDefault()
    {
        string upscalerDirectory = Path.Combine(AppContext.BaseDirectory, "realesrgan");

        return new AppPaths(
            Path.Combine(upscalerDirectory, "realesrgan-ncnn-vulkan.exe"),
            Path.Combine(upscalerDirectory, "models"));
    }
}
