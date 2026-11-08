namespace SharpForge.Models;

/// <summary>
/// Identifies the inference engine required by an upscaling model.
/// </summary>
internal enum UpscaleBackend
{
    Ncnn,
    PyTorchCuda
}

/// <summary>
/// Identifies a validated upscaling model and its inference backend.
/// </summary>
/// <param name="Name">The model name displayed by the application.</param>
/// <param name="DirectoryPath">The directory that contains the model files.</param>
/// <param name="Backend">The inference backend required by the model.</param>
/// <param name="ModelPath">The checkpoint path for direct-inference backends, or an empty string for NCNN.</param>
internal sealed record UpscaleModel(
    string Name,
    string DirectoryPath,
    UpscaleBackend Backend,
    string ModelPath)
{
    /// <summary>
    /// Returns the model name displayed by selection controls.
    /// </summary>
    /// <returns>The model name.</returns>
    public override string ToString()
    {
        return Name;
    }
}
