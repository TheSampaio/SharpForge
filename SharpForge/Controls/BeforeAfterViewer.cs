using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace SharpForge.Controls;

/// <summary>
/// Displays aligned before and after images separated by a draggable vertical divider.
/// </summary>
internal sealed class BeforeAfterViewer : Control
{
    private const int DividerWidth = 3;
    private const int HandleRadius = 8;
    private const int LabelPadding = 7;
    private float _dividerPosition = 0.5F;
    private bool _isDragging;
    private Image? _beforeImage;
    private Image? _afterImage;

    /// <summary>
    /// Initializes a transparent, flicker-free comparison viewer.
    /// </summary>
    public BeforeAfterViewer()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.UserPaint,
            true);
        BackColor = Color.Transparent;
        Cursor = Cursors.Default;
        TabStop = false;
    }

    /// <summary>
    /// Gets or sets the original image drawn across the full viewport.
    /// </summary>
    public Image? BeforeImage
    {
        get => _beforeImage;
        set
        {
            _beforeImage = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the upscaled image drawn to the right of the divider.
    /// </summary>
    public Image? AfterImage
    {
        get => _afterImage;
        set
        {
            _afterImage = value;
            Cursor = value is null ? Cursors.Default : Cursors.VSplit;
            Invalidate();
        }
    }

    /// <summary>
    /// Paints aligned images, comparison labels, and the draggable divider.
    /// </summary>
    /// <param name="eventArgs">The paint event data.</param>
    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        if (_beforeImage is null || ClientSize.Width <= 0 || ClientSize.Height <= 0)
        {
            return;
        }

        Rectangle imageBounds = GetImageBounds(_afterImage ?? _beforeImage);
        eventArgs.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        eventArgs.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        eventArgs.Graphics.CompositingMode = CompositingMode.SourceOver;
        DrawImage(eventArgs.Graphics, _beforeImage, imageBounds);

        if (_afterImage is null)
        {
            return;
        }

        int dividerX = imageBounds.Left + (int)Math.Round(imageBounds.Width * _dividerPosition);
        GraphicsState state = eventArgs.Graphics.Save();
        eventArgs.Graphics.SetClip(new Rectangle(
            dividerX,
            imageBounds.Top,
            Math.Max(0, imageBounds.Right - dividerX),
            imageBounds.Height));
        DrawImage(eventArgs.Graphics, _afterImage, imageBounds);
        eventArgs.Graphics.Restore(state);

        DrawComparisonLabel(eventArgs.Graphics, "BEFORE", imageBounds.Left + 8, imageBounds.Top + 8);
        Size afterLabelSize = TextRenderer.MeasureText("AFTER", Font);
        DrawComparisonLabel(
            eventArgs.Graphics,
            "AFTER",
            imageBounds.Right - afterLabelSize.Width - (LabelPadding * 2) - 8,
            imageBounds.Top + 8);
        DrawDivider(eventArgs.Graphics, dividerX, imageBounds);
    }

    /// <summary>
    /// Starts divider dragging and immediately moves it to the pointer position.
    /// </summary>
    /// <param name="eventArgs">The mouse event data.</param>
    protected override void OnMouseDown(MouseEventArgs eventArgs)
    {
        base.OnMouseDown(eventArgs);
        if (eventArgs.Button != MouseButtons.Left || _afterImage is null)
        {
            return;
        }

        _isDragging = true;
        Capture = true;
        UpdateDividerPosition(eventArgs.X);
    }

    /// <summary>
    /// Moves the divider while the primary mouse button is held.
    /// </summary>
    /// <param name="eventArgs">The mouse event data.</param>
    protected override void OnMouseMove(MouseEventArgs eventArgs)
    {
        base.OnMouseMove(eventArgs);
        if (_isDragging)
        {
            UpdateDividerPosition(eventArgs.X);
        }
    }

    /// <summary>
    /// Ends divider dragging when the primary mouse button is released.
    /// </summary>
    /// <param name="eventArgs">The mouse event data.</param>
    protected override void OnMouseUp(MouseEventArgs eventArgs)
    {
        base.OnMouseUp(eventArgs);
        if (eventArgs.Button == MouseButtons.Left)
        {
            _isDragging = false;
            Capture = false;
        }
    }

    /// <summary>
    /// Ends divider dragging if the control loses mouse capture.
    /// </summary>
    /// <param name="eventArgs">The event data.</param>
    protected override void OnMouseCaptureChanged(EventArgs eventArgs)
    {
        base.OnMouseCaptureChanged(eventArgs);
        if (!Capture)
        {
            _isDragging = false;
        }
    }

    /// <summary>
    /// Calculates an aspect-fit rectangle for the selected reference image.
    /// </summary>
    /// <param name="image">The image whose aspect ratio should be preserved.</param>
    /// <returns>The centered image bounds.</returns>
    private Rectangle GetImageBounds(Image image)
    {
        float scale = Math.Min(
            ClientSize.Width / (float)image.Width,
            ClientSize.Height / (float)image.Height);
        int width = Math.Max(1, (int)Math.Round(image.Width * scale));
        int height = Math.Max(1, (int)Math.Round(image.Height * scale));
        return new Rectangle(
            (ClientSize.Width - width) / 2,
            (ClientSize.Height - height) / 2,
            width,
            height);
    }

    /// <summary>
    /// Draws an image with edge wrapping that avoids dark interpolation borders.
    /// </summary>
    /// <param name="graphics">The destination graphics surface.</param>
    /// <param name="image">The image to draw.</param>
    /// <param name="bounds">The destination bounds.</param>
    private static void DrawImage(Graphics graphics, Image image, Rectangle bounds)
    {
        using ImageAttributes attributes = new();
        attributes.SetWrapMode(WrapMode.TileFlipXY);
        graphics.DrawImage(
            image,
            bounds,
            0,
            0,
            image.Width,
            image.Height,
            GraphicsUnit.Pixel,
            attributes);
    }

    /// <summary>
    /// Draws one compact comparison label over the image.
    /// </summary>
    /// <param name="graphics">The destination graphics surface.</param>
    /// <param name="text">The label text.</param>
    /// <param name="x">The left label coordinate.</param>
    /// <param name="y">The top label coordinate.</param>
    private void DrawComparisonLabel(Graphics graphics, string text, int x, int y)
    {
        Size textSize = TextRenderer.MeasureText(text, Font);
        Rectangle backgroundBounds = new(
            x,
            y,
            textSize.Width + (LabelPadding * 2),
            textSize.Height + 4);
        using GraphicsPath backgroundPath = UiGeometry.CreateRoundedRectangle(backgroundBounds, 5);
        using SolidBrush backgroundBrush = new(Color.FromArgb(210, 35, 40, 48));
        graphics.FillPath(backgroundBrush, backgroundPath);
        TextRenderer.DrawText(
            graphics,
            text,
            Font,
            new Point(x + LabelPadding, y + 2),
            Color.White,
            TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
    }

    /// <summary>
    /// Draws the vertical divider and its centered drag handle.
    /// </summary>
    /// <param name="graphics">The destination graphics surface.</param>
    /// <param name="dividerX">The horizontal divider coordinate.</param>
    /// <param name="imageBounds">The displayed image bounds.</param>
    private static void DrawDivider(Graphics graphics, int dividerX, Rectangle imageBounds)
    {
        using Pen shadowPen = new(Color.FromArgb(100, Color.Black), DividerWidth + 2);
        using Pen dividerPen = new(Color.White, DividerWidth);
        graphics.DrawLine(shadowPen, dividerX, imageBounds.Top, dividerX, imageBounds.Bottom);
        graphics.DrawLine(dividerPen, dividerX, imageBounds.Top, dividerX, imageBounds.Bottom);

        int centerY = imageBounds.Top + (imageBounds.Height / 2);
        Rectangle handleBounds = new(
            dividerX - HandleRadius,
            centerY - HandleRadius,
            HandleRadius * 2,
            HandleRadius * 2);
        using SolidBrush handleBrush = new(Color.White);
        using Pen handleBorder = new(Color.FromArgb(90, Color.Black), 1F);
        graphics.FillEllipse(handleBrush, handleBounds);
        graphics.DrawEllipse(handleBorder, handleBounds);
    }

    /// <summary>
    /// Converts a pointer coordinate into a bounded divider fraction.
    /// </summary>
    /// <param name="pointerX">The pointer's horizontal client coordinate.</param>
    private void UpdateDividerPosition(int pointerX)
    {
        if (_beforeImage is null)
        {
            return;
        }

        Rectangle imageBounds = GetImageBounds(_afterImage ?? _beforeImage);
        _dividerPosition = Math.Clamp(
            (pointerX - imageBounds.Left) / (float)Math.Max(1, imageBounds.Width),
            0F,
            1F);
        Invalidate();
    }
}
