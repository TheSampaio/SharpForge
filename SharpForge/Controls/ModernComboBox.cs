using System.Drawing.Drawing2D;

namespace SharpForge.Controls;

/// <summary>
/// Displays a fully themed selection control with a dark popup list.
/// </summary>
internal sealed class ModernComboBox : Control
{
    private const int HorizontalPadding = 12;
    private const int PopupItemHeight = 30;

    private readonly Bitmap _chevronIcon;
    private readonly List<object> _items = [];
    private readonly ToolStripDropDown _popup;
    private readonly ListBox _popupList;
    private readonly ToolStripControlHost _popupHost;
    private bool _isHovered;
    private int _selectedIndex = -1;

    /// <summary>
    /// Initializes a keyboard-accessible selector that follows the application theme.
    /// </summary>
    public ModernComboBox()
    {
        AccessibleRole = AccessibleRole.ComboBox;
        BackColor = UiTheme.SurfaceElevated;
        Cursor = Cursors.Hand;
        Font = new Font("Segoe UI", 9F);
        ForeColor = UiTheme.TextPrimary;
        MinimumSize = new Size(80, 30);
        Size = new Size(126, 30);
        TabStop = true;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);

        _chevronIcon = SvgIconProvider.Create("chevron-down", 12, UiTheme.TextSecondary);
        _popupList = CreatePopupList();
        _popupHost = new ToolStripControlHost(_popupList)
        {
            AutoSize = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        _popup = new ToolStripDropDown
        {
            AutoClose = true,
            BackColor = UiTheme.SurfaceElevated,
            DropShadowEnabled = true,
            Margin = Padding.Empty,
            Padding = new Padding(1)
        };
        _popup.Items.Add(_popupHost);
        _popup.Closed += Popup_Closed;
    }

    /// <summary>
    /// Occurs when the selected item changes.
    /// </summary>
    public event EventHandler? SelectedIndexChanged;

    /// <summary>
    /// Gets the number of available items.
    /// </summary>
    public int ItemCount => _items.Count;

    /// <summary>
    /// Gets or sets the index of the selected item.
    /// </summary>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (value < -1 || value >= _items.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (_selectedIndex == value)
            {
                return;
            }

            _selectedIndex = value;
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Gets the currently selected item, or <see langword="null"/> when no item is selected.
    /// </summary>
    public object? SelectedItem => SelectedIndex >= 0 ? _items[SelectedIndex] : null;

    /// <summary>
    /// Replaces the available items and clears the current selection.
    /// </summary>
    /// <param name="items">The items displayed by the selector.</param>
    public void SetItems(IEnumerable<object> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        _items.Clear();
        _items.AddRange(items);
        _selectedIndex = -1;
        Invalidate();
    }

    /// <summary>
    /// Releases the popup and SVG resources owned by the selector.
    /// </summary>
    /// <param name="disposing">Whether managed resources should be released.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _popup.Dispose();
            _chevronIcon.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Draws the selector surface, value, focus state, and SVG chevron.
    /// </summary>
    /// <param name="paintEvent">The paint event data.</param>
    protected override void OnPaint(PaintEventArgs paintEvent)
    {
        base.OnPaint(paintEvent);
        paintEvent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Rectangle bounds = new(0, 0, Width - 1, Height - 1);
        Color borderColor = Focused || _popup.Visible
            ? UiTheme.Primary
            : _isHovered ? Color.FromArgb(92, 105, 124) : UiTheme.Border;

        using GraphicsPath path = UiGeometry.CreateRoundedRectangle(bounds, 3);
        using SolidBrush backgroundBrush = new(Enabled ? BackColor : UiTheme.Surface);
        using Pen borderPen = new(borderColor);
        paintEvent.Graphics.FillPath(backgroundBrush, path);
        paintEvent.Graphics.DrawPath(borderPen, path);

        string displayText = SelectedItem?.ToString() ?? string.Empty;
        Rectangle textBounds = new(
            HorizontalPadding,
            1,
            Math.Max(0, Width - 46),
            Height - 2);
        TextRenderer.DrawText(
            paintEvent.Graphics,
            displayText,
            Font,
            textBounds,
            Enabled ? ForeColor : UiTheme.TextMuted,
            TextFormatFlags.Left |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPrefix);

        Rectangle iconBounds = new(Width - 25, (Height - _chevronIcon.Height) / 2, 12, 12);
        paintEvent.Graphics.DrawImage(_chevronIcon, iconBounds);
    }

