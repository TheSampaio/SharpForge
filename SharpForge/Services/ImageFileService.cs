namespace SharpForge.Services;

/// <summary>
/// Finds supported source images from files and directories selected by the user.
/// </summary>
internal static class ImageFileService
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".bmp",
        ".gif",
        ".jpeg",
        ".jpg",
        ".png",
        ".tif",
        ".tiff",
        ".webp"
    };

    /// <summary>
    /// Determines whether a path has a supported image extension.
    /// </summary>
    /// <param name="path">The file path to inspect.</param>
    /// <returns><see langword="true"/> when the extension is supported; otherwise, <see langword="false"/>.</returns>
    public static bool IsSupported(string path)
    {
        return File.Exists(path) && SupportedExtensions.Contains(Path.GetExtension(path));
    }

    /// <summary>
    /// Returns unique image files from the supplied file and directory paths.
    /// </summary>
    /// <param name="paths">The files or directories selected by the user.</param>
    /// <returns>A case-insensitive, de-duplicated collection of absolute image paths.</returns>
    public static IReadOnlyCollection<string> FindImages(IEnumerable<string> paths)
    {
        HashSet<string> images = new(StringComparer.OrdinalIgnoreCase);

        foreach (string path in paths)
        {
            if (IsSupported(path))
            {
                images.Add(Path.GetFullPath(path));
                continue;
            }

            if (!Directory.Exists(path))
            {
                continue;
            }

            foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly))
            {
                if (SupportedExtensions.Contains(Path.GetExtension(file)))
                {
                    images.Add(Path.GetFullPath(file));
                }
            }
        }

        return images;
    }
}
