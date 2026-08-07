using System.Collections.Concurrent;
using System.Diagnostics;
using SharpForge.Models;

namespace SharpForge.Services;

/// <summary>
/// Coordinates bounded, parallel processing of an image batch.
/// </summary>
internal sealed class UpscaleBatchProcessor
{
    private readonly IImageUpscaler _upscaler;

    /// <summary>
    /// Initializes a batch processor.
    /// </summary>
    /// <param name="upscaler">The image upscaler used for individual jobs.</param>
    public UpscaleBatchProcessor(IImageUpscaler upscaler)
    {
        _upscaler = upscaler;
    }

    /// <summary>
    /// Processes all images with bounded concurrency and progress reporting.
    /// </summary>
    /// <param name="inputPaths">The source image paths.</param>
    /// <param name="options">The immutable settings for the entire batch.</param>
    /// <param name="progress">An optional progress reporter.</param>
    /// <param name="cancellationToken">A token that requests batch cancellation.</param>
    /// <returns>Results and elapsed time for jobs that reached a final state.</returns>
    public async Task<UpscaleBatchResult> ProcessAsync(
        IReadOnlyCollection<string> inputPaths,
        UpscaleOptions options,
        IProgress<UpscaleProgress>? progress,
        CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        ConcurrentBag<UpscaleResult> results = [];
        int processedCount = 0;

        ParallelOptions parallelOptions = new()
        {
            MaxDegreeOfParallelism = Math.Max(1, options.MaxConcurrentJobs),
            CancellationToken = cancellationToken
        };

        try
        {
            await Parallel.ForEachAsync(inputPaths, parallelOptions, async (inputPath, token) =>
            {
                UpscaleResult result = await ProcessImageAsync(inputPath, options, token);
                results.Add(result);

                int currentCount = Interlocked.Increment(ref processedCount);
                progress?.Report(new UpscaleProgress(
                    currentCount,
                    inputPaths.Count,
                    Path.GetFileName(inputPath),
                    result.Status));
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Completed and canceled jobs remain available in the batch result.
        }

        stopwatch.Stop();
        return new UpscaleBatchResult(results.ToArray(), stopwatch.Elapsed);
    }

    /// <summary>
    /// Processes one image, including output creation and existing-file detection.
    /// </summary>
    /// <param name="inputPath">The source image path.</param>
    /// <param name="options">The active upscaling settings.</param>
    /// <param name="cancellationToken">A token that requests job cancellation.</param>
    /// <returns>The result of processing the image.</returns>
    private async Task<UpscaleResult> ProcessImageAsync(
        string inputPath,
        UpscaleOptions options,
        CancellationToken cancellationToken)
    {
        string outputPath = OutputPathBuilder.Build(inputPath, options);

        if (File.Exists(outputPath))
        {
            return new UpscaleResult(inputPath, outputPath, UpscaleStatus.Skipped);
        }

        try
        {
            string? outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            return await _upscaler.UpscaleAsync(inputPath, outputPath, options, cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new UpscaleResult(
                inputPath,
                outputPath,
                UpscaleStatus.Failed,
                exception.Message);
        }
    }
}
