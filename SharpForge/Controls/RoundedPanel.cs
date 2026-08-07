using System.Drawing.Drawing2D;

namespace SharpForge.Controls;

/// <summary>
/// Displays a double-buffered panel with a rounded border.
/// </summary>
internal sealed class RoundedPanel : Panel
{
    private int _borderRadius = 10;

    /// <summary>
    /// Initializes a rounded panel with the default application card style.
    /// </summary>
    public RoundedPanel()
    {
        DoubleBuffered = true;
        BackColor = UiTheme.Surface;
        BorderColor = UiTheme.Border;
        BorderThickness = 1;
        ResizeRedraw = true;
    }

    /// <summary>
    /// Gets or sets the border color.
    /// </summary>
    public Color BorderColor { get; set; }

    /// <summary>
    /// Gets or sets the corner radius in pixels.
    /// </summary>
    public int BorderRadius
    {
        get => _borderRadius;
        set
        {
            _borderRadius = Math.Max(0, value);
            UiGeometry.ApplyRoundedRegion(this, _borderRadius);
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the border thickness in pixels.
    /// </summary>
    public int BorderThickness { get; set; }

    /// <summary>
    /// Recalculates the native rounded region after the panel is resized.
    /// </summary>
    /// <param name="eventArgs">The event data.</param>
    protected override void OnResize(EventArgs eventArgs)
    {
        base.OnResize(eventArgs);
        UiGeometry.ApplyRoundedRegion(this, BorderRadius);
    }

    /// <summary>
    /// Draws the rounded background and border.
    /// </summary>
    /// <param name="eventArgs">The paint event data.</param>
    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Rectangle bounds = new(0, 0, Width - 1, Height - 1);

        using GraphicsPath path = UiGeometry.CreateRoundedRectangle(bounds, BorderRadius);
        using SolidBrush backgroundBrush = new(BackColor);
        using Pen borderPen = new(BorderColor, BorderThickness);
        eventArgs.Graphics.FillPath(backgroundBrush, path);
        eventArgs.Graphics.DrawPath(borderPen, path);
    }
}
