namespace SharpForge.Source
{
    partial class FormDonate
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormDonate));
            panel8 = new Panel();
            panel1 = new Panel();
            Btn_MoneyDollar = new Button();
            panel2 = new Panel();
            label2 = new Label();
            label1 = new Label();
            Btn_MoneyReal = new Button();
            Btn_MoneyEuro = new Button();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // panel8
            // 
            panel8.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panel8.BackColor = Color.FromArgb(36, 36, 36);
            panel8.BorderStyle = BorderStyle.FixedSingle;
            panel8.Location = new Point(-1, -1);
            panel8.Margin = new Padding(0);
            panel8.Name = "panel8";
            panel8.Size = new Size(588, 33);
            panel8.TabIndex = 8;
            // 
            // panel1
            // 
            panel1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panel1.BackColor = Color.FromArgb(36, 36, 36);
            panel1.BorderStyle = BorderStyle.FixedSingle;
            panel1.Location = new Point(-1, 428);
            panel1.Margin = new Padding(0);
            panel1.Name = "panel1";
            panel1.Size = new Size(588, 33);
            panel1.TabIndex = 9;
            // 
            // Btn_MoneyDollar
            // 
            Btn_MoneyDollar.BackColor = Color.FromArgb(36, 36, 36);
            Btn_MoneyDollar.BackgroundImage = (Image)resources.GetObject("Btn_MoneyDollar.BackgroundImage");
            Btn_MoneyDollar.BackgroundImageLayout = ImageLayout.Stretch;
            Btn_MoneyDollar.Cursor = Cursors.Hand;
            Btn_MoneyDollar.FlatAppearance.BorderSize = 0;
            Btn_MoneyDollar.FlatStyle = FlatStyle.Flat;
            Btn_MoneyDollar.Font = new Font("Lato", 12F);
            Btn_MoneyDollar.ForeColor = SystemColors.Control;
            Btn_MoneyDollar.Location = new Point(125, 95);
            Btn_MoneyDollar.Margin = new Padding(5, 3, 5, 3);
            Btn_MoneyDollar.Name = "Btn_MoneyDollar";
            Btn_MoneyDollar.Size = new Size(50, 50);
            Btn_MoneyDollar.TabIndex = 10;
            Btn_MoneyDollar.UseVisualStyleBackColor = false;
            Btn_MoneyDollar.Click += Btn_MoneyDollar_Click;
            // 
            // panel2
            // 
            panel2.BackColor = Color.FromArgb(36, 36, 36);
            panel2.BorderStyle = BorderStyle.FixedSingle;
            panel2.Controls.Add(label2);
            panel2.Controls.Add(label1);
            panel2.Controls.Add(Btn_MoneyReal);
            panel2.Controls.Add(Btn_MoneyEuro);
            panel2.Controls.Add(Btn_MoneyDollar);
            panel2.Location = new Point(150, 107);
            panel2.Name = "panel2";
            panel2.Padding = new Padding(29);
            panel2.Size = new Size(300, 250);
            panel2.TabIndex = 11;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Lato", 14F);
            label2.ForeColor = SystemColors.Control;
            label2.Location = new Point(78, 166);
            label2.Margin = new Padding(18);
            label2.Name = "label2";
            label2.Size = new Size(141, 23);
            label2.TabIndex = 11;
            label2.Text = "With Any Value";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Lato", 14F, FontStyle.Bold);
            label1.ForeColor = SystemColors.Control;
            label1.Location = new Point(96, 51);
            label1.Margin = new Padding(18);
            label1.Name = "label1";
            label1.Size = new Size(105, 23);
            label1.TabIndex = 11;
            label1.Text = "Support Us";
            // 
            // Btn_MoneyReal
            // 
            Btn_MoneyReal.BackColor = Color.FromArgb(36, 36, 36);
            Btn_MoneyReal.BackgroundImage = (Image)resources.GetObject("Btn_MoneyReal.BackgroundImage");
            Btn_MoneyReal.BackgroundImageLayout = ImageLayout.Stretch;
            Btn_MoneyReal.Cursor = Cursors.Hand;
            Btn_MoneyReal.FlatAppearance.BorderSize = 0;
            Btn_MoneyReal.FlatStyle = FlatStyle.Flat;
            Btn_MoneyReal.Font = new Font("Lato", 12F);
            Btn_MoneyReal.ForeColor = SystemColors.Control;
            Btn_MoneyReal.Location = new Point(185, 95);
            Btn_MoneyReal.Margin = new Padding(5, 3, 5, 3);
            Btn_MoneyReal.Name = "Btn_MoneyReal";
            Btn_MoneyReal.Size = new Size(50, 50);
            Btn_MoneyReal.TabIndex = 10;
            Btn_MoneyReal.UseVisualStyleBackColor = false;
            Btn_MoneyReal.Click += Btn_MoneyReal_Click;
            // 
            // Btn_MoneyEuro
            // 
            Btn_MoneyEuro.BackColor = Color.FromArgb(36, 36, 36);
            Btn_MoneyEuro.BackgroundImage = (Image)resources.GetObject("Btn_MoneyEuro.BackgroundImage");
            Btn_MoneyEuro.BackgroundImageLayout = ImageLayout.Stretch;
            Btn_MoneyEuro.Cursor = Cursors.Hand;
            Btn_MoneyEuro.FlatAppearance.BorderSize = 0;
            Btn_MoneyEuro.FlatStyle = FlatStyle.Flat;
            Btn_MoneyEuro.Font = new Font("Lato", 12F);
            Btn_MoneyEuro.ForeColor = SystemColors.Control;
            Btn_MoneyEuro.Location = new Point(65, 95);
            Btn_MoneyEuro.Margin = new Padding(5, 3, 5, 3);
            Btn_MoneyEuro.Name = "Btn_MoneyEuro";
            Btn_MoneyEuro.Size = new Size(50, 50);
            Btn_MoneyEuro.TabIndex = 10;
            Btn_MoneyEuro.UseVisualStyleBackColor = false;
            Btn_MoneyEuro.Click += Btn_MoneyEuro_Click;
            // 
            // FormDonate
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(32, 32, 32);
            ClientSize = new Size(584, 461);
            Controls.Add(panel2);
            Controls.Add(panel1);
            Controls.Add(panel8);
            Font = new Font("Segoe UI", 9F);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FormDonate";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Donate";
            KeyPress += FormDonate_KeyPress;
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel panel8;
        private Panel panel1;
        private Button Btn_MoneyDollar;
        private Panel panel2;
        private Button Btn_MoneyReal;
        private Button Btn_MoneyEuro;
        private Label label1;
        private Label label2;
    }
}