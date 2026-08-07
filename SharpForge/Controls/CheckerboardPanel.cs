namespace SharpForge.Controls;

/// <summary>
/// Displays a subtle checkerboard surface behind transparent preview images.
/// </summary>
internal sealed class CheckerboardPanel : Panel
{
    private const int CellSize = 14;

    /// <summary>
    /// Initializes a flicker-free checkerboard panel.
    /// </summary>
    public CheckerboardPanel()
    {
        DoubleBuffered = true;
        AutoScroll = true;
        ResizeRedraw = true;
    }

    /// <summary>
    /// Draws the checkerboard background.
    /// </summary>
    /// <param name="eventArgs">The paint event data.</param>
    protected override void OnPaintBackground(PaintEventArgs eventArgs)
    {
        Color firstColor = Color.FromArgb(30, 30, 30);
        Color secondColor = Color.FromArgb(34, 34, 34);
        using SolidBrush firstBrush = new(firstColor);
        using SolidBrush secondBrush = new(secondColor);

        for (int y = 0; y < Height; y += CellSize)
        {
            for (int x = 0; x < Width; x += CellSize)
            {
                bool alternate = ((x / CellSize) + (y / CellSize)) % 2 == 0;
                eventArgs.Graphics.FillRectangle(
                    alternate ? firstBrush : secondBrush,
                    x,
                    y,
                    CellSize,
                    CellSize);
            }
        }
    }
}
