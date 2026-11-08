using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Reflection;
using SharpForge.Configuration;
using SharpForge.Controls;
using SharpForge.Models;
using SharpForge.Services;
using SharpForge.Source;

namespace SharpForge;

/// <summary>
/// Presents the image queue, upscaling settings, progress, and preview workflow.
/// </summary>
public sealed partial class FormMain : Form
{
    private static readonly int[] ThreadCounts = [1, 2, 3, 4, 5, 6, 7, 8];
    private static readonly int[] TileSizes = [32, 64, 128, 256, 512, 1024];
    private const string DefaultModelName = "realesrgan-x4plus";
    private static readonly string[] OutputFormats = ["png", "jpg", "dds"];
    private static readonly string[] ZoomLevels = ["Fit", "25%", "50%", "75%", "100%", "125%", "150%", "200%", "400%"];

    private readonly List<string> _imagePaths = [];
    private readonly List<Image> _uiIcons = [];
    private readonly Dictionary<string, Image> _queueThumbnails = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _upscaledPaths = new(StringComparer.OrdinalIgnoreCase);
    private readonly Stopwatch _batchStopwatch = new();
    private readonly ImageUpscalerRouter _upscaler;
    private readonly UpscaleBatchProcessor _batchProcessor;
    private readonly ModelDiscoveryService _modelDiscovery;
    private readonly ModelImportService _modelImporter;

    private Image? _currentPreview;
    private Image? _currentAfterPreview;
    private CancellationTokenSource? _cancellationTokenSource;
    private CancellationTokenSource? _modelImportCancellationTokenSource;
    private bool _closeAfterCancellation;
    private bool _isImportingModel;
    private bool _isProcessing;
    private bool _settingsVisible = true;

    /// <summary>
    /// Initializes the main application window with default runtime services.
    /// </summary>
    public FormMain()
        : this([])
    {
    }

    /// <summary>
    /// Initializes the main application window and queues optional startup paths.
    /// </summary>
    /// <param name="startupPaths">Image or directory paths supplied by the operating system.</param>
    internal FormMain(IEnumerable<string> startupPaths)
    {
        AppPaths paths = AppPaths.CreateDefault();
        OptionalBackendInstaller optionalBackendInstaller = new(paths);
        _upscaler = new ImageUpscalerRouter(
            new RealEsrganUpscaler(paths),
            new PyTorchUpscaler(paths, optionalBackendInstaller));
        _batchProcessor = new UpscaleBatchProcessor(_upscaler);
        _modelDiscovery = new ModelDiscoveryService(paths);
        _modelImporter = new ModelImportService(paths, optionalBackendInstaller);

        InitializeComponent();
        InitializeView();
        AddImages(startupPaths);

        if (Cmb_AI_Model.ItemCount == 0)
        {
            Lbl_Log.Text = "No usable models are installed. Import a model or run Build\\Setup.bat.";
            Lbl_Log.ForeColor = UiTheme.Danger;
        }
        else if (_imagePaths.Count == 0)
        {
            Lbl_Log.Text = "Ready";
            Lbl_Log.ForeColor = UiTheme.Success;
        }
    }

    /// <summary>
    /// Populates controls with safe defaults and displays the application version.
    /// </summary>
    private void InitializeView()
    {
        NativeWindowTheme.Apply(this);
        Version? version = Assembly.GetExecutingAssembly().GetName().Version;
        Lbl_ApplicationVersion.Text = version?.ToString(3) ?? "0.0.0";

        RefreshModelList(DefaultModelName);
        ConfigureComboBox(Cmb_ConcurrentJobs, ThreadCounts, 0);
        ConfigureComboBox(Cmb_TileSize, TileSizes, 2);
        ConfigureComboBox(Cmb_GPU_Id, [0], 0);
        ConfigureComboBox(Cmb_GPU_ThreadsLoad, ThreadCounts, 1);
        ConfigureComboBox(Cmb_GPU_ThreadsProcess, ThreadCounts, 1);
        ConfigureComboBox(Cmb_GPU_ThreadsSave, ThreadCounts, 1);
        ConfigureComboBox(Cmb_FileExtension, OutputFormats, 0);
        ConfigureComboBox(Cmb_Zoom, ZoomLevels, 0);
        NativeWindowTheme.ApplyToControl(SettingsFlow);
        NativeWindowTheme.ApplyToControl(Lst_Images);

        Cmb_AI_Model.SelectedIndexChanged += PreviewSettings_SelectedIndexChanged;
        Cmb_FileExtension.SelectedIndexChanged += PreviewSettings_SelectedIndexChanged;
        Chk_UseSuffix.CheckedChanged += PreviewSettings_SelectedIndexChanged;
    }

