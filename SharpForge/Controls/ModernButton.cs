using System.Drawing.Drawing2D;

namespace SharpForge.Controls;

/// <summary>
/// Displays a rounded, theme-aware action button.
/// </summary>
internal sealed class ModernButton : Button
{
    private int _cornerRadius = 8;
    private bool _isHovered;
    private bool _isPressed;

    /// <summary>
    /// Initializes a modern button with the default secondary style.
    /// </summary>
    public ModernButton()
    {
        BackColor = UiTheme.SurfaceElevated;
        BorderColor = UiTheme.Border;
        Cursor = Cursors.Hand;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        ForeColor = UiTheme.TextPrimary;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);
    }

    /// <summary>
    /// Gets or sets the normal border color.
    /// </summary>
    public Color BorderColor { get; set; }

    /// <summary>
    /// Gets or sets the hover background color.
    /// </summary>
    public Color HoverColor { get; set; } = UiTheme.SurfaceHover;

    /// <summary>
    /// Gets or sets the button corner radius.
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
    /// Recalculates the native rounded region after the button is resized.
    /// </summary>
    /// <param name="eventArgs">The event data.</param>
    protected override void OnResize(EventArgs eventArgs)
    {
        base.OnResize(eventArgs);
        UiGeometry.ApplyRoundedRegion(this, CornerRadius);
    }

    /// <summary>
    /// Updates the visual hover state.
    /// </summary>
    /// <param name="eventArgs">The event data.</param>
    protected override void OnMouseEnter(EventArgs eventArgs)
    {
        _isHovered = true;
        Invalidate();
        base.OnMouseEnter(eventArgs);
    }

    /// <summary>
    /// Clears the visual hover and pressed states.
    /// </summary>
    /// <param name="eventArgs">The event data.</param>
    protected override void OnMouseLeave(EventArgs eventArgs)
    {
        _isHovered = false;
        _isPressed = false;
        Invalidate();
        base.OnMouseLeave(eventArgs);
    }

    /// <summary>
    /// Updates the visual pressed state.
    /// </summary>
    /// <param name="eventArgs">The mouse event data.</param>
    protected override void OnMouseDown(MouseEventArgs eventArgs)
    {
        _isPressed = true;
        Invalidate();
        base.OnMouseDown(eventArgs);
    }

    /// <summary>
    /// Clears the visual pressed state.
    /// </summary>
    /// <param name="eventArgs">The mouse event data.</param>
    protected override void OnMouseUp(MouseEventArgs eventArgs)
    {
        _isPressed = false;
        Invalidate();
        base.OnMouseUp(eventArgs);
    }

    /// <summary>
    /// Draws the button surface, border, SVG icon, and text.
    /// </summary>
    /// <param name="paintEvent">The paint event data.</param>
    protected override void OnPaint(PaintEventArgs paintEvent)
    {
        paintEvent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Color background = ResolveBackgroundColor();
        Color foreground = Enabled ? ForeColor : UiTheme.TextMuted;

        using SolidBrush backgroundBrush = new(background);
        paintEvent.Graphics.FillRectangle(backgroundBrush, ClientRectangle);

        Rectangle borderBounds = new(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
        using GraphicsPath borderPath = UiGeometry.CreateRoundedRectangle(
            borderBounds,
            Math.Max(1, CornerRadius - 1));
        using Pen borderPen = new(Enabled ? BorderColor : UiTheme.BorderSubtle);
        paintEvent.Graphics.DrawPath(borderPen, borderPath);

        DrawContent(paintEvent.Graphics, foreground);
    }

    /// <summary>
    /// Suppresses rectangular native background painting because the control region owns the shape.
    /// </summary>
    /// <param name="paintEvent">The paint event data.</param>
    protected override void OnPaintBackground(PaintEventArgs paintEvent)
    {
        // The clipped region is filled by OnPaint to keep every corner artifact-free.
    }

    /// <summary>
    /// Resolves the background color for the current interaction state.
    /// </summary>
    /// <returns>The background color to paint.</returns>
    private Color ResolveBackgroundColor()
    {
        if (!Enabled)
        {
            return UiTheme.Surface;
        }

        if (_isPressed)
        {
            return ControlPaint.Dark(HoverColor, 0.08F);
        }

        return _isHovered ? HoverColor : BackColor;
    }

    /// <summary>
    /// Draws centered icon-only or icon-and-text button content.
    /// </summary>
    /// <param name="graphics">The graphics surface used to draw the content.</param>
    /// <param name="foreground">The color used to draw the button text.</param>
    private void DrawContent(Graphics graphics, Color foreground)
    {
        if (Image is null)
        {
            TextRenderer.DrawText(
                graphics,
                Text,
                Font,
                ClientRectangle,
                foreground,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPrefix);
            return;
        }

        if (string.IsNullOrWhiteSpace(Text))
        {
            int iconX = (Width - Image.Width) / 2;
            int iconY = (Height - Image.Height) / 2;
            graphics.DrawImage(Image, iconX, iconY, Image.Width, Image.Height);
            return;
        }

        Size textSize = TextRenderer.MeasureText(
            Text,
            Font,
            Size.Empty,
            TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        const int spacing = 9;
        int contentWidth = Image.Width + spacing + textSize.Width;
        int iconLeft = Math.Max(10, (Width - contentWidth) / 2);
        int iconTop = (Height - Image.Height) / 2;
        graphics.DrawImage(Image, iconLeft, iconTop, Image.Width, Image.Height);

        Rectangle textBounds = new(
            iconLeft + Image.Width + spacing,
            0,
            Math.Max(0, Width - iconLeft - Image.Width - spacing - 8),
            Height);
        TextRenderer.DrawText(
            graphics,
            Text,
            Font,
            textBounds,
            foreground,
            TextFormatFlags.Left |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPrefix);
    }
}