    /// <summary>
    /// Tracks the hover state used by the selector border.
    /// </summary>
    /// <param name="eventArgs">The event data.</param>
    protected override void OnMouseEnter(EventArgs eventArgs)
    {
        _isHovered = true;
        Invalidate();
        base.OnMouseEnter(eventArgs);
    }

    /// <summary>
    /// Clears the hover state used by the selector border.
    /// </summary>
    /// <param name="eventArgs">The event data.</param>
    protected override void OnMouseLeave(EventArgs eventArgs)
    {
        _isHovered = false;
        Invalidate();
        base.OnMouseLeave(eventArgs);
    }

    /// <summary>
    /// Toggles the popup list when the selector is clicked.
    /// </summary>
    /// <param name="eventArgs">The mouse event data.</param>
    protected override void OnMouseDown(MouseEventArgs eventArgs)
    {
        base.OnMouseDown(eventArgs);
        Focus();

        if (eventArgs.Button == MouseButtons.Left && Enabled)
        {
            TogglePopup();
        }
    }

    /// <summary>
    /// Invalidates the control when it receives keyboard focus.
    /// </summary>
    /// <param name="eventArgs">The event data.</param>
    protected override void OnGotFocus(EventArgs eventArgs)
    {
        Invalidate();
        base.OnGotFocus(eventArgs);
    }

    /// <summary>
    /// Invalidates the control when it loses keyboard focus.
    /// </summary>
    /// <param name="eventArgs">The event data.</param>
    protected override void OnLostFocus(EventArgs eventArgs)
    {
        Invalidate();
        base.OnLostFocus(eventArgs);
    }

    /// <summary>
    /// Treats navigation keys as input keys for accessible selection changes.
    /// </summary>
    /// <param name="keyData">The key to evaluate.</param>
    /// <returns><see langword="true"/> for selector navigation keys.</returns>
    protected override bool IsInputKey(Keys keyData)
    {
        Keys keyCode = keyData & Keys.KeyCode;
        return keyCode is Keys.Up or Keys.Down or Keys.Home or Keys.End || base.IsInputKey(keyData);
    }

    /// <summary>
    /// Supports keyboard navigation and popup activation.
    /// </summary>
    /// <param name="eventArgs">The keyboard event data.</param>
    protected override void OnKeyDown(KeyEventArgs eventArgs)
    {
        switch (eventArgs.KeyCode)
        {
            case Keys.Enter:
            case Keys.Space:
                TogglePopup();
                eventArgs.Handled = true;
                break;
            case Keys.Down:
                MoveSelection(1);
                eventArgs.Handled = true;
                break;
            case Keys.Up:
                MoveSelection(-1);
                eventArgs.Handled = true;
                break;
            case Keys.Home when _items.Count > 0:
                SelectedIndex = 0;
                eventArgs.Handled = true;
                break;
            case Keys.End when _items.Count > 0:
                SelectedIndex = _items.Count - 1;
                eventArgs.Handled = true;
                break;
            case Keys.Escape when _popup.Visible:
                _popup.Close();
                eventArgs.Handled = true;
                break;
        }

        base.OnKeyDown(eventArgs);
    }

    /// <summary>
    /// Repaints the selector after its enabled state changes.
    /// </summary>
    /// <param name="eventArgs">The event data.</param>
    protected override void OnEnabledChanged(EventArgs eventArgs)
    {
        Invalidate();
        base.OnEnabledChanged(eventArgs);
    }

    /// <summary>
    /// Recalculates the native rounded region after the selector is resized.
    /// </summary>
    /// <param name="eventArgs">The event data.</param>
    protected override void OnResize(EventArgs eventArgs)
    {
        base.OnResize(eventArgs);
        UiGeometry.ApplyRoundedRegion(this, 3);
    }