    /// <summary>
    /// Reloads usable model pairs and selects a preferred model when available.
    /// </summary>
    /// <param name="preferredModelName">The model name to select, or <see langword="null"/> to keep the current selection.</param>
    private void RefreshModelList(string? preferredModelName = null)
    {
        string? currentName = (Cmb_AI_Model.SelectedItem as UpscaleModel)?.Name;
        UpscaleModel[] models = _modelDiscovery.GetAvailableModels().ToArray();
        string? selectionName = preferredModelName ?? currentName;
        int selectedIndex = Array.FindIndex(
            models,
            model => string.Equals(model.Name, selectionName, StringComparison.OrdinalIgnoreCase));
        ConfigureComboBox(Cmb_AI_Model, models, selectedIndex >= 0 ? selectedIndex : 0);
    }

    /// <summary>
    /// Replaces the contents of a combo box and selects a bounded default index.
    /// </summary>
    /// <typeparam name="T">The type displayed by the combo box.</typeparam>
    /// <param name="comboBox">The combo box to configure.</param>
    /// <param name="items">The values to display.</param>
    /// <param name="defaultIndex">The preferred selected index.</param>
    private static void ConfigureComboBox<T>(
        ModernComboBox comboBox,
        IEnumerable<T> items,
        int defaultIndex)
    {
        object[] values = items.Cast<object>().ToArray();
        comboBox.SetItems(values);

        if (values.Length > 0)
        {
            comboBox.SelectedIndex = Math.Clamp(defaultIndex, 0, values.Length - 1);
        }
    }

    /// <summary>
    /// Opens a file picker and appends the selected images to the queue.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void Btn_AddImages_Click(object? sender, EventArgs e)
    {
        using OpenFileDialog dialog = new()
        {
            AddExtension = true,
            CheckFileExists = true,
            Filter = "Supported images|*.bmp;*.gif;*.jpeg;*.jpg;*.png;*.tif;*.tiff;*.webp|All files|*.*",
            Multiselect = true,
            RestoreDirectory = true,
            Title = "Select images to upscale"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            AddImages(dialog.FileNames);
        }
    }

