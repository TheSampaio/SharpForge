namespace SharpForge
{
    partial class FormMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            Pnl_DragImage = new Panel();
            Prg_Process = new ProgressBar();
            Pnl_ImagePreview = new Panel();
            panel2 = new Panel();
            Lbl_ApplicationVersion = new Label();
            Lbl_Log = new Label();
            Btn_Upscale = new Button();
            panel1 = new Panel();
            Btn_Clear = new Button();
            panel3 = new Panel();
            Cmb_AI_Model = new ComboBox();
            label6 = new Label();
            label1 = new Label();
            panel4 = new Panel();
            Cmb_CPU_Threads = new ComboBox();
            label2 = new Label();
            label7 = new Label();
            panel5 = new Panel();
            Cmb_GPU_ThreadsWriting = new ComboBox();
            Cmb_GPU_ThreadsReading = new ComboBox();
            Cmb_GPU_ThreadsUpload = new ComboBox();
            label3 = new Label();
            label10 = new Label();
            label9 = new Label();
            label8 = new Label();
            panel6 = new Panel();
            Chk_UseSuffix = new CheckBox();
            Cmb_FileExtension = new ComboBox();
            label5 = new Label();
            label13 = new Label();
            label12 = new Label();
            panel7 = new Panel();
            Cmb_TileSize = new ComboBox();
            label4 = new Label();
            label11 = new Label();
            panel8 = new Panel();
            Btn_Donate = new Button();
            panel9 = new Panel();
            Pnl_DragImage.SuspendLayout();
            panel2.SuspendLayout();
            panel1.SuspendLayout();
            panel3.SuspendLayout();
            panel4.SuspendLayout();
            panel5.SuspendLayout();
            panel6.SuspendLayout();
            panel7.SuspendLayout();
            panel9.SuspendLayout();
            SuspendLayout();
            // 
            // Pnl_DragImage
            // 
            Pnl_DragImage.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            Pnl_DragImage.AutoScroll = true;
            Pnl_DragImage.BackgroundImage = (Image)resources.GetObject("Pnl_DragImage.BackgroundImage");
            Pnl_DragImage.BorderStyle = BorderStyle.FixedSingle;
            Pnl_DragImage.Controls.Add(Prg_Process);
            Pnl_DragImage.Controls.Add(Pnl_ImagePreview);
            Pnl_DragImage.Location = new Point(340, 45);
            Pnl_DragImage.Margin = new Padding(10, 0, 10, 0);
            Pnl_DragImage.Name = "Pnl_DragImage";
            Pnl_DragImage.Size = new Size(120, 474);
            Pnl_DragImage.TabIndex = 0;
            Pnl_DragImage.DragDrop += Pnl_DragImage_DragDrop;
            Pnl_DragImage.DragEnter += Pnl_DragImage_DragEnter;
            // 
            // Prg_Process
            // 
            Prg_Process.BackColor = Color.FromArgb(36, 36, 36);
            Prg_Process.Dock = DockStyle.Bottom;
            Prg_Process.Location = new Point(0, 452);
            Prg_Process.Margin = new Padding(0);
            Prg_Process.Name = "Prg_Process";
            Prg_Process.Size = new Size(118, 20);
            Prg_Process.TabIndex = 1;
            // 
            // Pnl_ImagePreview
            // 
            Pnl_ImagePreview.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            Pnl_ImagePreview.BackColor = Color.Transparent;
            Pnl_ImagePreview.BackgroundImageLayout = ImageLayout.Center;
            Pnl_ImagePreview.Location = new Point(60, 188);
            Pnl_ImagePreview.Name = "Pnl_ImagePreview";
            Pnl_ImagePreview.Size = new Size(0, 98);
            Pnl_ImagePreview.TabIndex = 2;
            // 
            // panel2
            // 
            panel2.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panel2.BackColor = Color.FromArgb(36, 36, 36);
            panel2.BorderStyle = BorderStyle.FixedSingle;
            panel2.Controls.Add(Lbl_ApplicationVersion);
            panel2.Controls.Add(Lbl_Log);
            panel2.Location = new Point(-1, 529);
            panel2.Margin = new Padding(0);
            panel2.Name = "panel2";
            panel2.Size = new Size(786, 33);
            panel2.TabIndex = 1;
            // 
            // Lbl_ApplicationVersion
            // 
            Lbl_ApplicationVersion.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            Lbl_ApplicationVersion.AutoSize = true;
            Lbl_ApplicationVersion.Font = new Font("Lato", 12F);
            Lbl_ApplicationVersion.ForeColor = SystemColors.Control;
            Lbl_ApplicationVersion.Location = new Point(728, 6);
            Lbl_ApplicationVersion.Margin = new Padding(2);
            Lbl_ApplicationVersion.Name = "Lbl_ApplicationVersion";
            Lbl_ApplicationVersion.Size = new Size(44, 19);
            Lbl_ApplicationVersion.TabIndex = 0;
            Lbl_ApplicationVersion.Text = "0.0.0";
            // 
            // Lbl_Log
            // 
            Lbl_Log.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            Lbl_Log.AutoSize = true;
            Lbl_Log.Font = new Font("Lato", 12F, FontStyle.Bold);
            Lbl_Log.ForeColor = SystemColors.Control;
            Lbl_Log.Location = new Point(12, 6);
            Lbl_Log.Margin = new Padding(2);
            Lbl_Log.Name = "Lbl_Log";
            Lbl_Log.Size = new Size(55, 19);
            Lbl_Log.TabIndex = 0;
            Lbl_Log.Text = "Ready.";
            // 
            // Btn_Upscale
            // 
            Btn_Upscale.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            Btn_Upscale.BackColor = Color.FromArgb(50, 50, 50);
            Btn_Upscale.Cursor = Cursors.Hand;
            Btn_Upscale.FlatAppearance.BorderSize = 0;
            Btn_Upscale.FlatStyle = FlatStyle.Flat;
            Btn_Upscale.Font = new Font("Lato", 12F, FontStyle.Bold);
            Btn_Upscale.ForeColor = SystemColors.Control;
            Btn_Upscale.Location = new Point(5, 61);
            Btn_Upscale.Name = "Btn_Upscale";
            Btn_Upscale.Size = new Size(287, 50);
            Btn_Upscale.TabIndex = 0;
            Btn_Upscale.Text = "Upscale";
            Btn_Upscale.UseVisualStyleBackColor = false;
            Btn_Upscale.Click += Btn_Upscale_Click;
            // 
            // panel1
            // 
            panel1.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            panel1.BackColor = Color.FromArgb(36, 36, 36);
            panel1.BorderStyle = BorderStyle.FixedSingle;
            panel1.Controls.Add(Btn_Clear);
            panel1.Controls.Add(Btn_Upscale);
            panel1.Location = new Point(473, 401);
            panel1.Name = "panel1";
            panel1.Padding = new Padding(2);
            panel1.Size = new Size(299, 118);
            panel1.TabIndex = 5;
            // 
            // Btn_Clear
            // 
            Btn_Clear.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Btn_Clear.BackColor = Color.FromArgb(50, 50, 50);
            Btn_Clear.Cursor = Cursors.Hand;
            Btn_Clear.Enabled = false;
            Btn_Clear.FlatAppearance.BorderSize = 0;
            Btn_Clear.FlatStyle = FlatStyle.Flat;
            Btn_Clear.Font = new Font("Lato", 12F, FontStyle.Bold);
            Btn_Clear.ForeColor = SystemColors.Control;
            Btn_Clear.Location = new Point(5, 5);
            Btn_Clear.Name = "Btn_Clear";
            Btn_Clear.Size = new Size(287, 50);
            Btn_Clear.TabIndex = 1;
            Btn_Clear.Text = "Clear";
            Btn_Clear.UseVisualStyleBackColor = false;
            Btn_Clear.Click += Btn_Clear_Click;
            // 
            // panel3
            // 
            panel3.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            panel3.BackColor = Color.FromArgb(36, 36, 36);
            panel3.BorderStyle = BorderStyle.FixedSingle;
            panel3.Controls.Add(Cmb_AI_Model);
            panel3.Controls.Add(label6);
            panel3.Controls.Add(label1);
            panel3.Location = new Point(12, 185);
            panel3.Margin = new Padding(3, 5, 3, 5);
            panel3.Name = "panel3";
            panel3.Padding = new Padding(2);
            panel3.Size = new Size(315, 84);
            panel3.TabIndex = 0;
            // 
            // Cmb_AI_Model
            // 
            Cmb_AI_Model.BackColor = SystemColors.Window;
            Cmb_AI_Model.Cursor = Cursors.Hand;
            Cmb_AI_Model.DropDownHeight = 100;
            Cmb_AI_Model.DropDownStyle = ComboBoxStyle.DropDownList;
            Cmb_AI_Model.Font = new Font("Segoe UI", 12F);
            Cmb_AI_Model.ForeColor = SystemColors.ControlText;
            Cmb_AI_Model.FormattingEnabled = true;
            Cmb_AI_Model.IntegralHeight = false;
            Cmb_AI_Model.Location = new Point(85, 38);
            Cmb_AI_Model.Name = "Cmb_AI_Model";
            Cmb_AI_Model.Size = new Size(215, 29);
            Cmb_AI_Model.TabIndex = 0;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Lato", 12F);
            label6.ForeColor = SystemColors.Control;
            label6.Location = new Point(10, 43);
            label6.Margin = new Padding(8, 2, 7, 2);
            label6.Name = "label6";
            label6.Size = new Size(54, 19);
            label6.TabIndex = 0;
            label6.Text = "Model";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Lato", 14F, FontStyle.Bold);
            label1.ForeColor = SystemColors.Control;
            label1.Location = new Point(10, 11);
            label1.Margin = new Padding(8);
            label1.Name = "label1";
            label1.Size = new Size(29, 23);
            label1.TabIndex = 0;
            label1.Text = "AI";
            // 
            // panel4
            // 
            panel4.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            panel4.BackColor = Color.FromArgb(36, 36, 36);
            panel4.BorderStyle = BorderStyle.FixedSingle;
            panel4.Controls.Add(Cmb_CPU_Threads);
            panel4.Controls.Add(label2);
            panel4.Controls.Add(label7);
            panel4.Location = new Point(12, 279);
            panel4.Margin = new Padding(3, 5, 3, 5);
            panel4.Name = "panel4";
            panel4.Padding = new Padding(2);
            panel4.Size = new Size(315, 85);
            panel4.TabIndex = 1;
            // 
            // Cmb_CPU_Threads
            // 
            Cmb_CPU_Threads.BackColor = SystemColors.Window;
            Cmb_CPU_Threads.Cursor = Cursors.Hand;
            Cmb_CPU_Threads.DropDownHeight = 100;
            Cmb_CPU_Threads.DropDownStyle = ComboBoxStyle.DropDownList;
            Cmb_CPU_Threads.Font = new Font("Segoe UI", 12F);
            Cmb_CPU_Threads.ForeColor = SystemColors.ControlText;
            Cmb_CPU_Threads.FormattingEnabled = true;
            Cmb_CPU_Threads.IntegralHeight = false;
            Cmb_CPU_Threads.Location = new Point(85, 38);
            Cmb_CPU_Threads.Name = "Cmb_CPU_Threads";
            Cmb_CPU_Threads.Size = new Size(215, 29);
            Cmb_CPU_Threads.TabIndex = 0;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Lato", 14F, FontStyle.Bold);
            label2.ForeColor = SystemColors.Control;
            label2.Location = new Point(10, 11);
            label2.Margin = new Padding(8);
            label2.Name = "label2";
            label2.Size = new Size(49, 23);
            label2.TabIndex = 0;
            label2.Text = "CPU";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Font = new Font("Lato", 12F);
            label7.ForeColor = SystemColors.Control;
            label7.Location = new Point(10, 43);
            label7.Margin = new Padding(8, 2, 7, 2);
            label7.Name = "label7";
            label7.Size = new Size(65, 19);
            label7.TabIndex = 0;
            label7.Text = "Threads";
            // 
            // panel5
            // 
            panel5.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            panel5.BackColor = Color.FromArgb(36, 36, 36);
            panel5.BorderStyle = BorderStyle.FixedSingle;
            panel5.Controls.Add(Cmb_GPU_ThreadsWriting);
            panel5.Controls.Add(Cmb_GPU_ThreadsReading);
            panel5.Controls.Add(Cmb_GPU_ThreadsUpload);
            panel5.Controls.Add(label3);
            panel5.Controls.Add(label10);
            panel5.Controls.Add(label9);
            panel5.Controls.Add(label8);
            panel5.Location = new Point(12, 375);
            panel5.Margin = new Padding(3, 5, 3, 5);
            panel5.Name = "panel5";
            panel5.Padding = new Padding(2);
            panel5.Size = new Size(315, 144);
            panel5.TabIndex = 2;
            // 
            // Cmb_GPU_ThreadsWriting
            // 
            Cmb_GPU_ThreadsWriting.BackColor = SystemColors.Window;
            Cmb_GPU_ThreadsWriting.Cursor = Cursors.Hand;
            Cmb_GPU_ThreadsWriting.DropDownHeight = 100;
            Cmb_GPU_ThreadsWriting.DropDownStyle = ComboBoxStyle.DropDownList;
            Cmb_GPU_ThreadsWriting.Font = new Font("Segoe UI", 12F);
            Cmb_GPU_ThreadsWriting.ForeColor = SystemColors.ControlText;
            Cmb_GPU_ThreadsWriting.FormattingEnabled = true;
            Cmb_GPU_ThreadsWriting.IntegralHeight = false;
            Cmb_GPU_ThreadsWriting.Location = new Point(146, 99);
            Cmb_GPU_ThreadsWriting.Margin = new Padding(3, 3, 8, 3);
            Cmb_GPU_ThreadsWriting.Name = "Cmb_GPU_ThreadsWriting";
            Cmb_GPU_ThreadsWriting.Size = new Size(154, 29);
            Cmb_GPU_ThreadsWriting.TabIndex = 2;
            // 
            // Cmb_GPU_ThreadsReading
            // 
            Cmb_GPU_ThreadsReading.BackColor = SystemColors.Window;
            Cmb_GPU_ThreadsReading.Cursor = Cursors.Hand;
            Cmb_GPU_ThreadsReading.DropDownHeight = 100;
            Cmb_GPU_ThreadsReading.DropDownStyle = ComboBoxStyle.DropDownList;
            Cmb_GPU_ThreadsReading.Font = new Font("Segoe UI", 12F);
            Cmb_GPU_ThreadsReading.ForeColor = SystemColors.ControlText;
            Cmb_GPU_ThreadsReading.FormattingEnabled = true;
            Cmb_GPU_ThreadsReading.IntegralHeight = false;
            Cmb_GPU_ThreadsReading.Location = new Point(146, 68);
            Cmb_GPU_ThreadsReading.Name = "Cmb_GPU_ThreadsReading";
            Cmb_GPU_ThreadsReading.Size = new Size(154, 29);
            Cmb_GPU_ThreadsReading.TabIndex = 1;
            // 
            // Cmb_GPU_ThreadsUpload
            // 
            Cmb_GPU_ThreadsUpload.BackColor = SystemColors.Window;
            Cmb_GPU_ThreadsUpload.Cursor = Cursors.Hand;
            Cmb_GPU_ThreadsUpload.DropDownHeight = 100;
            Cmb_GPU_ThreadsUpload.DropDownStyle = ComboBoxStyle.DropDownList;
            Cmb_GPU_ThreadsUpload.Font = new Font("Segoe UI", 12F);
            Cmb_GPU_ThreadsUpload.ForeColor = SystemColors.ControlText;
            Cmb_GPU_ThreadsUpload.FormattingEnabled = true;
            Cmb_GPU_ThreadsUpload.IntegralHeight = false;
            Cmb_GPU_ThreadsUpload.Location = new Point(146, 37);
            Cmb_GPU_ThreadsUpload.Name = "Cmb_GPU_ThreadsUpload";
            Cmb_GPU_ThreadsUpload.Size = new Size(154, 29);
            Cmb_GPU_ThreadsUpload.TabIndex = 0;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Lato", 14F, FontStyle.Bold);
            label3.ForeColor = SystemColors.Control;
            label3.Location = new Point(10, 11);
            label3.Margin = new Padding(8);
            label3.Name = "label3";
            label3.Size = new Size(50, 23);
            label3.TabIndex = 0;
            label3.Text = "GPU";
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Font = new Font("Lato", 12F);
            label10.ForeColor = SystemColors.Control;
            label10.Location = new Point(10, 104);
            label10.Margin = new Padding(8, 8, 7, 2);
            label10.Name = "label10";
            label10.Size = new Size(122, 19);
            label10.TabIndex = 0;
            label10.Text = "Writing Threads";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Font = new Font("Lato", 12F);
            label9.ForeColor = SystemColors.Control;
            label9.Location = new Point(10, 73);
            label9.Margin = new Padding(8, 8, 7, 2);
            label9.Name = "label9";
            label9.Size = new Size(124, 19);
            label9.TabIndex = 0;
            label9.Text = "Reading Threads";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Font = new Font("Lato", 12F);
            label8.ForeColor = SystemColors.Control;
            label8.Location = new Point(10, 42);
            label8.Margin = new Padding(8, 2, 7, 2);
            label8.Name = "label8";
            label8.Size = new Size(120, 19);
            label8.TabIndex = 0;
            label8.Text = "Upload Threads";
            // 
            // panel6
            // 
            panel6.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            panel6.BackColor = Color.FromArgb(36, 36, 36);
            panel6.BorderStyle = BorderStyle.FixedSingle;
            panel6.Controls.Add(Chk_UseSuffix);
            panel6.Controls.Add(Cmb_FileExtension);
            panel6.Controls.Add(label5);
            panel6.Controls.Add(label13);
            panel6.Controls.Add(label12);
            panel6.Location = new Point(473, 279);
            panel6.Margin = new Padding(3, 5, 3, 5);
            panel6.Name = "panel6";
            panel6.Padding = new Padding(2);
            panel6.Size = new Size(299, 113);
            panel6.TabIndex = 4;
            // 
            // Chk_UseSuffix
            // 
            Chk_UseSuffix.AutoSize = true;
            Chk_UseSuffix.BackColor = Color.FromArgb(32, 32, 32);
            Chk_UseSuffix.Cursor = Cursors.Hand;
            Chk_UseSuffix.Font = new Font("Segoe UI", 10F);
            Chk_UseSuffix.ForeColor = SystemColors.ControlText;
            Chk_UseSuffix.Location = new Point(96, 79);
            Chk_UseSuffix.Margin = new Padding(3, 9, 3, 3);
            Chk_UseSuffix.Name = "Chk_UseSuffix";
            Chk_UseSuffix.Size = new Size(15, 14);
            Chk_UseSuffix.TabIndex = 1;
            Chk_UseSuffix.UseVisualStyleBackColor = false;
            // 
            // Cmb_FileExtension
            // 
            Cmb_FileExtension.BackColor = SystemColors.Window;
            Cmb_FileExtension.Cursor = Cursors.Hand;
            Cmb_FileExtension.DropDownHeight = 100;
            Cmb_FileExtension.DropDownStyle = ComboBoxStyle.DropDownList;
            Cmb_FileExtension.Font = new Font("Segoe UI", 12F);
            Cmb_FileExtension.ForeColor = SystemColors.ControlText;
            Cmb_FileExtension.FormattingEnabled = true;
            Cmb_FileExtension.IntegralHeight = false;
            Cmb_FileExtension.Location = new Point(96, 38);
            Cmb_FileExtension.Name = "Cmb_FileExtension";
            Cmb_FileExtension.Size = new Size(139, 29);
            Cmb_FileExtension.TabIndex = 0;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Lato", 14F, FontStyle.Bold);
            label5.ForeColor = SystemColors.Control;
            label5.Location = new Point(10, 11);
            label5.Margin = new Padding(8);
            label5.Name = "label5";
            label5.Size = new Size(71, 23);
            label5.TabIndex = 0;
            label5.Text = "Format";
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Font = new Font("Lato", 12F);
            label13.ForeColor = SystemColors.Control;
            label13.Location = new Point(10, 76);
            label13.Margin = new Padding(8, 2, 7, 2);
            label13.Name = "label13";
            label13.Size = new Size(80, 19);
            label13.TabIndex = 0;
            label13.Text = "Use Suffix";
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Font = new Font("Lato", 12F);
            label12.ForeColor = SystemColors.Control;
            label12.Location = new Point(10, 43);
            label12.Margin = new Padding(8, 8, 7, 2);
            label12.Name = "label12";
            label12.Size = new Size(78, 19);
            label12.TabIndex = 0;
            label12.Text = "Extension";
            // 
            // panel7
            // 
            panel7.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            panel7.BackColor = Color.FromArgb(36, 36, 36);
            panel7.BorderStyle = BorderStyle.FixedSingle;
            panel7.Controls.Add(Cmb_TileSize);
            panel7.Controls.Add(label4);
            panel7.Controls.Add(label11);
            panel7.Location = new Point(473, 185);
            panel7.Margin = new Padding(3, 5, 3, 5);
            panel7.Name = "panel7";
            panel7.Padding = new Padding(2);
            panel7.Size = new Size(299, 84);
            panel7.TabIndex = 3;
            // 
            // Cmb_TileSize
            // 
            Cmb_TileSize.BackColor = SystemColors.Window;
            Cmb_TileSize.Cursor = Cursors.Hand;
            Cmb_TileSize.DropDownHeight = 100;
            Cmb_TileSize.DropDownStyle = ComboBoxStyle.DropDownList;
            Cmb_TileSize.Font = new Font("Segoe UI", 12F);
            Cmb_TileSize.ForeColor = SystemColors.ControlText;
            Cmb_TileSize.FormattingEnabled = true;
            Cmb_TileSize.IntegralHeight = false;
            Cmb_TileSize.Location = new Point(96, 38);
            Cmb_TileSize.Name = "Cmb_TileSize";
            Cmb_TileSize.Size = new Size(139, 29);
            Cmb_TileSize.TabIndex = 0;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Lato", 14F, FontStyle.Bold);
            label4.ForeColor = SystemColors.Control;
            label4.Location = new Point(10, 11);
            label4.Margin = new Padding(8);
            label4.Name = "label4";
            label4.Size = new Size(57, 23);
            label4.TabIndex = 0;
            label4.Text = "Tiling";
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Font = new Font("Lato", 12F);
            label11.ForeColor = SystemColors.Control;
            label11.Location = new Point(10, 43);
            label11.Margin = new Padding(8, 2, 7, 2);
            label11.Name = "label11";
            label11.Size = new Size(37, 19);
            label11.TabIndex = 0;
            label11.Text = "Size";
            // 
            // panel8
            // 
            panel8.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panel8.BackColor = Color.FromArgb(36, 36, 36);
            panel8.BorderStyle = BorderStyle.FixedSingle;
            panel8.Location = new Point(-1, 0);
            panel8.Name = "panel8";
            panel8.Size = new Size(786, 33);
            panel8.TabIndex = 7;
            // 
            // Btn_Donate
            // 
            Btn_Donate.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            Btn_Donate.BackgroundImage = (Image)resources.GetObject("Btn_Donate.BackgroundImage");
            Btn_Donate.BackgroundImageLayout = ImageLayout.Stretch;
            Btn_Donate.Cursor = Cursors.Hand;
            Btn_Donate.FlatAppearance.BorderSize = 0;
            Btn_Donate.FlatStyle = FlatStyle.Flat;
            Btn_Donate.ForeColor = SystemColors.Control;
            Btn_Donate.Location = new Point(-1, -1);
            Btn_Donate.Margin = new Padding(0);
            Btn_Donate.Name = "Btn_Donate";
            Btn_Donate.Size = new Size(299, 80);
            Btn_Donate.TabIndex = 8;
            Btn_Donate.UseVisualStyleBackColor = true;
            Btn_Donate.Click += Btn_DonatePayPal_Click;
            // 
            // panel9
            // 
            panel9.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            panel9.BackColor = Color.FromArgb(36, 36, 36);
            panel9.BorderStyle = BorderStyle.FixedSingle;
            panel9.Controls.Add(Btn_Donate);
            panel9.Location = new Point(473, 95);
            panel9.Margin = new Padding(3, 5, 3, 5);
            panel9.Name = "panel9";
            panel9.Size = new Size(299, 80);
            panel9.TabIndex = 3;
            // 
            // FormMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(32, 32, 32);
            ClientSize = new Size(784, 561);
            Controls.Add(panel5);
            Controls.Add(panel4);
            Controls.Add(panel9);
            Controls.Add(panel7);
            Controls.Add(panel6);
            Controls.Add(panel3);
            Controls.Add(panel1);
            Controls.Add(panel2);
            Controls.Add(Pnl_DragImage);
            Controls.Add(panel8);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "FormMain";
            Text = "Sharp Forge";
            WindowState = FormWindowState.Maximized;
            Pnl_DragImage.ResumeLayout(false);
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            panel1.ResumeLayout(false);
            panel3.ResumeLayout(false);
            panel3.PerformLayout();
            panel4.ResumeLayout(false);
            panel4.PerformLayout();
            panel5.ResumeLayout(false);
            panel5.PerformLayout();
            panel6.ResumeLayout(false);
            panel6.PerformLayout();
            panel7.ResumeLayout(false);
            panel7.PerformLayout();
            panel9.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel Pnl_DragImage;
        private Panel panel2;
        private Button Btn_Upscale;
        private Panel panel1;
        private Button Btn_Clear;
        private Label Lbl_Log;
        private ProgressBar Prg_Process;
        private Panel panel3;
        private Panel panel4;
        private Panel panel5;
        private Panel panel6;
        private Panel panel7;
        private Label label1;
        private Label label2;
        private Label label3;
        private Label label5;
        private Label label4;
        private Label label6;
        private ComboBox Cmb_AI_Model;
        private ComboBox Cmb_CPU_Threads;
        private Label label7;
        private ComboBox Cmb_GPU_ThreadsWriting;
        private ComboBox Cmb_GPU_ThreadsReading;
        private ComboBox Cmb_GPU_ThreadsUpload;
        private Label label10;
        private Label label9;
        private Label label8;
        private ComboBox Cmb_FileExtension;
        private Label label12;
        private ComboBox Cmb_TileSize;
        private Label label11;
        private CheckBox Chk_UseSuffix;
        private Label label13;
        private Label Lbl_ApplicationVersion;
        private Panel Pnl_ImagePreview;
        private Panel panel8;
        private Button Btn_Donate;
        private Panel panel9;
    }
}
