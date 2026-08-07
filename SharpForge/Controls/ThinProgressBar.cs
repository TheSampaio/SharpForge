using System.Drawing.Drawing2D;

namespace SharpForge.Controls;

/// <summary>
/// Displays compact progress using the application accent color.
/// </summary>
internal sealed class ThinProgressBar : Control
{
    private int _maximum = 1;
    private int _value;

    /// <summary>
    /// Initializes a double-buffered progress bar.
    /// </summary>
    public ThinProgressBar()
    {
        DoubleBuffered = true;
        Height = 3;
    }

    /// <summary>
    /// Gets or sets the maximum progress value.
    /// </summary>
    public int Maximum
    {
        get => _maximum;
        set
        {
            _maximum = Math.Max(1, value);
            Value = Math.Min(_value, _maximum);
        }
    }

    /// <summary>
    /// Gets or sets the current progress value.
    /// </summary>
    public int Value
    {
        get => _value;
        set
        {
            _value = Math.Clamp(value, 0, Maximum);
            Invalidate();
        }
    }

    /// <summary>
    /// Draws the progress track and completed portion.
    /// </summary>
    /// <param name="eventArgs">The paint event data.</param>
    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        eventArgs.Graphics.Clear(UiTheme.BorderSubtle);

        if (Value == 0)
        {
            return;
        }

        int progressWidth = Math.Max(2, (int)Math.Round(Width * (Value / (double)Maximum)));
        using SolidBrush brush = new(UiTheme.Primary);
        eventArgs.Graphics.FillRectangle(brush, 0, 0, progressWidth, Height);
    }
}