    /// <summary>
    /// Opens a model picker, imports or converts the selected model, and refreshes the model list.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event data.</param>
    private async void Btn_ImportModel_Click(object? sender, EventArgs e)
    {
        if (_isProcessing || _isImportingModel)
        {
            return;
        }

        using OpenFileDialog dialog = new()
        {
            AddExtension = true,
            CheckFileExists = true,
            Filter = "Upscaling models|*.pth;*.safetensors;*.param;*.bin|PyTorch checkpoints|*.pth;*.safetensors|NCNN model files|*.param;*.bin|All files|*.*",
            Multiselect = false,
            RestoreDirectory = true,
            Title = "Import an upscaling model"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        if (string.Equals(Path.GetExtension(dialog.FileName), ".pth", StringComparison.OrdinalIgnoreCase) &&
            MessageBox.Show(
                this,
                "PyTorch .pth files can contain executable pickle data. Import this checkpoint only if you trust its source. Continue?",
                "Trust this model?",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes)
        {
            return;
        }

        Btn_ImportModel.Enabled = false;
        SettingsFlow.Enabled = false;
        _isImportingModel = true;
        CancellationTokenSource importCancellation = new();
        _modelImportCancellationTokenSource = importCancellation;
        Progress<string> progress = new(message =>
        {
            Lbl_Log.Text = message;
            Lbl_Log.ForeColor = UiTheme.Primary;
        });

        try
        {
            UpscaleModel importedModel;
            try
            {
                importedModel = await _modelImporter.ImportAsync(
                    dialog.FileName,
                    progress,
                    installOptionalBackend: false,
                    importCancellation.Token);
            }
            catch (OptionalBackendRequiredException exception)
            {
                DialogResult installChoice = MessageBox.Show(
                    this,
                    $"This {exception.Architecture} model requires the optional PyTorch CUDA backend. " +
                    "It downloads an isolated Python, PyTorch CUDA, and Spandrel runtime (several gigabytes) " +
                    "into your local Sharp Forge application data. Install it now?",
                    "Install optional CUDA backend?",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (installChoice != DialogResult.Yes)
                {
                    Lbl_Log.Text = "Optional backend installation was declined.";
                    Lbl_Log.ForeColor = UiTheme.Primary;
                    return;
                }

                importedModel = await _modelImporter.ImportAsync(
                    dialog.FileName,
                    progress,
                    installOptionalBackend: true,
                    importCancellation.Token);
            }

            RefreshModelList(importedModel.Name);
            UpdateQueueState(0);
            RefreshSelectedPreviewComparison();
            Lbl_Log.Text = $"Imported model {importedModel.Name}.";
            Lbl_Log.ForeColor = UiTheme.Success;
        }
        catch (OperationCanceledException)
        {
            Lbl_Log.Text = "Model import canceled.";
            Lbl_Log.ForeColor = UiTheme.Primary;
        }
        catch (Exception exception) when (exception is ArgumentException or FileNotFoundException or InvalidDataException or InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            ShowError("The model could not be imported.", exception.Message);
            Lbl_Log.Text = "Model import failed.";
            Lbl_Log.ForeColor = UiTheme.Danger;
        }
        finally
        {
            importCancellation.Dispose();
            _modelImportCancellationTokenSource = null;
            _isImportingModel = false;
            Btn_ImportModel.Enabled = true;
            SettingsFlow.Enabled = true;

            if (_closeAfterCancellation)
            {
                Close();
            }
        }
    }

    /// <summary>
    /// Adds unique supported images and updates the queue controls.
    /// </summary>
    /// <param name="paths">The selected files or directories.</param>
    private void AddImages(IEnumerable<string> paths)
    {
        IReadOnlyCollection<string> discoveredImages;

        try
        {
            discoveredImages = ImageFileService.FindImages(paths);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ShowError("Some files could not be read.", exception.Message);
            return;
        }

        int addedCount = 0;
        int invalidCount = 0;
        foreach (string imagePath in discoveredImages)
        {
            if (_imagePaths.Contains(imagePath, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            Bitmap thumbnail;
            try
            {
                thumbnail = CreateQueueThumbnail(imagePath);
            }
            catch (Exception exception) when (exception is ArgumentException or IOException or System.Runtime.InteropServices.ExternalException)
            {
                invalidCount++;
                continue;
            }

            _imagePaths.Add(imagePath);
            _queueThumbnails[imagePath] = thumbnail;
            Lst_Images.Items.Add(imagePath);
            addedCount++;
        }

        UpdateQueueState(addedCount);

        if (invalidCount > 0)
        {
            Lbl_Log.Text = $"Skipped {invalidCount} invalid or unreadable image{(invalidCount == 1 ? string.Empty : "s")}.";
            Lbl_Log.ForeColor = UiTheme.Danger;
        }
    }

    /// <summary>
    /// Updates queue counters, controls, and the selected preview item.
    /// </summary>
    /// <param name="addedCount">The number of newly queued images.</param>
    private void UpdateQueueState(int addedCount)
    {
        int imageCount = _imagePaths.Count;
        Lbl_QueueCount.Text = imageCount == 0
            ? "Queue is empty"
            : $"{imageCount} image{(imageCount == 1 ? string.Empty : "s")} in queue";
        Lbl_QueueBadge.Text = imageCount.ToString(CultureInfo.InvariantCulture);
        Lbl_StatusQueue.Text = imageCount == 0
            ? "Queue is empty"
            : $"{imageCount} image{(imageCount == 1 ? string.Empty : "s")} in queue";

        Btn_Clear.Enabled = imageCount > 0 && !_isProcessing;
        Btn_Upscale.Enabled = _isProcessing || (imageCount > 0 && Cmb_AI_Model.SelectedItem is UpscaleModel);
        Btn_RemoveSelected.Enabled = Lst_Images.SelectedIndex >= 0 && !_isProcessing;

        if (addedCount > 0)
        {
            Lbl_Log.Text = $"Added {addedCount} image{(addedCount == 1 ? string.Empty : "s")}.";
            Lbl_Log.ForeColor = UiTheme.Success;
            Lst_Images.SelectedIndex = Lst_Images.Items.Count - addedCount;
        }
        else if (imageCount > 0)
        {
            Lbl_Log.Text = "No new supported images were found.";
            Lbl_Log.ForeColor = UiTheme.TextSecondary;
        }
        else
        {
            Lbl_Log.Text = "No supported images were found.";
            Lbl_Log.ForeColor = UiTheme.TextSecondary;
        }
    }

    /// <summary>
    /// Draws a modern queue row with a thumbnail, file name, directory, and selection state.
    /// </summary>
    /// <param name="sender">The queue control that raised the event.</param>
    /// <param name="e">The item drawing event data.</param>
    private void Lst_Images_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= Lst_Images.Items.Count)
        {
            return;
        }

        string imagePath = (string)Lst_Images.Items[e.Index];
        bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        Rectangle rowBounds = Rectangle.Inflate(e.Bounds, -3, -3);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using GraphicsPath rowPath = UiGeometry.CreateRoundedRectangle(rowBounds, 7);
        using SolidBrush rowBrush = new(selected ? Color.FromArgb(28, 63, 108) : UiTheme.Surface);
        e.Graphics.FillPath(rowBrush, rowPath);

        Rectangle thumbnailBounds = new(rowBounds.X + 7, rowBounds.Y + 7, 48, 48);
        if (_queueThumbnails.TryGetValue(imagePath, out Image? thumbnail))
        {
            e.Graphics.DrawImage(thumbnail, thumbnailBounds);
        }

        Rectangle nameBounds = new(rowBounds.X + 66, rowBounds.Y + 10, rowBounds.Width - 98, 21);
        Rectangle directoryBounds = new(rowBounds.X + 66, rowBounds.Y + 33, rowBounds.Width - 98, 18);
        using Font nameFont = new(Lst_Images.Font, FontStyle.Bold);
        TextRenderer.DrawText(
            e.Graphics,
            Path.GetFileName(imagePath),
            nameFont,
            nameBounds,
            UiTheme.TextPrimary,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        TextRenderer.DrawText(
            e.Graphics,
            Path.GetDirectoryName(imagePath) ?? string.Empty,
            Font,
            directoryBounds,
            UiTheme.TextSecondary,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

        if (selected)
        {
            Rectangle checkBounds = new(rowBounds.Right - 25, rowBounds.Y + 21, 17, 17);
            e.Graphics.DrawImage(_queueCheckIcon, checkBounds);
        }
    }

    /// <summary>
    /// Creates a small, aspect-preserving thumbnail for the queue.
    /// </summary>
    /// <param name="imagePath">The source image path.</param>
    /// <returns>A detached thumbnail owned by the form.</returns>
    private static Bitmap CreateQueueThumbnail(string imagePath)
    {
        using Image source = ImagePreviewLoader.Load(imagePath);
        Bitmap thumbnail = new(48, 48);
        using Graphics graphics = Graphics.FromImage(thumbnail);
        graphics.Clear(UiTheme.SurfaceHover);
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = SmoothingMode.HighQuality;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        Rectangle destination = CalculateAspectFit(source.Size, thumbnail.Size);
        graphics.DrawImage(source, destination);
        return thumbnail;
    }

    /// <summary>
    /// Calculates an aspect-preserving rectangle centered inside a target size.
    /// </summary>
    /// <param name="sourceSize">The original image size.</param>
    /// <param name="targetSize">The available drawing size.</param>
    /// <returns>The centered aspect-fit rectangle.</returns>
    private static Rectangle CalculateAspectFit(Size sourceSize, Size targetSize)
    {
        double scale = Math.Min(
            targetSize.Width / (double)sourceSize.Width,
            targetSize.Height / (double)sourceSize.Height);
        int width = Math.Max(1, (int)Math.Round(sourceSize.Width * scale));
        int height = Math.Max(1, (int)Math.Round(sourceSize.Height * scale));
        return new Rectangle((targetSize.Width - width) / 2, (targetSize.Height - height) / 2, width, height);
    }

    /// <summary>
    /// Sets the drag cursor when supported files or a directory are present.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The drag event data.</param>
    private void Preview_DragEnter(object? sender, DragEventArgs e)
    {
        if (_isProcessing || e.Data?.GetData(DataFormats.FileDrop) is not string[] paths)
        {
            e.Effect = DragDropEffects.None;
            return;
        }

        e.Effect = paths.Any(path => ImageFileService.IsSupported(path) || Directory.Exists(path))
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    /// <summary>
    /// Adds dropped image files or the images directly inside a dropped directory.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The drag event data.</param>
    private void Preview_DragDrop(object? sender, DragEventArgs e)
    {
        if (!_isProcessing && e.Data?.GetData(DataFormats.FileDrop) is string[] paths)
        {
            AddImages(paths);
        }
    }

    /// <summary>
    /// Loads a detached preview for the selected queue item.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void Lst_Images_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (Lst_Images.SelectedItem is not string imagePath)
        {
            return;
        }

        try
        {
            Image preview = ImagePreviewLoader.Load(imagePath);
            Image? afterPreview = TryLoadAfterPreview(imagePath);
            Image? previousPreview = _currentPreview;
            Image? previousAfterPreview = _currentAfterPreview;
            _currentPreview = preview;
            _currentAfterPreview = afterPreview;
            Pic_Preview.BeforeImage = preview;
            Pic_Preview.AfterImage = afterPreview;
            previousPreview?.Dispose();
            previousAfterPreview?.Dispose();
            Lbl_DropHint.Visible = false;
            Lbl_PreviewFileName.Text = Path.GetFileName(imagePath);
            ApplySelectedZoom();
            Btn_RemoveSelected.Enabled = !_isProcessing;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            ShowError("The selected image could not be previewed.", exception.Message);
        }
    }

    /// <summary>
    /// Loads the current upscaled output for an input image when that output exists and can be previewed.
    /// </summary>
    /// <param name="inputPath">The selected source image path.</param>
    /// <returns>A detached after image, or <see langword="null"/> when no previewable output exists.</returns>
    private Bitmap? TryLoadAfterPreview(string inputPath)
    {
        string? outputPath = null;
        if (Cmb_AI_Model.SelectedItem is UpscaleModel)
        {
            string candidatePath = OutputPathBuilder.Build(inputPath, CaptureOptions());
            if (File.Exists(candidatePath))
            {
                outputPath = candidatePath;
            }
        }
        else if (_upscaledPaths.TryGetValue(inputPath, out string? recordedPath) && File.Exists(recordedPath))
        {
            outputPath = recordedPath;
        }

        if (outputPath is null)
        {
            return null;
        }

        try
        {
            return ImagePreviewLoader.Load(outputPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidDataException)
        {
            return null;
        }
    }

    /// <summary>
    /// Reloads the selected queue item so the comparison follows model and output setting changes.
    /// </summary>
    private void RefreshSelectedPreviewComparison()
    {
        if (Lst_Images.SelectedItem is string)
        {
            Lst_Images_SelectedIndexChanged(Lst_Images, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Refreshes the comparison when a setting changes the expected output path.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void PreviewSettings_SelectedIndexChanged(object? sender, EventArgs e)
    {
        RefreshSelectedPreviewComparison();
        Btn_Upscale.Enabled = _isProcessing ||
            (_imagePaths.Count > 0 && Cmb_AI_Model.SelectedItem is UpscaleModel);
    }

    /// <summary>
    /// Clears the queue, preview, status, and progress controls.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void Btn_Clear_Click(object? sender, EventArgs e)
    {
        _imagePaths.Clear();
        _upscaledPaths.Clear();
        Lst_Images.Items.Clear();
        DisposeQueueThumbnails();
        ClearPreview();
        Prg_Process.Value = 0;
        UpdateQueueState(0);
        Lbl_Log.Text = "Ready";
        Lbl_Log.ForeColor = UiTheme.Success;
        Lbl_TaskStatus.Text = "No active tasks  |  00:00:00";
    }

    /// <summary>
    /// Removes the selected queue item and selects the nearest remaining image.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void Btn_RemoveSelected_Click(object? sender, EventArgs e)
    {
        int selectedIndex = Lst_Images.SelectedIndex;
        if (_isProcessing || selectedIndex < 0)
        {
            return;
        }

        string imagePath = (string)Lst_Images.Items[selectedIndex];
        _imagePaths.Remove(imagePath);
        _upscaledPaths.Remove(imagePath);
        Lst_Images.Items.RemoveAt(selectedIndex);

        if (_queueThumbnails.Remove(imagePath, out Image? thumbnail))
        {
            thumbnail.Dispose();
        }

        if (Lst_Images.Items.Count > 0)
        {
            Lst_Images.SelectedIndex = Math.Min(selectedIndex, Lst_Images.Items.Count - 1);
        }
        else
        {
            ClearPreview();
        }

        UpdateQueueState(0);
        Lbl_Log.Text = "Removed image from queue.";
        Lbl_Log.ForeColor = UiTheme.TextSecondary;
    }

    /// <summary>
    /// Starts a batch operation or requests cancellation for the active operation.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event data.</param>
    private async void Btn_Upscale_Click(object? sender, EventArgs e)
    {
        if (_isProcessing)
        {
            RequestCancellation();
            return;
        }

        if (_imagePaths.Count == 0)
        {
            MessageBox.Show(
                this,
                "Add at least one supported image before starting.",
                "No images",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        if (Cmb_AI_Model.SelectedItem is not UpscaleModel)
        {
            MessageBox.Show(
                this,
                "Import or install a usable AI model before starting.",
                "No AI model",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        UpscaleOptions options = CaptureOptions();
        if (!_upscaler.IsAvailable(options.Backend))
        {
            MessageBox.Show(
                this,
                _upscaler.GetUnavailableMessage(options.Backend),
                "Upscaling backend not found",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        string[] inputPaths = _imagePaths.ToArray();
        _cancellationTokenSource = new CancellationTokenSource();
        SetProcessingState(isProcessing: true, inputPaths.Length);

        Progress<UpscaleProgress> progress = new(UpdateProgress);

        try
        {
            UpscaleBatchResult result = await _batchProcessor.ProcessAsync(
                inputPaths,
                options,
                progress,
                _cancellationTokenSource.Token);

            DisplayBatchResult(result, inputPaths.Length, _cancellationTokenSource.IsCancellationRequested);
        }
        catch (Exception exception)
        {
            ShowError("The batch stopped because of an unexpected error.", exception.Message);
        }
        finally
        {
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
            SetProcessingState(isProcessing: false, inputPaths.Length);

            if (_closeAfterCancellation)
            {
                Close();
            }
        }
    }

    /// <summary>
    /// Captures all UI settings on the UI thread before background work starts.
    /// </summary>
    /// <returns>Immutable settings for the next batch.</returns>
    private UpscaleOptions CaptureOptions()
    {
        UpscaleModel model = Cmb_AI_Model.SelectedItem as UpscaleModel
            ?? throw new InvalidOperationException("No usable AI model is selected.");

        return new UpscaleOptions(
            model.Name,
            model.DirectoryPath,
            model.ModelPath,
            model.Backend,
            Cmb_FileExtension.SelectedItem?.ToString() ?? "png",
            GetSelectedInt(Cmb_TileSize, 128),
            GetSelectedInt(Cmb_GPU_Id, 0),
            GetSelectedInt(Cmb_GPU_ThreadsLoad, 2),
            GetSelectedInt(Cmb_GPU_ThreadsProcess, 2),
            GetSelectedInt(Cmb_GPU_ThreadsSave, 2),
            GetSelectedInt(Cmb_ConcurrentJobs, 1),
            Chk_UseSuffix.Checked);
    }

    /// <summary>
    /// Reads an integer selection with a fallback value.
    /// </summary>
    /// <param name="comboBox">The combo box to read.</param>
    /// <param name="fallback">The value used when no valid selection exists.</param>
    /// <returns>The selected integer or the fallback value.</returns>
    private static int GetSelectedInt(ModernComboBox comboBox, int fallback)
    {
        return comboBox.SelectedItem is int value ? value : fallback;
    }

    /// <summary>
    /// Updates progress controls for the latest completed, skipped, failed, or canceled job.
    /// </summary>
    /// <param name="progress">The aggregate batch progress.</param>
    private void UpdateProgress(UpscaleProgress progress)
    {
        Prg_Process.Value = Math.Clamp(progress.ProcessedCount, 0, Prg_Process.Maximum);

        string action = progress.Status switch
        {
            UpscaleStatus.Completed => "Upscaled",
            UpscaleStatus.Skipped => "Skipped existing output for",
            UpscaleStatus.Failed => "Failed",
            UpscaleStatus.Canceled => "Canceled",
            _ => "Processed"
        };

        Lbl_Log.Text = $"{action} {progress.CurrentFileName} ({progress.ProcessedCount}/{progress.TotalCount}).";
        Lbl_Log.ForeColor = progress.Status == UpscaleStatus.Failed ? UiTheme.Danger : UiTheme.Primary;
        Lbl_TaskStatus.Text = $"Processing {progress.ProcessedCount}/{progress.TotalCount}  |  {FormatElapsedTime()}";
    }

    /// <summary>
    /// Displays the aggregate outcome and concise failure details for a batch.
    /// </summary>
    /// <param name="result">The completed batch result.</param>
    /// <param name="totalCount">The total number of requested jobs.</param>
    /// <param name="wasCanceled">Whether cancellation was requested by the user.</param>
    private void DisplayBatchResult(UpscaleBatchResult result, int totalCount, bool wasCanceled)
    {
        foreach (UpscaleResult item in result.Results.Where(item =>
                     (item.Status is UpscaleStatus.Completed or UpscaleStatus.Skipped) && File.Exists(item.OutputPath)))
        {
            _upscaledPaths[item.InputPath] = item.OutputPath;
        }

        RefreshSelectedPreviewComparison();

        if (wasCanceled)
        {
            Lbl_Log.Text = $"Canceled. {result.Results.Count}/{totalCount} images reached a final state.";
            Lbl_Log.ForeColor = UiTheme.Danger;
            return;
        }

        Lbl_Log.Text = $"Done in {result.Elapsed.TotalSeconds:F1}s - {result.CompletedCount} completed, {result.SkippedCount} skipped, {result.FailedCount} failed.";
        Lbl_Log.ForeColor = result.FailedCount > 0 ? UiTheme.Danger : UiTheme.Success;

        if (result.FailedCount == 0)
        {
            return;
        }

        string details = string.Join(
            Environment.NewLine + Environment.NewLine,
            result.Results
                .Where(item => item.Status == UpscaleStatus.Failed)
                .Take(5)
                .Select(item => $"{Path.GetFileName(item.InputPath)}: {item.ErrorMessage}"));

        ShowError($"{result.FailedCount} image(s) could not be upscaled.", details);
    }

    /// <summary>
    /// Enables or disables controls to reflect the active batch state.
    /// </summary>
    /// <param name="isProcessing">Whether a batch is currently active.</param>
    /// <param name="totalCount">The number of jobs in the active batch.</param>
    private void SetProcessingState(bool isProcessing, int totalCount)
    {
        _isProcessing = isProcessing;
        Btn_AddImages.Enabled = !isProcessing;
        Btn_Clear.Enabled = !isProcessing && _imagePaths.Count > 0;
        Btn_RemoveSelected.Enabled = !isProcessing && Lst_Images.SelectedIndex >= 0;
        Btn_ImportModel.Enabled = !isProcessing;
        SettingsFlow.Enabled = !isProcessing;
        AllowDrop = !isProcessing;
        PreviewPanel.AllowDrop = !isProcessing;
        Pic_Preview.AllowDrop = !isProcessing;

        Btn_Upscale.Enabled = isProcessing || (_imagePaths.Count > 0 && Cmb_AI_Model.SelectedItem is UpscaleModel);
        Btn_Upscale.Text = isProcessing ? "Cancel Upscaling" : "Start Upscaling";
        Btn_Upscale.Image = isProcessing ? _cancelUpscaleIcon : _startUpscaleIcon;
        Btn_Upscale.BackColor = isProcessing
            ? UiTheme.Danger
            : UiTheme.Primary;
        Btn_Upscale.BorderColor = isProcessing ? UiTheme.Danger : UiTheme.Primary;
        Btn_Upscale.HoverColor = isProcessing ? UiTheme.DangerHover : UiTheme.PrimaryHover;

        if (isProcessing)
        {
            Prg_Process.Maximum = Math.Max(1, totalCount);
            Prg_Process.Value = 0;
            _batchStopwatch.Restart();
            ElapsedTimer.Start();
            Lbl_Log.Text = $"Starting {totalCount} image{(totalCount == 1 ? string.Empty : "s")}...";
            Lbl_Log.ForeColor = UiTheme.Primary;
            Lbl_TaskStatus.Text = $"Processing 0/{totalCount}  |  00:00:00";
        }
        else
        {
            ElapsedTimer.Stop();
            _batchStopwatch.Stop();
            Lbl_TaskStatus.Text = $"No active tasks  |  {FormatElapsedTime()}";
        }
    }

    /// <summary>
    /// Requests batch cancellation and prevents duplicate cancellation requests.
    /// </summary>
    private void RequestCancellation()
    {
        _cancellationTokenSource?.Cancel();
        Btn_Upscale.Enabled = false;
        Btn_Upscale.Text = "Canceling...";
        Btn_Upscale.Image = _cancelUpscaleIcon;
        Lbl_Log.Text = "Canceling active processes...";
        Lbl_Log.ForeColor = UiTheme.Danger;
        Lbl_TaskStatus.Text = $"Canceling task  |  {FormatElapsedTime()}";
    }

    /// <summary>
    /// Releases and removes the current preview image.
    /// </summary>
    private void ClearPreview()
    {
        Pic_Preview.BeforeImage = null;
        Pic_Preview.AfterImage = null;
        _currentPreview?.Dispose();
        _currentPreview = null;
        _currentAfterPreview?.Dispose();
        _currentAfterPreview = null;
        Pic_Preview.Dock = DockStyle.Fill;
        Lbl_DropHint.Visible = true;
        Lbl_PreviewFileName.Text = "No image selected";
        Cmb_Zoom.SelectedIndex = 0;
    }

    /// <summary>
    /// Applies the selected fit or numeric zoom level to the preview image.
    /// </summary>
    private void ApplySelectedZoom()
    {
        if (_currentPreview is null || Cmb_Zoom.SelectedItem is not string selectedZoom)
        {
            return;
        }

        if (selectedZoom == "Fit")
        {
            PreviewCanvas.AutoScroll = false;
            Pic_Preview.Dock = DockStyle.Fill;
            return;
        }

        if (!int.TryParse(selectedZoom.TrimEnd('%'), out int zoomPercent))
        {
            return;
        }

        PreviewCanvas.AutoScroll = true;
        PreviewCanvas.AutoScrollPosition = Point.Empty;
        Pic_Preview.Dock = DockStyle.None;
        Pic_Preview.Size = new Size(
            Math.Max(1, _currentPreview.Width * zoomPercent / 100),
            Math.Max(1, _currentPreview.Height * zoomPercent / 100));
        CenterPreviewImage();
    }

    /// <summary>
    /// Centers a numeric-zoom preview when it is smaller than the available canvas.
    /// </summary>
    private void CenterPreviewImage()
    {
        if (Pic_Preview.Dock == DockStyle.Fill)
        {
            return;
        }

        int x = Math.Max(PreviewCanvas.Padding.Left, (PreviewCanvas.ClientSize.Width - Pic_Preview.Width) / 2);
        int y = Math.Max(PreviewCanvas.Padding.Top, (PreviewCanvas.ClientSize.Height - Pic_Preview.Height) / 2);
        Pic_Preview.Location = new Point(x, y);
    }

    /// <summary>
    /// Applies a newly selected zoom level.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void Cmb_Zoom_SelectedIndexChanged(object? sender, EventArgs e)
    {
        ApplySelectedZoom();
    }

    /// <summary>
    /// Selects the previous numeric zoom level.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void Btn_ZoomOut_Click(object? sender, EventArgs e)
    {
        Cmb_Zoom.SelectedIndex = Math.Max(1, Cmb_Zoom.SelectedIndex - 1);
    }

    /// <summary>
    /// Selects the next numeric zoom level.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void Btn_ZoomIn_Click(object? sender, EventArgs e)
    {
        Cmb_Zoom.SelectedIndex = Math.Min(Cmb_Zoom.ItemCount - 1, Math.Max(1, Cmb_Zoom.SelectedIndex + 1));
    }

    /// <summary>
    /// Opens the current image in a distraction-free fullscreen preview.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void Btn_Fullscreen_Click(object? sender, EventArgs e)
    {
        if (_currentPreview is null)
        {
            return;
        }

        using Bitmap fullscreenImage = new(_currentPreview);
        using Bitmap? fullscreenAfterImage = _currentAfterPreview is null
            ? null
            : new Bitmap(_currentAfterPreview);
        using Form fullscreenForm = new()
        {
            BackColor = UiTheme.Window,
            FormBorderStyle = FormBorderStyle.None,
            KeyPreview = true,
            StartPosition = FormStartPosition.CenterParent,
            WindowState = FormWindowState.Maximized
        };
        BeforeAfterViewer fullscreenPreview = new()
        {
            BackColor = UiTheme.Window,
            Dock = DockStyle.Fill,
            BeforeImage = fullscreenImage,
            AfterImage = fullscreenAfterImage
        };
        fullscreenForm.KeyDown += (_, keyEvent) =>
        {
            if (keyEvent.KeyCode == Keys.Escape)
            {
                fullscreenForm.Close();
            }
        };
        fullscreenForm.Controls.Add(fullscreenPreview);
        fullscreenForm.ShowDialog(this);
    }

    /// <summary>
    /// Re-centers a numeric-zoom image when the preview canvas changes size.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void PreviewCanvas_Resize(object? sender, EventArgs e)
    {
        CenterPreviewImage();
    }

    /// <summary>
    /// Shows or hides the settings sidebar without affecting the preview workspace.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void Btn_ToggleSettings_Click(object? sender, EventArgs e)
    {
        _settingsVisible = !_settingsVisible;
        SettingsPanel.Visible = _settingsVisible;
        ContentLayout.ColumnStyles[0].Width = _settingsVisible ? 320F : 0F;
        MainToolTip.SetToolTip(
            Btn_ToggleSettings,
            _settingsVisible ? "Hide the settings sidebar." : "Show the settings sidebar.");
    }

    /// <summary>
    /// Updates the elapsed time displayed for the active batch.
    /// </summary>
    /// <param name="sender">The timer that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void ElapsedTimer_Tick(object? sender, EventArgs e)
    {
        Lbl_TaskStatus.Text = $"Processing queue  |  {FormatElapsedTime()}";
    }

    /// <summary>
    /// Formats the current batch elapsed time as hours, minutes, and seconds.
    /// </summary>
    /// <returns>The formatted elapsed time.</returns>
    private string FormatElapsedTime()
    {
        return _batchStopwatch.Elapsed.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Disposes every cached queue thumbnail.
    /// </summary>
    private void DisposeQueueThumbnails()
    {
        foreach (Image thumbnail in _queueThumbnails.Values)
        {
            thumbnail.Dispose();
        }

        _queueThumbnails.Clear();
    }

    /// <summary>
    /// Disposes every SVG icon raster owned by the form.
    /// </summary>
    private void DisposeUiIcons()
    {
        foreach (Image icon in _uiIcons)
        {
            icon.Dispose();
        }

        _uiIcons.Clear();
    }

    /// <summary>
    /// Opens the donation window as a modal dialog.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void Btn_Donate_Click(object? sender, EventArgs e)
    {
        using FormDonate donateForm = new();
        donateForm.ShowDialog(this);
    }

    /// <summary>
    /// Requests process cancellation before the application window closes.
    /// </summary>
    /// <param name="sender">The form that raised the event.</param>
    /// <param name="e">The form-closing event data.</param>
    private void FormMain_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_isImportingModel)
        {
            e.Cancel = true;
            _closeAfterCancellation = true;
            _modelImportCancellationTokenSource?.Cancel();
            Lbl_Log.Text = "Canceling model import...";
            Lbl_Log.ForeColor = UiTheme.Danger;
            return;
        }

        if (!_isProcessing)
        {
            return;
        }

        e.Cancel = true;
        _closeAfterCancellation = true;
        RequestCancellation();
    }

    /// <summary>
    /// Shows a consistent error dialog with a heading and optional details.
    /// </summary>
    /// <param name="heading">The user-facing error summary.</param>
    /// <param name="details">Additional diagnostic information.</param>
    private void ShowError(string heading, string details)
    {
        string message = string.IsNullOrWhiteSpace(details)
            ? heading
            : $"{heading}{Environment.NewLine}{Environment.NewLine}{details}";

        MessageBox.Show(this, message, "Sharp Forge", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
