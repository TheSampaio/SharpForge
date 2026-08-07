namespace SharpForge.Models;

/// <summary>
/// Defines the immutable settings used for one upscaling batch.
/// </summary>
/// <param name="ModelName">The Real-ESRGAN model name without its file extension.</param>
/// <param name="OutputFormat">The output image format without a leading dot.</param>
/// <param name="TileSize">The tile size processed by Real-ESRGAN.</param>
/// <param name="GpuId">The Vulkan GPU identifier.</param>
/// <param name="LoadThreads">The number of GPU input-loading threads.</param>
/// <param name="ProcessThreads">The number of GPU processing threads.</param>
/// <param name="SaveThreads">The number of GPU output-saving threads.</param>
/// <param name="MaxConcurrentJobs">The maximum number of Real-ESRGAN processes run concurrently.</param>
/// <param name="UseSuffix">Whether output files are saved beside their source with a model suffix.</param>
internal sealed record UpscaleOptions(
    string ModelName,
    string OutputFormat,
    int TileSize,
    int GpuId,
    int LoadThreads,
    int ProcessThreads,
    int SaveThreads,
    int MaxConcurrentJobs,
    bool UseSuffix);
