using SharpForge.Source;
using System.Diagnostics;

namespace SharpForge
{
    public partial class FormMain : Form
    {
        private Image? _currentPreview;
        private CancellationTokenSource? _cancellationToken;

        private readonly List<string> _imagePaths = [];
        private readonly string _upscaler = "realesrgan/realesrgan-ncnn-vulkan.exe";
        private readonly string _models = "realesrgan/models";
        private readonly string _appVersion = "1.1.0";
        private readonly object _lock = new();

        private readonly int[] _threadAmountList = [1, 2, 3, 4, 5, 6, 7, 8];
        private readonly int[] _tileSizeList = [32, 64, 128, 256, 512, 1024];

        private readonly string[] _imageFormatList = ["jpg", "png", "webp"];
        private readonly string[] _aiModelsList = ["esrgan-x4", "realesrgan-x4plus", "realesrgnet-x4plus"];

        public FormMain()
        {
            InitializeComponent();
            SetupComponent();
        }

        private void SetupComponent()
        {
            // Vesion label
            Lbl_ApplicationVersion.Text = _appVersion;

            // Drag panel
            Pnl_DragImage.AllowDrop = true;

            // Image preview panel
            Pnl_ImagePreview.BackColor = Color.Transparent;

            // AI models (load from folder)
            LoadModelsList(Cmb_AI_Model, _aiModelsList, 1);

            // CPU
            LoadComboBox(Cmb_CPU_Threads, _threadAmountList, 3);

            // GPU
            LoadComboBox(Cmb_GPU_ThreadsUpload, _threadAmountList, 5);
            LoadComboBox(Cmb_GPU_ThreadsReading, _threadAmountList, 7);
            LoadComboBox(Cmb_GPU_ThreadsWriting, _threadAmountList, 1);

            // Tiling
            LoadComboBox(Cmb_TileSize, _tileSizeList, 2);

            // Format
            LoadComboBox(Cmb_FileExtension, _imageFormatList, 1);
        }

        private static bool IsImageFile(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            return ext is ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".tiff";
        }

        private static void LoadComboBox<T>(ComboBox comboBox, T[] list, int defaultIndex = 0)
        {
            if (list.Length == 0)
                return;

            comboBox.Items.AddRange(list.Cast<object>().ToArray());
            comboBox.SelectedIndex = Math.Clamp(defaultIndex, 0, list.Length - 1);
        }

        private void LoadModelsList<T>(ComboBox comboBox, T[] list, int defaultIndex = 0)
        {
            if (!Directory.Exists(_models))
            {
                MessageBox.Show($"Models directory not found: {_models}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Get all .bin files in the models folder
            string[] modelFiles = Directory.GetFiles(_models, "*.bin");

            // Extract file names without extension
            string[] aiModels = modelFiles.Select(file => Path.GetFileNameWithoutExtension(file)).ToArray();

            // Combine hardcoded list with dynamic list and remove duplicates
            HashSet<string> uniqueModels = new(list.Cast<string>().Concat(aiModels));

            // Load into combo box
            LoadComboBox(comboBox, uniqueModels.ToArray(), defaultIndex);
        }

        private void Pnl_DragImage_DragDrop(object sender, DragEventArgs e)
        {
            object? fileDrop = e.Data?.GetData(DataFormats.FileDrop);

            if (fileDrop is string[] files)
            {
                foreach (string file in files)
                {
                    if (IsImageFile(file) && !_imagePaths.Contains(file))
                        _imagePaths.Add(file);
                }

                Lbl_Log.Text = $"Added {_imagePaths.Count} image(s).";

                // Enable "Clear" button
                Btn_Clear.Enabled = true;

                // Load image preview
                _currentPreview = Image.FromFile(files[0]);
                Pnl_ImagePreview.Size = new(_currentPreview.Width, _currentPreview.Height);

                // Center image preview location
                Pnl_ImagePreview.Location = new(
                    (Pnl_DragImage.Size.Width / 2) - (Pnl_ImagePreview.Size.Width / 2),
                    (Pnl_DragImage.Size.Height / 2) - (Pnl_ImagePreview.Size.Height / 2)
                );

                // Show image preview
                Pnl_ImagePreview.BackgroundImage = _currentPreview;
            }
        }

        private void Pnl_DragImage_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) != null)
            {
                object? fileDrop = e.Data?.GetData(DataFormats.FileDrop);

                e.Effect = (fileDrop is string[] files && files.Any(IsImageFile)) ? DragDropEffects.Copy : DragDropEffects.None;
            }
        }

        private void Btn_Clear_Click(object sender, EventArgs e)
        {
            _imagePaths.Clear();
            Prg_Process.Value = 0;
            Lbl_Log.Text = "Ready.";

            // Disable "Clear" button
            Btn_Clear.Enabled = false;

            // Clear image preview
            Pnl_ImagePreview.BackgroundImage = null;
        }

