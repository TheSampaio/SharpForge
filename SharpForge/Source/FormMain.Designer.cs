#nullable enable

using SharpForge.Controls;

namespace SharpForge;

partial class FormMain
{
    private System.ComponentModel.IContainer? components;

    /// <summary>
    /// Releases resources owned by the form.
    /// </summary>
    /// <param name="disposing">Whether managed resources should be released.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
            _currentPreview?.Dispose();
            _currentAfterPreview?.Dispose();
            _cancellationTokenSource?.Dispose();
            _modelImportCancellationTokenSource?.Dispose();
            _upscaler.Dispose();
            DisposeQueueThumbnails();
            DisposeUiIcons();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Creates and arranges the visual controls used by the main window.
    /// </summary>
    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        System.ComponentModel.ComponentResourceManager resources = new(typeof(FormMain));
        MainToolTip = new ToolTip(components);
        ElapsedTimer = new System.Windows.Forms.Timer(components) { Interval = 250 };
        ElapsedTimer.Tick += ElapsedTimer_Tick;

        RootLayout = new TableLayoutPanel
        {
            BackColor = UiTheme.Window,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            RowCount = 3
        };
        RootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        RootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 92F));
        RootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        RootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));

        InitializeHeader();
        InitializeContent();
        InitializeStatusBar();
        RootLayout.Controls.Add(HeaderPanel, 0, 0);
        RootLayout.Controls.Add(ContentLayout, 0, 1);
        RootLayout.Controls.Add(StatusPanel, 0, 2);

        AllowDrop = true;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = UiTheme.Window;
        ClientSize = new Size(1440, 900);
        Controls.Add(RootLayout);
        Icon = (Icon?)resources.GetObject("$this.Icon");
        MinimumSize = new Size(1080, 700);
        Name = "FormMain";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Sharp Forge";
        FormClosing += FormMain_FormClosing;
        DragDrop += Preview_DragDrop;
        DragEnter += Preview_DragEnter;
    }

    /// <summary>
    /// Creates the responsive title and action toolbar.
    /// </summary>
    private void InitializeHeader()
    {
        HeaderPanel = new Panel
        {
            BackColor = UiTheme.Window,
            Dock = DockStyle.Fill,
            Padding = new Padding(26, 14, 26, 8)
        };

        TableLayoutPanel headerLayout = new()
        {
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            RowCount = 1
        };
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        Panel titlePanel = new() { Dock = DockStyle.Fill };
        Lbl_Title = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 19F, FontStyle.Bold),
            ForeColor = UiTheme.TextPrimary,
            Location = new Point(0, 3),
            Text = "Sharp Forge"
        };
        Lbl_Subtitle = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 9F),
            ForeColor = UiTheme.TextSecondary,
            Location = new Point(2, 42),
            Text = "Batch AI upscaling powered by Real-ESRGAN"
        };
        titlePanel.Controls.AddRange([Lbl_Title, Lbl_Subtitle]);

        FlowLayoutPanel actionsPanel = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 10, 0, 0),
            WrapContents = false
        };
        Btn_AddImages = CreateToolbarButton("Add Images", 132, "image-plus");
        Btn_AddImages.Click += Btn_AddImages_Click;
        Btn_Upscale = CreateToolbarButton("Start Upscaling", 158, "play", isPrimary: true);
        _startUpscaleIcon = Btn_Upscale.Image!;
        _cancelUpscaleIcon = CreateUiIcon("square", 16, Color.White);
        _queueCheckIcon = CreateUiIcon("circle-check", 17, Color.FromArgb(145, 198, 255));
        Btn_Upscale.Enabled = false;
        Btn_Upscale.Click += Btn_Upscale_Click;
        Btn_Clear = CreateToolbarButton("Clear Queue", 132, "trash");
        Btn_Clear.Enabled = false;
        Btn_Clear.Click += Btn_Clear_Click;
        Btn_ToggleSettings = CreateToolbarButton(string.Empty, 46, "settings");
        Btn_ToggleSettings.Click += Btn_ToggleSettings_Click;
        Btn_Donate = CreateToolbarButton(string.Empty, 46, "heart");
        Btn_Donate.Click += Btn_Donate_Click;

        MainToolTip.SetToolTip(Btn_AddImages, "Select one or more source images.");
        MainToolTip.SetToolTip(Btn_Upscale, "Start the queue or cancel the active task.");
        MainToolTip.SetToolTip(Btn_Clear, "Remove every image from the queue.");
        MainToolTip.SetToolTip(Btn_ToggleSettings, "Show or hide the settings sidebar.");
        MainToolTip.SetToolTip(Btn_Donate, "Support Sharp Forge development.");
        actionsPanel.Controls.AddRange([Btn_Upscale, Btn_Clear, Btn_AddImages, Btn_Donate, Btn_ToggleSettings]);

        headerLayout.Controls.Add(titlePanel, 0, 0);
        headerLayout.Controls.Add(actionsPanel, 1, 0);
        HeaderPanel.Controls.Add(headerLayout);
    }

    /// <summary>
    /// Creates the settings sidebar and preview workspace.
    /// </summary>
    private void InitializeContent()
    {
        ContentLayout = new TableLayoutPanel
        {
            BackColor = UiTheme.Window,
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Padding = new Padding(26, 8, 26, 10),
            RowCount = 1
        };
        ContentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320F));
        ContentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        ContentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        SettingsPanel = new Panel
        {
            AutoScroll = true,
            BackColor = UiTheme.Window,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 10, 0)
        };
        SettingsFlow = new FlowLayoutPanel
        {
            AutoScroll = true,
            BackColor = UiTheme.Window,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            Margin = Padding.Empty,
            WrapContents = false
        };

        InitializeQueueSection();
        InitializeModelSection();
        InitializePerformanceSection();
        InitializePipelineSection();
        InitializeOutputSection();
        SettingsFlow.Controls.AddRange([Grp_Queue, Grp_Model, Grp_Performance, Grp_Advanced, Grp_Output]);
        SettingsPanel.Controls.Add(SettingsFlow);

        InitializePreview();
        ContentLayout.Controls.Add(SettingsPanel, 0, 0);
        ContentLayout.Controls.Add(PreviewPanel, 1, 0);
    }

    /// <summary>
    /// Creates the owner-drawn image queue card.
    /// </summary>
    private void InitializeQueueSection()
    {
        Grp_Queue = CreateSectionCard("images", "QUEUE", 238);
        Lbl_QueueBadge = new RoundedBadge
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            CornerRadius = 5,
            Location = new Point(250, 11),
            Size = new Size(26, 24),
            Text = "0",
        };
        Lst_Images = new ListBox
        {
            BackColor = UiTheme.Surface,
            BorderStyle = BorderStyle.None,
            DrawMode = DrawMode.OwnerDrawFixed,
            Font = new Font("Segoe UI", 9F),
            ForeColor = UiTheme.TextPrimary,
            IntegralHeight = false,
            ItemHeight = 64,
            Location = new Point(10, 43),
            Size = new Size(270, 150)
        };
        Lst_Images.DrawItem += Lst_Images_DrawItem;
        Lst_Images.SelectedIndexChanged += Lst_Images_SelectedIndexChanged;
        Lbl_QueueCount = new Label
        {
            AutoEllipsis = true,
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = UiTheme.TextSecondary,
            Location = new Point(12, 206),
            Size = new Size(185, 20),
            Text = "Queue is empty"
        };
        Btn_RemoveSelected = new ModernButton
        {
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            BackColor = UiTheme.Surface,
            BorderColor = UiTheme.Surface,
            Enabled = false,
            Image = CreateUiIcon("x", 14, UiTheme.TextSecondary),
            Location = new Point(245, 199),
            Size = new Size(34, 30),
            Text = string.Empty
        };
        Btn_RemoveSelected.Click += Btn_RemoveSelected_Click;
        MainToolTip.SetToolTip(Btn_RemoveSelected, "Remove the selected image from the queue.");
        Grp_Queue.Controls.AddRange([Lbl_QueueBadge, Lst_Images, Lbl_QueueCount, Btn_RemoveSelected]);
    }

    /// <summary>
    /// Creates the AI model card.
    /// </summary>
    private void InitializeModelSection()
    {
        Grp_Model = CreateSectionCard("sparkles", "AI MODEL", 124);
        Cmb_AI_Model = CreateComboBox();
        AddField(Grp_Model, "Model", Cmb_AI_Model, 42);
        Btn_ImportModel = new ModernButton
        {
            BackColor = UiTheme.Surface,
            BorderColor = UiTheme.Border,
            Image = CreateUiIcon("plus", 14, UiTheme.TextSecondary),
            Location = new Point(150, 78),
            Size = new Size(126, 30),
            Text = "Import Model"
        };
        Btn_ImportModel.Click += Btn_ImportModel_Click;
        MainToolTip.SetToolTip(Btn_ImportModel, "Import a .pth checkpoint or a matching .param/.bin NCNN model pair.");
        Grp_Model.Controls.Add(Btn_ImportModel);
    }

    /// <summary>
    /// Creates the performance settings card.
    /// </summary>
    private void InitializePerformanceSection()
    {
        Grp_Performance = CreateSectionCard("gauge", "PERFORMANCE", 140);
        Cmb_ConcurrentJobs = CreateComboBox();
        Cmb_TileSize = CreateComboBox();
        Cmb_GPU_Id = CreateComboBox();
        AddField(Grp_Performance, "Concurrent jobs", Cmb_ConcurrentJobs, 42);
        AddField(Grp_Performance, "Tile size", Cmb_TileSize, 72);
        AddField(Grp_Performance, "GPU ID", Cmb_GPU_Id, 102);
        MainToolTip.SetToolTip(Cmb_ConcurrentJobs, "Use 1 unless your GPU can run multiple processes.");
        MainToolTip.SetToolTip(Cmb_TileSize, "Lower values use less GPU memory.");
    }

    /// <summary>
    /// Creates the Real-ESRGAN pipeline settings card.
    /// </summary>
    private void InitializePipelineSection()
    {
        Grp_Advanced = CreateSectionCard("cpu", "GPU PIPELINE", 140);
        Cmb_GPU_ThreadsLoad = CreateComboBox();
        Cmb_GPU_ThreadsProcess = CreateComboBox();
        Cmb_GPU_ThreadsSave = CreateComboBox();
        AddField(Grp_Advanced, "Load threads", Cmb_GPU_ThreadsLoad, 42);
        AddField(Grp_Advanced, "Process threads", Cmb_GPU_ThreadsProcess, 72);
        AddField(Grp_Advanced, "Save threads", Cmb_GPU_ThreadsSave, 102);
    }

    /// <summary>
    /// Creates the output settings card.
    /// </summary>
    private void InitializeOutputSection()
    {
        Grp_Output = CreateSectionCard("folder-output", "OUTPUT", 110);
        Cmb_FileExtension = CreateComboBox();
        AddField(Grp_Output, "File format", Cmb_FileExtension, 42);
        Chk_UseSuffix = new ModernCheckBox
        {
            Location = new Point(13, 78),
            Size = new Size(260, 24),
            Text = "Save beside source file with model suffix"
        };
        Grp_Output.Controls.Add(Chk_UseSuffix);
    }

    /// <summary>
    /// Creates the preview card, toolbar, checkerboard canvas, and zoom controls.
    /// </summary>
    private void InitializePreview()
    {
        PreviewPanel = new RoundedPanel
        {
            AllowDrop = true,
            BackColor = UiTheme.Surface,
            BorderColor = UiTheme.Border,
            BorderRadius = 10,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 0),
            Padding = new Padding(1)
        };

        TableLayoutPanel previewHeader = new()
        {
            BackColor = UiTheme.Surface,
            ColumnCount = 3,
            Dock = DockStyle.Top,
            Height = 50,
            Padding = new Padding(12, 7, 8, 6)
        };
        previewHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125F));
        previewHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        previewHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        Panel previewTitle = new()
        {
            Dock = DockStyle.Fill,
        };
        PictureBox previewIcon = new()
        {
            Image = CreateUiIcon("eye", 16, UiTheme.Primary),
            Location = new Point(3, 9),
            Size = new Size(16, 16),
            SizeMode = PictureBoxSizeMode.CenterImage,
            TabStop = false
        };
        Label previewTitleText = new()
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = UiTheme.TextPrimary,
            Location = new Point(29, 9),
            Text = "PREVIEW"
        };
        previewTitle.Controls.AddRange([previewIcon, previewTitleText]);
        Lbl_PreviewFileName = new Label
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = UiTheme.TextPrimary,
            Text = "No image selected",
            TextAlign = ContentAlignment.MiddleCenter
        };

        FlowLayoutPanel zoomControls = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = Padding.Empty,
            WrapContents = false
        };
        Cmb_Zoom = CreateComboBox();
        Cmb_Zoom.Margin = new Padding(3, 2, 4, 0);
        Cmb_Zoom.Size = new Size(76, 30);
        Cmb_Zoom.SelectedIndexChanged += Cmb_Zoom_SelectedIndexChanged;
        Btn_ZoomOut = CreatePreviewButton("minus");
        Btn_ZoomOut.Click += Btn_ZoomOut_Click;
        Btn_ZoomIn = CreatePreviewButton("plus");
        Btn_ZoomIn.Click += Btn_ZoomIn_Click;
        Btn_Fullscreen = CreatePreviewButton("maximize");
        Btn_Fullscreen.Click += Btn_Fullscreen_Click;
        zoomControls.Controls.AddRange([Cmb_Zoom, Btn_ZoomOut, Btn_ZoomIn, Btn_Fullscreen]);

        previewHeader.Controls.Add(previewTitle, 0, 0);
        previewHeader.Controls.Add(Lbl_PreviewFileName, 1, 0);
        previewHeader.Controls.Add(zoomControls, 2, 0);

        PreviewCanvas = new CheckerboardPanel
        {
            AllowDrop = true,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(18)
        };
        PreviewCanvas.Resize += PreviewCanvas_Resize;
        Pic_Preview = new BeforeAfterViewer
        {
            AllowDrop = true,
            BackColor = Color.Transparent,
            Dock = DockStyle.Fill,
            TabStop = false
        };
        Lbl_DropHint = new Label
        {
            AllowDrop = true,
            BackColor = Color.Transparent,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = UiTheme.TextMuted,
            Text = "Drop images or a folder here\n\nor use Add Images",
            TextAlign = ContentAlignment.MiddleCenter
        };

        AttachPreviewDropEvents(PreviewPanel);
        AttachPreviewDropEvents(PreviewCanvas);
        AttachPreviewDropEvents(Pic_Preview);
        AttachPreviewDropEvents(Lbl_DropHint);
        PreviewCanvas.Controls.Add(Pic_Preview);
        PreviewCanvas.Controls.Add(Lbl_DropHint);
        Lbl_DropHint.BringToFront();
        PreviewPanel.Controls.Add(PreviewCanvas);
        PreviewPanel.Controls.Add(previewHeader);
    }

    /// <summary>
    /// Creates the responsive status bar and compact progress indicator.
    /// </summary>
    private void InitializeStatusBar()
    {
        StatusPanel = new RoundedPanel
        {
            BackColor = UiTheme.Surface,
            BorderColor = UiTheme.BorderSubtle,
            BorderRadius = 9,
            Dock = DockStyle.Fill,
            Margin = new Padding(26, 0, 26, 12),
            Padding = new Padding(1)
        };
        Prg_Process = new ThinProgressBar { Dock = DockStyle.Top, Height = 3 };

        TableLayoutPanel statusLayout = new()
        {
            ColumnCount = 4,
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 3, 12, 2),
            RowCount = 1
        };
        statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));
        statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24F));
        statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
        statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 62F));

        Lbl_Log = new Label
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = UiTheme.TextSecondary,
            Text = "Ready",
            TextAlign = ContentAlignment.MiddleLeft
        };
        Lbl_StatusQueue = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = UiTheme.TextSecondary,
            Text = "Queue is empty",
            TextAlign = ContentAlignment.MiddleCenter
        };
        Lbl_TaskStatus = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = UiTheme.TextSecondary,
            Text = "No active tasks  |  00:00:00",
            TextAlign = ContentAlignment.MiddleRight
        };
        Lbl_ApplicationVersion = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = UiTheme.TextMuted,
            Text = "0.0.0",
            TextAlign = ContentAlignment.MiddleRight
        };
        statusLayout.Controls.Add(Lbl_Log, 0, 0);
        statusLayout.Controls.Add(Lbl_StatusQueue, 1, 0);
        statusLayout.Controls.Add(Lbl_TaskStatus, 2, 0);
        statusLayout.Controls.Add(Lbl_ApplicationVersion, 3, 0);
        StatusPanel.Controls.Add(statusLayout);
        StatusPanel.Controls.Add(Prg_Process);
    }

    /// <summary>
    /// Creates a rounded card with an accent icon and uppercase title.
    /// </summary>
    /// <param name="iconName">The embedded SVG icon name.</param>
    /// <param name="title">The section title.</param>
    /// <param name="height">The card height.</param>
    /// <returns>A styled section card.</returns>
    private RoundedPanel CreateSectionCard(string iconName, string title, int height)
    {
        RoundedPanel card = new()
        {
            BackColor = UiTheme.Surface,
            BorderColor = UiTheme.Border,
            BorderRadius = 9,
            Margin = new Padding(0, 0, 0, 12),
            Size = new Size(286, height)
        };
        PictureBox icon = new()
        {
            Image = CreateUiIcon(iconName, 16, UiTheme.Primary),
            Location = new Point(12, 12),
            Size = new Size(16, 16),
            SizeMode = PictureBoxSizeMode.CenterImage,
            TabStop = false
        };
        Label titleLabel = new()
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            ForeColor = UiTheme.TextPrimary,
            Location = new Point(37, 14),
            Text = title
        };
        card.Controls.AddRange([icon, titleLabel]);
        return card;
    }

    /// <summary>
    /// Adds a label and combo box row to a settings card.
    /// </summary>
    /// <param name="card">The destination settings card.</param>
    /// <param name="labelText">The field label.</param>
    /// <param name="comboBox">The field combo box.</param>
    /// <param name="top">The row's top coordinate.</param>
    private static void AddField(RoundedPanel card, string labelText, ModernComboBox comboBox, int top)
    {
        Label label = new()
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = UiTheme.TextSecondary,
            Location = new Point(12, top + 6),
            Text = labelText
        };
        comboBox.Location = new Point(150, top);
        comboBox.Size = new Size(126, 30);
        card.Controls.AddRange([label, comboBox]);
    }

    /// <summary>
    /// Creates a themed combo box for settings and preview controls.
    /// </summary>
    /// <returns>A dark owner-drawn combo box.</returns>
    private static ModernComboBox CreateComboBox()
    {
        return new ModernComboBox { TabIndex = 0 };
    }

    /// <summary>
    /// Creates a toolbar button with primary or secondary styling.
    /// </summary>
    /// <param name="text">The button text.</param>
    /// <param name="width">The button width.</param>
    /// <param name="iconName">The embedded SVG icon name.</param>
    /// <param name="isPrimary">Whether the button uses the primary accent style.</param>
    /// <returns>A configured toolbar button.</returns>
    private ModernButton CreateToolbarButton(
        string text,
        int width,
        string iconName,
        bool isPrimary = false)
    {
        return new ModernButton
        {
            BackColor = isPrimary ? UiTheme.Primary : UiTheme.Surface,
            BorderColor = isPrimary ? UiTheme.Primary : UiTheme.Border,
            HoverColor = isPrimary ? UiTheme.PrimaryHover : UiTheme.SurfaceHover,
            Image = CreateUiIcon(iconName, 17, isPrimary ? Color.White : UiTheme.TextPrimary),
            Margin = new Padding(5, 0, 5, 0),
            Size = new Size(width, 42),
            Text = text
        };
    }

    /// <summary>
    /// Creates a compact preview toolbar button.
    /// </summary>
    /// <param name="iconName">The embedded SVG icon name.</param>
    /// <returns>A compact themed button.</returns>
    private ModernButton CreatePreviewButton(string iconName)
    {
        return new ModernButton
        {
            BackColor = UiTheme.Surface,
            BorderColor = UiTheme.Border,
            Image = CreateUiIcon(iconName, 15, UiTheme.TextSecondary),
            Margin = new Padding(2, 2, 2, 0),
            Size = new Size(34, 30),
            Text = string.Empty
        };
    }

    /// <summary>
    /// Creates and tracks an SVG icon for the lifetime of the form.
    /// </summary>
    /// <param name="iconName">The embedded SVG icon name.</param>
    /// <param name="size">The raster size in pixels.</param>
    /// <param name="color">The icon color.</param>
    /// <returns>A bitmap owned by the form.</returns>
    private Image CreateUiIcon(string iconName, int size, Color color)
    {
        Image icon = SvgIconProvider.Create(iconName, size, color);
        _uiIcons.Add(icon);
        return icon;
    }

    /// <summary>
    /// Connects drag-and-drop events to a preview control.
    /// </summary>
    /// <param name="control">The control that should accept image drops.</param>
    private void AttachPreviewDropEvents(Control control)
    {
        control.DragDrop += Preview_DragDrop;
        control.DragEnter += Preview_DragEnter;
    }

    #endregion

    private TableLayoutPanel RootLayout = null!;
    private Panel HeaderPanel = null!;
    private ModernButton Btn_Donate = null!;
    private ModernButton Btn_AddImages = null!;
    private ModernButton Btn_Upscale = null!;
    private ModernButton Btn_Clear = null!;
    private ModernButton Btn_ToggleSettings = null!;
    private Label Lbl_Subtitle = null!;
    private Label Lbl_Title = null!;
    private TableLayoutPanel ContentLayout = null!;
    private Panel SettingsPanel = null!;
    private FlowLayoutPanel SettingsFlow = null!;
    private RoundedPanel Grp_Queue = null!;
    private ListBox Lst_Images = null!;
    private Label Lbl_QueueCount = null!;
    private RoundedBadge Lbl_QueueBadge = null!;
    private ModernButton Btn_RemoveSelected = null!;
    private RoundedPanel Grp_Model = null!;
    private ModernComboBox Cmb_AI_Model = null!;
    private ModernButton Btn_ImportModel = null!;
    private RoundedPanel Grp_Performance = null!;
    private ModernComboBox Cmb_GPU_Id = null!;
    private ModernComboBox Cmb_TileSize = null!;
    private ModernComboBox Cmb_ConcurrentJobs = null!;
    private RoundedPanel Grp_Advanced = null!;
    private ModernComboBox Cmb_GPU_ThreadsSave = null!;
    private ModernComboBox Cmb_GPU_ThreadsProcess = null!;
    private ModernComboBox Cmb_GPU_ThreadsLoad = null!;
    private RoundedPanel Grp_Output = null!;
    private ModernCheckBox Chk_UseSuffix = null!;
    private ModernComboBox Cmb_FileExtension = null!;
    private RoundedPanel PreviewPanel = null!;
    private CheckerboardPanel PreviewCanvas = null!;
    private BeforeAfterViewer Pic_Preview = null!;
    private Label Lbl_DropHint = null!;
    private Label Lbl_PreviewFileName = null!;
    private ModernComboBox Cmb_Zoom = null!;
    private ModernButton Btn_ZoomOut = null!;
    private ModernButton Btn_ZoomIn = null!;
    private ModernButton Btn_Fullscreen = null!;
    private RoundedPanel StatusPanel = null!;
    private ThinProgressBar Prg_Process = null!;
    private Label Lbl_Log = null!;
    private Label Lbl_StatusQueue = null!;
    private Label Lbl_TaskStatus = null!;
    private Label Lbl_ApplicationVersion = null!;
    private ToolTip MainToolTip = null!;
    private System.Windows.Forms.Timer ElapsedTimer = null!;
    private Image _startUpscaleIcon = null!;
    private Image _cancelUpscaleIcon = null!;
    private Image _queueCheckIcon = null!;
}
