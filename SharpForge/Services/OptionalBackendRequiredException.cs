namespace SharpForge.Services;

/// <summary>
/// Indicates that importing a checkpoint requires the optional CUDA inference backend.
/// </summary>
internal sealed class OptionalBackendRequiredException : InvalidOperationException
{
    /// <summary>
    /// Initializes an exception for a model architecture that requires the optional backend.
    /// </summary>
    /// <param name="architecture">The detected or expected model architecture.</param>
    public OptionalBackendRequiredException(string architecture)
        : base($"The {architecture} model requires the optional PyTorch CUDA backend.")
    {
        Architecture = architecture;
    }

    /// <summary>
    /// Gets the architecture that requires the optional backend.
    /// </summary>
    public string Architecture { get; }
}
