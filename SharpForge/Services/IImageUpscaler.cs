using SharpForge.Models;

namespace SharpForge.Services;

/// <summary>
/// Defines the external image-upscaling boundary used by the application.
/// </summary>
internal interface IImageUpscaler
{
    /// <summary>
    /// Determines whether the backend required by a model is available.
    /// </summary>
    /// <param name="backend">The inference backend to inspect.</param>
    /// <returns><see langword="true"/> when the required runtime is installed.</returns>
    public bool IsAvailable(UpscaleBackend backend);

    /// <summary>
    /// Returns an actionable message when a backend is unavailable.
    /// </summary>
    /// <param name="backend">The unavailable inference backend.</param>
    /// <returns>A user-facing runtime availability message.</returns>
    public string GetUnavailableMessage(UpscaleBackend backend);

    /// <summary>
    /// Upscales one image asynchronously.
    /// </summary>
    /// <param name="inputPath">The source image path.</param>
    /// <param name="outputPath">The destination image path.</param>
    /// <param name="options">The active upscaling settings.</param>
    /// <param name="cancellationToken">A token that requests process cancellation.</param>
    /// <returns>The result of the external upscaling process.</returns>
    public Task<UpscaleResult> UpscaleAsync(
        string inputPath,
        string outputPath,
        UpscaleOptions options,
        CancellationToken cancellationToken);
}
