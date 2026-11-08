namespace SharpForge.Configuration;

/// <summary>
/// Stores the absolute paths required by the application at runtime.
/// </summary>
/// <param name="UpscalerExecutable">The Real-ESRGAN executable path.</param>
/// <param name="BuiltInModelsDirectory">The directory that contains bundled Real-ESRGAN model files.</param>
/// <param name="ImportedModelsDirectory">The writable directory that contains user-imported model files.</param>
/// <param name="ModelConverterDirectory">The directory that contains the bundled PyTorch-to-NCNN converter.</param>
/// <param name="ModelNormalizerScript">The script that normalizes supported ESRGAN checkpoints before conversion.</param>
/// <param name="ModelInspectorScript">The script that identifies supported PyTorch model architectures.</param>
/// <param name="OptionalBackendDirectory">The per-user directory that contains the optional PyTorch CUDA runtime.</param>
/// <param name="OptionalBackendPythonExecutable">The optional backend Python executable path.</param>
/// <param name="PyTorchWorkerScript">The bundled persistent PyTorch inference worker script.</param>
/// <param name="TexconvExecutable">The bundled DirectXTex texture converter executable.</param>
internal sealed record AppPaths(
    string UpscalerExecutable,
    string BuiltInModelsDirectory,
    string ImportedModelsDirectory,
    string ModelConverterDirectory,
    string ModelNormalizerScript,
    string ModelInspectorScript,
    string OptionalBackendDirectory,
    string OptionalBackendPythonExecutable,
    string PyTorchWorkerScript,
    string TexconvExecutable)
{
    /// <summary>
    /// Creates paths relative to the application executable.
    /// </summary>
    /// <returns>The runtime paths used by Sharp Forge.</returns>
    public static AppPaths CreateDefault()
    {
        string upscalerDirectory = Path.Combine(AppContext.BaseDirectory, "realesrgan");
        string applicationDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SharpForge");
        string optionalBackendDirectory = Path.Combine(
            applicationDataDirectory,
            "PyTorchRuntime",
            "1.0.0");

        return new AppPaths(
            Path.Combine(upscalerDirectory, "realesrgan-ncnn-vulkan.exe"),
            Path.Combine(upscalerDirectory, "models"),
            Path.Combine(applicationDataDirectory, "models"),
            Path.Combine(upscalerDirectory, "tools", "pth2ncnn"),
            Path.Combine(upscalerDirectory, "tools", "pth2ncnn", "normalize_esrgan.py"),
            Path.Combine(upscalerDirectory, "tools", "pth2ncnn", "inspect_model.py"),
            optionalBackendDirectory,
            Path.Combine(optionalBackendDirectory, "venv", "Scripts", "python.exe"),
            Path.Combine(AppContext.BaseDirectory, "pytorch", "sharpforge_worker.py"),
            Path.Combine(AppContext.BaseDirectory, "texconv", "texconv.exe"));
    }
}
