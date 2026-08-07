using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace SharpForge.Controls;

/// <summary>
/// Defines the shared colors and geometry used by the Sharp Forge interface.
/// </summary>
internal static class UiTheme
{
    public static readonly Color Window = Color.FromArgb(32, 32, 32);
    public static readonly Color Surface = Color.FromArgb(37, 37, 37);
    public static readonly Color SurfaceElevated = Color.FromArgb(32, 32, 32);
    public static readonly Color SurfaceHover = Color.FromArgb(48, 48, 48);
    public static readonly Color Border = Color.FromArgb(59, 70, 88);
    public static readonly Color BorderSubtle = Color.FromArgb(48, 48, 48);
    public static readonly Color Primary = Color.FromArgb(42, 130, 246);
    public static readonly Color PrimaryHover = Color.FromArgb(64, 146, 255);
    public static readonly Color PrimaryMuted = Color.FromArgb(28, 65, 115);
    public static readonly Color Danger = Color.FromArgb(220, 70, 80);
    public static readonly Color DangerHover = Color.FromArgb(235, 86, 96);
    public static readonly Color Success = Color.FromArgb(42, 201, 111);
    public static readonly Color TextPrimary = Color.FromArgb(244, 247, 252);
    public static readonly Color TextSecondary = Color.FromArgb(166, 176, 194);
    public static readonly Color TextMuted = Color.FromArgb(112, 123, 143);
}

/// <summary>
/// Creates rounded paths shared by custom-drawn controls.
/// </summary>
internal static class UiGeometry
{
    /// <summary>
    /// Creates a rounded rectangle path bounded by the supplied rectangle.
    /// </summary>
    /// <param name="bounds">The rectangle enclosed by the path.</param>
    /// <param name="radius">The corner radius in pixels.</param>
    /// <returns>A graphics path that the caller must dispose.</returns>
    public static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
    {
        GraphicsPath path = new();
        int diameter = Math.Max(1, Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height)));
        Rectangle arc = new(bounds.Location, new Size(diameter, diameter));

        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();

        return path;
    }

    /// <summary>
    /// Clips a control to a genuinely rounded window region.
    /// </summary>
    /// <param name="control">The control whose native region should be updated.</param>
    /// <param name="radius">The corner radius in pixels.</param>
    public static void ApplyRoundedRegion(Control control, int radius)
    {
        if (control.Width <= 0 || control.Height <= 0)
        {
            return;
        }

        using GraphicsPath path = CreateRoundedRectangle(
            new Rectangle(0, 0, control.Width, control.Height),
            radius);
        Region? previousRegion = control.Region;
        control.Region = new Region(path);
        previousRegion?.Dispose();
    }
}

/// <summary>
/// Applies supported native dark-window attributes on Windows.
/// </summary>
internal static partial class NativeWindowTheme
{
    private const int ImmersiveDarkModeAttribute = 20;

    /// <summary>
    /// Requests a dark native title bar for a form.
    /// </summary>
    /// <param name="form">The form whose title bar should use dark colors.</param>
    public static void Apply(Form form)
    {
        int enabled = 1;
        _ = DwmSetWindowAttribute(
            form.Handle,
            ImmersiveDarkModeAttribute,
            ref enabled,
            Marshal.SizeOf<int>());
    }

    /// <summary>
    /// Requests the native dark Explorer theme for a child control.
    /// </summary>
    /// <param name="control">The control whose native chrome should use dark colors.</param>
    public static void ApplyToControl(Control control)
    {
        _ = SetWindowTheme(control.Handle, "DarkMode_Explorer", null);
    }

    /// <summary>
    /// Sets a Desktop Window Manager attribute for a native window.
    /// </summary>
    /// <param name="windowHandle">The native window handle.</param>
    /// <param name="attribute">The DWM attribute identifier.</param>
    /// <param name="value">The integer attribute value.</param>
    /// <param name="valueSize">The size of the attribute value.</param>
    /// <returns>Zero when the operation succeeds; otherwise, an HRESULT error code.</returns>
    [LibraryImport("dwmapi.dll")]
    private static partial int DwmSetWindowAttribute(
        IntPtr windowHandle,
        int attribute,
        ref int value,
        int valueSize);

    /// <summary>
    /// Sets the native visual style used to draw a child control.
    /// </summary>
    /// <param name="windowHandle">The native control handle.</param>
    /// <param name="subApplicationName">The visual-style application name.</param>
    /// <param name="subIdentifierList">The optional visual-style identifiers.</param>
    /// <returns>Zero when the operation succeeds; otherwise, an HRESULT error code.</returns>
    [LibraryImport("uxtheme.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int SetWindowTheme(
        IntPtr windowHandle,
        string? subApplicationName,
        string? subIdentifierList);
}
