using System.Diagnostics;

namespace SharpForge
{
    public partial class FormMain : Form
    {
        private readonly List<string> _imagePaths = [];
        private readonly string _upscaler = "realesrgan/realesrgan-ncnn-vulkan.exe";
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
            // Drag panel
            Pnl_DragImage.AllowDrop = true;

            // AI
            LoadComboBox(Cmb_AI_Model, _aiModelsList, 1);

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

#pragma warning disable CS8604
        private static void LoadComboBox<T>(ComboBox comboBox, T[] list, int defaultIndex = 0)
        {
            if (comboBox != null && list.Length > 0)
            {
                for (int i = 0; i < list.Length; i++)
                    comboBox.Items.Add(list[i]);

                comboBox.SelectedIndex = defaultIndex;
            }
        }
#pragma warning restore CS8604

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
        }

        private void Btn_Upscale_Click(object sender, EventArgs e)
        {
            if (_imagePaths.Count == 0)
            {
                MessageBox.Show("No images to upscale.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Lbl_Log.Text = $"Upscaling {_imagePaths.Count} image(s)...";
            Prg_Process.Minimum = 0;
            Prg_Process.Maximum = _imagePaths.Count;
            Prg_Process.Value = 0;

            // Disable "Clear" button
            Btn_Clear.Enabled = false;

            Thread upscaleThread = new(() =>
            {
                int threadCount = (int)Invoke(new Func<int>(() =>
                    Cmb_CPU_Threads.SelectedItem != null ? Convert.ToInt32(Cmb_CPU_Threads.SelectedItem) : 1
                ));

                ParallelUpscale(threadCount);
            })
            {
                IsBackground = true
            };


            upscaleThread.Start();
        }

        private void ParallelUpscale(int threadCount)
        {
            Stopwatch stopwatch = new();
            stopwatch.Start();

            int processedCount = 0;

            Parallel.ForEach(_imagePaths, new ParallelOptions { MaxDegreeOfParallelism = threadCount }, inputPath =>
            {
                string? directory = Path.GetDirectoryName(inputPath) ?? String.Empty;
                string upscaledDir = Path.Combine(directory, "_Upscaled");

                Directory.CreateDirectory(upscaledDir);

                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(inputPath);
                string outputPath = Path.Combine(upscaledDir, $"{fileNameWithoutExt}.png");

                if (File.Exists(outputPath))
                {
                    lock (_lock)
                    {
                        Invoke((MethodInvoker)(() =>
                        {
                            Lbl_Log.Text = "Skipping already upscaled image(s).";
                            Prg_Process.Value = ++processedCount;
                        }));
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

                    // Ensure UI thread access before modifying Arguments
                    Invoke((MethodInvoker)(() =>
                    {
                        psi.Arguments = $"-i \"{inputPath}\" -o \"{outputPath}\" -g 0 -j " +
                                        $"{Cmb_GPU_ThreadsUpload.SelectedItem?.ToString()}:" +
                                        $"{Cmb_GPU_ThreadsReading.SelectedItem?.ToString()}:" +
                                        $"{Cmb_GPU_ThreadsWriting.SelectedItem?.ToString()} " +
                                        $"-t {Cmb_TileSize.SelectedItem?.ToString()}" +
                                        $"-n {Cmb_AI_Model.SelectedItem?.ToString()}";
                    }));

                    using Process process = new() { StartInfo = psi };
                    process.Start();
                    process.WaitForExit();

                    lock (_lock)
                    {
                        Invoke((MethodInvoker)(() =>
                        {
                            if (process.ExitCode == 0)
                                Lbl_Log.Text = $"Image {Prg_Process.Value + 1}/{Prg_Process.Maximum} upscaled.";
                            else
                                MessageBox.Show($"Upscale failed for {inputPath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                            Prg_Process.Value = ++processedCount;
                        }));
                    }
                }
                catch (Exception ex)
                {
                    lock (_lock)
                    {
                        Invoke((MethodInvoker)(() =>
                        {
                            MessageBox.Show($"Exception: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }));
                    }
                }
            });

            stopwatch.Stop();
            int elapsedSeconds = (int)stopwatch.Elapsed.TotalSeconds;

            Invoke((MethodInvoker)(() =>
            {
                Lbl_Log.Text = $"Done – Upscaling took {elapsedSeconds} seconds.";

                // Enable "Clear" button
                Btn_Clear.Enabled = true;
            }));
        }
    }
}
