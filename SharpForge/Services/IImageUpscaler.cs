using SharpForge.Models;

namespace SharpForge.Services;

/// <summary>
/// Defines the external image-upscaling boundary used by the application.
/// </summary>
internal interface IImageUpscaler
{
    /// <summary>
    /// Gets a value indicating whether the upscaler executable and model directory are available.
    /// </summary>
    public bool IsAvailable { get; }

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
