using SharpForge.Models;

namespace SharpForge.Services;

/// <summary>
/// Routes image jobs to the runtime required by the selected model.
/// </summary>
internal sealed class ImageUpscalerRouter : IImageUpscaler, IDisposable
{
    private readonly RealEsrganUpscaler _ncnnUpscaler;
    private readonly PyTorchUpscaler _pyTorchUpscaler;

    /// <summary>
    /// Initializes the available image-upscaling backends.
    /// </summary>
    /// <param name="ncnnUpscaler">The bundled NCNN Vulkan upscaler.</param>
    /// <param name="pyTorchUpscaler">The optional PyTorch CUDA upscaler.</param>
    public ImageUpscalerRouter(
        RealEsrganUpscaler ncnnUpscaler,
        PyTorchUpscaler pyTorchUpscaler)
    {
        _ncnnUpscaler = ncnnUpscaler;
        _pyTorchUpscaler = pyTorchUpscaler;
    }

    /// <inheritdoc/>
    public bool IsAvailable(UpscaleBackend backend)
    {
        return backend switch
        {
            UpscaleBackend.Ncnn => _ncnnUpscaler.IsAvailable,
            UpscaleBackend.PyTorchCuda => _pyTorchUpscaler.IsAvailable,
            _ => false
        };
    }

    /// <inheritdoc/>
    public string GetUnavailableMessage(UpscaleBackend backend)
    {
        return backend switch
        {
            UpscaleBackend.PyTorchCuda =>
                "The optional PyTorch CUDA backend is not installed. Reimport the model to install it.",
            _ => "Real-ESRGAN is not installed. Run Build\\Setup.bat before starting Sharp Forge."
        };
    }

    /// <inheritdoc/>
    public Task<UpscaleResult> UpscaleAsync(
        string inputPath,
        string outputPath,
        UpscaleOptions options,
        CancellationToken cancellationToken)
    {
        return options.Backend switch
        {
            UpscaleBackend.Ncnn => _ncnnUpscaler.UpscaleAsync(
                inputPath,
                outputPath,
                options,
                cancellationToken),
            UpscaleBackend.PyTorchCuda => _pyTorchUpscaler.UpscaleAsync(
                inputPath,
                outputPath,
                options,
                cancellationToken),
            _ => Task.FromResult(new UpscaleResult(
                inputPath,
                outputPath,
                UpscaleStatus.Failed,
                "The selected model uses an unknown inference backend."))
        };
    }

    /// <summary>
    /// Stops any persistent optional-backend worker.
    /// </summary>
    public void Dispose()
    {
        _pyTorchUpscaler.Dispose();
    }
}
