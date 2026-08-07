using System.Drawing.Drawing2D;

namespace SharpForge.Controls;

/// <summary>
/// Displays a compact count inside a rounded accent badge.
/// </summary>
internal sealed class RoundedBadge : Control
{
    private int _cornerRadius = 5;

    /// <summary>
    /// Initializes a blue badge with centered text.
    /// </summary>
    public RoundedBadge()
    {
        BackColor = UiTheme.Primary;
        Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        ForeColor = Color.White;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);
    }

    /// <summary>
    /// Gets or sets the subtle corner radius in pixels.
    /// </summary>
    public int CornerRadius
    {
        get => _cornerRadius;
        set
        {
            _cornerRadius = Math.Max(0, value);
            UiGeometry.ApplyRoundedRegion(this, _cornerRadius);
            Invalidate();
        }
    }

    /// <summary>
    /// Recalculates the capsule region after the badge is resized.
    /// </summary>
    /// <param name="eventArgs">The event data.</param>
    protected override void OnResize(EventArgs eventArgs)
    {
        base.OnResize(eventArgs);
        UiGeometry.ApplyRoundedRegion(this, CornerRadius);
    }

    /// <summary>
    /// Draws the badge background and centered count.
    /// </summary>
    /// <param name="paintEvent">The paint event data.</param>
    protected override void OnPaint(PaintEventArgs paintEvent)
    {
        paintEvent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using SolidBrush backgroundBrush = new(BackColor);
        paintEvent.Graphics.FillRectangle(backgroundBrush, ClientRectangle);
        TextRenderer.DrawText(
            paintEvent.Graphics,
            Text,
            Font,
            ClientRectangle,
            ForeColor,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPrefix);
    }
}
