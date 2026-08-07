using System.Drawing.Drawing2D;

namespace SharpForge.Controls;

/// <summary>
/// Displays a larger rounded checkbox with blue interactive states.
/// </summary>
internal sealed class ModernCheckBox : CheckBox
{
    private const int BoxSize = 16;
    private readonly Bitmap _checkIcon;
    private bool _isHovered;

    /// <summary>
    /// Initializes a theme-aware checkbox with an SVG check mark.
    /// </summary>
    public ModernCheckBox()
    {
        AutoSize = false;
        BackColor = UiTheme.Surface;
        Cursor = Cursors.Hand;
        FlatStyle = FlatStyle.Flat;
        Font = new Font("Segoe UI", 8.5F);
        ForeColor = UiTheme.TextSecondary;
        MinimumSize = new Size(80, 22);
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);
        _checkIcon = SvgIconProvider.Create("check", 10, Color.White);
    }

    /// <summary>
    /// Releases the SVG icon owned by the checkbox.
    /// </summary>
    /// <param name="disposing">Whether managed resources should be released.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _checkIcon.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Draws the rounded box, selection state, and label.
    /// </summary>
    /// <param name="paintEvent">The paint event data.</param>
    protected override void OnPaint(PaintEventArgs paintEvent)
    {
        paintEvent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using SolidBrush controlBackground = new(BackColor);
        paintEvent.Graphics.FillRectangle(controlBackground, ClientRectangle);

        Rectangle boxBounds = new(0, (Height - BoxSize) / 2, BoxSize - 1, BoxSize - 1);
        Color borderColor = Enabled
            ? _isHovered ? UiTheme.PrimaryHover : UiTheme.Primary
            : UiTheme.Border;
        Color boxColor = Checked ? UiTheme.Primary : UiTheme.SurfaceElevated;
        using GraphicsPath boxPath = UiGeometry.CreateRoundedRectangle(boxBounds, 4);
        using SolidBrush boxBrush = new(boxColor);
        using Pen borderPen = new(borderColor, 1.5F);
        paintEvent.Graphics.FillPath(boxBrush, boxPath);
        paintEvent.Graphics.DrawPath(borderPen, boxPath);

        if (Checked)
        {
            int iconX = boxBounds.X + ((boxBounds.Width - _checkIcon.Width) / 2);
            int iconY = boxBounds.Y + ((boxBounds.Height - _checkIcon.Height) / 2);
            paintEvent.Graphics.DrawImage(_checkIcon, iconX, iconY, _checkIcon.Width, _checkIcon.Height);
        }

        Rectangle textBounds = new(BoxSize + 8, 0, Math.Max(0, Width - BoxSize - 8), Height);
        TextRenderer.DrawText(
            paintEvent.Graphics,
            Text,
            Font,
            textBounds,
            Enabled ? ForeColor : UiTheme.TextMuted,
            TextFormatFlags.Left |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPrefix);
    }

    /// <summary>
    /// Activates the hover border color.
    /// </summary>
    /// <param name="eventArgs">The event data.</param>
    protected override void OnMouseEnter(EventArgs eventArgs)
    {
        _isHovered = true;
        Invalidate();
        base.OnMouseEnter(eventArgs);
    }

    /// <summary>
    /// Restores the default border color.
    /// </summary>
    /// <param name="eventArgs">The event data.</param>
    protected override void OnMouseLeave(EventArgs eventArgs)
    {
        _isHovered = false;
        Invalidate();
        base.OnMouseLeave(eventArgs);
    }

    /// <summary>
    /// Repaints the checkbox when its value changes.
    /// </summary>
    /// <param name="eventArgs">The event data.</param>
    protected override void OnCheckedChanged(EventArgs eventArgs)
    {
        Invalidate();
        base.OnCheckedChanged(eventArgs);
    }

    /// <summary>
    /// Repaints the checkbox when its enabled state changes.
    /// </summary>
    /// <param name="eventArgs">The event data.</param>
    protected override void OnEnabledChanged(EventArgs eventArgs)
    {
        Invalidate();
        base.OnEnabledChanged(eventArgs);
    }
}
