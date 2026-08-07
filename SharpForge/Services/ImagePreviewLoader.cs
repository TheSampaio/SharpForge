namespace SharpForge.Services;

/// <summary>
/// Loads image previews without keeping source files locked.
/// </summary>
internal static class ImagePreviewLoader
{
    /// <summary>
    /// Creates a detached bitmap from an image file.
    /// </summary>
    /// <param name="filePath">The source image path.</param>
    /// <returns>A bitmap that the caller owns and must dispose.</returns>
    /// <exception cref="ArgumentException">Thrown when the file does not contain a valid image.</exception>
    public static Bitmap Load(string filePath)
    {
        using FileStream stream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using Image source = Image.FromStream(stream);

        return new Bitmap(source);
    }
}