    /// <summary>
    /// Creates the owner-drawn list displayed inside the popup.
    /// </summary>
    /// <returns>A configured popup list.</returns>
    private ListBox CreatePopupList()
    {
        ListBox list = new()
        {
            BackColor = UiTheme.SurfaceElevated,
            BorderStyle = BorderStyle.None,
            DrawMode = DrawMode.OwnerDrawFixed,
            Font = Font,
            ForeColor = UiTheme.TextPrimary,
            IntegralHeight = false,
            ItemHeight = PopupItemHeight
        };
        list.DrawItem += PopupList_DrawItem;
        list.MouseMove += PopupList_MouseMove;
        list.Click += PopupList_Click;
        list.HandleCreated += PopupList_HandleCreated;
        return list;
    }

    /// <summary>
    /// Applies the dark native theme to the popup list and its vertical scrollbar.
    /// </summary>
    /// <param name="sender">The popup list.</param>
    /// <param name="eventArgs">The event data.</param>
    private static void PopupList_HandleCreated(object? sender, EventArgs eventArgs)
    {
        if (sender is Control control)
        {
            NativeWindowTheme.ApplyToControl(control);
        }
    }

    /// <summary>
    /// Opens or closes the popup list.
    /// </summary>
    private void TogglePopup()
    {
        if (_popup.Visible)
        {
            _popup.Close();
            return;
        }

        if (_items.Count == 0)
        {
            return;
        }

        _popupList.Items.Clear();
        _popupList.Items.AddRange(_items.ToArray());
        _popupList.SelectedIndex = SelectedIndex;
        int visibleItemCount = Math.Min(8, _items.Count);
        Size popupSize = new(Width, (visibleItemCount * PopupItemHeight) + 2);
        _popupList.Size = popupSize;
        _popupHost.Size = popupSize;
        _popup.Size = popupSize;
        _popup.Show(PointToScreen(new Point(0, Height + 2)));
        _popupList.Focus();
        Invalidate();
    }

    /// <summary>
    /// Moves the current selection by a bounded offset.
    /// </summary>
    /// <param name="offset">The signed selection offset.</param>
    private void MoveSelection(int offset)
    {
        if (_items.Count == 0)
        {
            return;
        }

        SelectedIndex = Math.Clamp(Math.Max(0, SelectedIndex) + offset, 0, _items.Count - 1);
    }

    /// <summary>
    /// Draws a popup item with dark normal and blue selected states.
    /// </summary>
    /// <param name="sender">The popup list.</param>
    /// <param name="eventArgs">The item drawing event data.</param>
    private void PopupList_DrawItem(object? sender, DrawItemEventArgs eventArgs)
    {
        if (eventArgs.Index < 0)
        {
            return;
        }

        bool selected = (eventArgs.State & DrawItemState.Selected) != 0;
        using SolidBrush backgroundBrush = new(selected ? UiTheme.PrimaryMuted : UiTheme.SurfaceElevated);
        eventArgs.Graphics.FillRectangle(backgroundBrush, eventArgs.Bounds);
        Rectangle textBounds = Rectangle.Inflate(eventArgs.Bounds, -HorizontalPadding, 0);
        TextRenderer.DrawText(
            eventArgs.Graphics,
            _popupList.GetItemText(_popupList.Items[eventArgs.Index]),
            Font,
            textBounds,
            UiTheme.TextPrimary,
            TextFormatFlags.Left |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPrefix);
    }

    /// <summary>
    /// Highlights the popup item under the pointer.
    /// </summary>
    /// <param name="sender">The popup list.</param>
    /// <param name="eventArgs">The mouse event data.</param>
    private void PopupList_MouseMove(object? sender, MouseEventArgs eventArgs)
    {
        int index = _popupList.IndexFromPoint(eventArgs.Location);
        if (index >= 0)
        {
            _popupList.SelectedIndex = index;
        }
    }

    /// <summary>
    /// Commits the clicked popup item and closes the popup.
    /// </summary>
    /// <param name="sender">The popup list.</param>
    /// <param name="eventArgs">The event data.</param>
    private void PopupList_Click(object? sender, EventArgs eventArgs)
    {
        if (_popupList.SelectedIndex >= 0)
        {
            SelectedIndex = _popupList.SelectedIndex;
        }

        _popup.Close();
        Focus();
    }

    /// <summary>
    /// Restores focus styling after the popup closes.
    /// </summary>
    /// <param name="sender">The popup.</param>
    /// <param name="eventArgs">The close event data.</param>
    private void Popup_Closed(object? sender, ToolStripDropDownClosedEventArgs eventArgs)
    {
        Invalidate();
    }
}
