namespace SharpForge.Models;

/// <summary>
/// Describes an imported checkpoint that runs directly through the optional PyTorch backend.
/// </summary>
/// <param name="SchemaVersion">The metadata schema version.</param>
/// <param name="Name">The user-facing model name.</param>
/// <param name="ModelFileName">The checkpoint file name relative to the metadata file.</param>
/// <param name="Architecture">The architecture detected by Spandrel.</param>
/// <param name="Scale">The integer image scale produced by the model.</param>
/// <param name="InputChannels">The number of model input channels.</param>
/// <param name="OutputChannels">The number of model output channels.</param>
internal sealed record ImportedModelMetadata(
    int SchemaVersion,
    string Name,
    string ModelFileName,
    string Architecture,
    int Scale,
    int InputChannels,
    int OutputChannels);
