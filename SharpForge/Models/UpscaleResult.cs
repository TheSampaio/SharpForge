namespace SharpForge.Models;

/// <summary>
/// Describes the final state of an upscaling job.
/// </summary>
internal enum UpscaleStatus
{
    Completed,
    Skipped,
    Failed,
    Canceled
}

/// <summary>
/// Contains the outcome of processing one image.
/// </summary>
/// <param name="InputPath">The source image path.</param>
/// <param name="OutputPath">The intended output image path.</param>
/// <param name="Status">The final processing status.</param>
/// <param name="ErrorMessage">The failure details, when available.</param>
internal sealed record UpscaleResult(
    string InputPath,
    string OutputPath,
    UpscaleStatus Status,
    string? ErrorMessage = null);

/// <summary>
/// Reports aggregate progress for an upscaling batch.
/// </summary>
/// <param name="ProcessedCount">The number of jobs that reached a final state.</param>
/// <param name="TotalCount">The total number of jobs in the batch.</param>
/// <param name="CurrentFileName">The file name associated with the latest update.</param>
/// <param name="Status">The final status of the latest job.</param>
internal sealed record UpscaleProgress(
    int ProcessedCount,
    int TotalCount,
    string CurrentFileName,
    UpscaleStatus Status);

/// <summary>
/// Contains all results and timing information for a completed batch operation.
/// </summary>
/// <param name="Results">The results produced before completion or cancellation.</param>
/// <param name="Elapsed">The total elapsed processing time.</param>
internal sealed record UpscaleBatchResult(
    IReadOnlyCollection<UpscaleResult> Results,
    TimeSpan Elapsed)
{
    /// <summary>
    /// Gets the number of successfully processed images.
    /// </summary>
    public int CompletedCount => Results.Count(result => result.Status == UpscaleStatus.Completed);

    /// <summary>
    /// Gets the number of images skipped because their output already existed.
    /// </summary>
    public int SkippedCount => Results.Count(result => result.Status == UpscaleStatus.Skipped);

    /// <summary>
    /// Gets the number of images that failed to process.
    /// </summary>
    public int FailedCount => Results.Count(result => result.Status == UpscaleStatus.Failed);
}
