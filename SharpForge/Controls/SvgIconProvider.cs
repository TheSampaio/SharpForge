using System.Reflection;
using System.Text;
using Svg;

namespace SharpForge.Controls;

/// <summary>
/// Loads embedded SVG assets and rasterizes them for WinForms controls.
/// </summary>
internal static class SvgIconProvider
{
    private const string ResourcePrefix = "SharpForge.Resources.Icons.Svg";

    /// <summary>
    /// Creates a bitmap from an embedded SVG icon using the requested color.
    /// </summary>
    /// <param name="iconName">The SVG file name without its extension.</param>
    /// <param name="size">The square raster size in device-independent pixels.</param>
    /// <param name="color">The color used to replace the SVG currentColor value.</param>
    /// <returns>A bitmap owned by the caller.</returns>
    public static Bitmap Create(string iconName, int size, Color color)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(iconName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);

        Assembly assembly = typeof(SvgIconProvider).Assembly;
        string resourceName = $"{ResourcePrefix}.{iconName}.svg";
        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"The SVG icon '{iconName}' was not found.");
        using StreamReader reader = new(stream, Encoding.UTF8);
        string svg = reader.ReadToEnd().Replace(
            "currentColor",
            $"#{color.R:X2}{color.G:X2}{color.B:X2}",
            StringComparison.Ordinal);
        using MemoryStream svgStream = new(Encoding.UTF8.GetBytes(svg));
        SvgDocument document = SvgDocument.Open<SvgDocument>(svgStream);
        return document.Draw(size, size);
    }
}