        private void Btn_Upscale_Click(object sender, EventArgs e)
        {
            if (Btn_Upscale.Text == "Cancel")
            {
                // If already upscaling, cancel the operation
                _cancellationToken?.Cancel();
                Btn_Upscale.Enabled = false;
                return;
            }

            if (_imagePaths.Count == 0)
            {
                MessageBox.Show("No images to upscale.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Lbl_Log.Text = $"Upscaling {_imagePaths.Count} image(s)...";
            Prg_Process.Minimum = 0;
            Prg_Process.Maximum = _imagePaths.Count;
            Prg_Process.Value = 0;

            Btn_Clear.Enabled = false;

            // Change button to "Cancel"
            Btn_Upscale.Text = "Cancel";
            Btn_Upscale.ForeColor = Color.Red;

            _cancellationToken = new CancellationTokenSource();

            Thread upscaleThread = new(() =>
            {
                int threadCount = (int)Invoke(new Func<int>(() =>
                    Cmb_CPU_Threads.SelectedItem != null ? Convert.ToInt32(Cmb_CPU_Threads.SelectedItem) : 1
                ));

                ParallelUpscale(threadCount, _cancellationToken.Token);
            })
            {
                IsBackground = true
            };

            upscaleThread.Start();
        }


        private void ParallelUpscale(int threadCount, CancellationToken cancellationToken)
        {
            Stopwatch stopwatch = new();
            stopwatch.Start();

            int processedCount = 0;

            Parallel.ForEach(_imagePaths, new ParallelOptions { MaxDegreeOfParallelism = threadCount }, (inputPath, state) =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    state.Break();
                    return;
                }

                // Retrieve the selected AI model from the UI thread before starting the parallel task
                string selectedAIModel = (string)Invoke(new Func<string>(() =>
                    Cmb_AI_Model.SelectedItem?.ToString() ?? "realesrgan-x4plus"
                ));

                string? directory = Path.GetDirectoryName(inputPath) ?? String.Empty;
                string upscaledDir = Path.Combine(directory, $"_Upscaled ({selectedAIModel})");

                // Create "_Upscaled" directory
                if (!Chk_UseSuffix.Checked)
                    Directory.CreateDirectory(upscaledDir);

                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(inputPath);

                // Fetch the extension synchronously
                string? extension = (string?)Invoke(new Func<string?>(() =>
                    Cmb_FileExtension.SelectedItem?.ToString()));

                // Default extension in case of null/empty
                if (string.IsNullOrWhiteSpace(extension))
                    extension = "png";

                // Check if using suffix
                string outputPath = !Chk_UseSuffix.Checked ?
                    Path.Combine(upscaledDir, $"{fileNameWithoutExt}.{extension}") :
                    Path.Combine(directory, $"{fileNameWithoutExt} ({selectedAIModel}).{extension}");

                if (File.Exists(outputPath))
                {
                    lock (_lock)
                    {
                        BeginInvoke(() =>
                        {
                            Lbl_Log.Text = "Skipping already upscaled image(s).";
                            Prg_Process.Value = ++processedCount;
                        });
                    }

                    return;
                }

                try
                {
                    ProcessStartInfo psi = new()
                    {
                        FileName = _upscaler,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    Invoke((MethodInvoker)(() =>
                    {
                        psi.Arguments = $"-i \"{inputPath}\" -o \"{outputPath}\" -g 0 -j " +
                                        $"{Cmb_GPU_ThreadsUpload.SelectedItem?.ToString()}:" +
                                        $"{Cmb_GPU_ThreadsReading.SelectedItem?.ToString()}:" +
                                        $"{Cmb_GPU_ThreadsWriting.SelectedItem?.ToString()} " +
                                        $"-t {Cmb_TileSize.SelectedItem?.ToString()}" +
                                        $"-n {Cmb_AI_Model.SelectedItem?.ToString()}" +
                                        $"-f {Cmb_FileExtension.SelectedItem?.ToString()}";
                    }));

                    using Process process = new() { StartInfo = psi };

                    process.Start();
                    process.WaitForExit();

                    if (cancellationToken.IsCancellationRequested)
                    {
                        process.Kill();
                        state.Break();
                        return;
                    }

                    lock (_lock)
                    {
                        Invoke(() =>
                        {
                            if (process.ExitCode == 0)
                                Lbl_Log.Text = $"Image {Prg_Process.Value + 1}/{Prg_Process.Maximum} upscaled.";

                            else
                                MessageBox.Show($"Upscale failed for {inputPath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                            Prg_Process.Value = ++processedCount;
                        });
                    }
                }

                catch (Exception ex)
                {
                    lock (_lock)
                    {
                        BeginInvoke(() =>
                        {
                            MessageBox.Show($"Exception: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        });
                    }
                }
            });

            stopwatch.Stop();

            BeginInvoke(() =>
            {
                if (cancellationToken.IsCancellationRequested)
                    Lbl_Log.Text = "Upscaling canceled.";

                else
                {
                    float elapsedSeconds = (float)stopwatch.Elapsed.TotalSeconds;
                    Lbl_Log.Text = $"Done – Upscaling took {elapsedSeconds:f1} seconds.";
                }

                // Restore button state
                Btn_Upscale.Text = "Upscale";
                Btn_Upscale.ForeColor = Color.White;
                Btn_Upscale.Enabled = true;
                Btn_Clear.Enabled = true;
            });
        }

        private void Btn_DonatePayPal_Click(object sender, EventArgs e)
        {
            FormDonate formDonate = new();
            formDonate.ShowDialog();
        }
    }
}
