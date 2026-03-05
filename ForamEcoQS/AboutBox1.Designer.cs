namespace ForamEcoQS
{
    partial class AboutBox1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutBox1));
            okButton = new Button();
            checkUpdatesButton = new Button();
            logoPictureBox = new PictureBox();
            label1 = new Label();
            groupBox1 = new GroupBox();
            label2 = new Label();
            authorsPanel = new Panel();
            groupBox2 = new GroupBox();
            label3 = new Label();
            textBox1 = new TextBox();
            ((System.ComponentModel.ISupportInitialize)logoPictureBox).BeginInit();
            groupBox1.SuspendLayout();
            authorsPanel.SuspendLayout();
            groupBox2.SuspendLayout();
            SuspendLayout();
            //
            // okButton
            //
            okButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            okButton.DialogResult = DialogResult.Cancel;
            okButton.FlatStyle = FlatStyle.Flat;
            okButton.Location = new Point(842, 762);
            okButton.Margin = new Padding(4, 5, 4, 5);
            okButton.Name = "okButton";
            okButton.Size = new Size(100, 35);
            okButton.TabIndex = 24;
            okButton.Text = "&OK";
            //
            // checkUpdatesButton
            //
            checkUpdatesButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            checkUpdatesButton.FlatStyle = FlatStyle.Flat;
            checkUpdatesButton.Location = new Point(686, 762);
            checkUpdatesButton.Margin = new Padding(4, 5, 4, 5);
            checkUpdatesButton.Name = "checkUpdatesButton";
            checkUpdatesButton.Size = new Size(150, 35);
            checkUpdatesButton.TabIndex = 29;
            checkUpdatesButton.Text = "Check for Updates";
            checkUpdatesButton.Click += checkUpdatesButton_Click;
            //
            // logoPictureBox
            //
            logoPictureBox.Image = Properties.Resources.logo;
            logoPictureBox.Location = new Point(16, 101);
            logoPictureBox.Margin = new Padding(4, 5, 4, 5);
            logoPictureBox.Name = "logoPictureBox";
            logoPictureBox.Size = new Size(349, 367);
            logoPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            logoPictureBox.TabIndex = 12;
            logoPictureBox.TabStop = false;
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            label1.Location = new Point(420, 14);
            label1.Name = "label1";
            label1.Size = new Size(346, 20);
            label1.TabIndex = 25;
            label1.Text = "ForamEcoQS - Foraminiferal Ecological Quality Status - v1.0";
            // 
            // groupBox1
            //
            groupBox1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            groupBox1.Controls.Add(authorsPanel);
            groupBox1.Location = new Point(372, 37);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(560, 300);
            groupBox1.TabIndex = 26;
            groupBox1.TabStop = false;
            groupBox1.Text = "Authors";
            //
            // label2
            //
            label2.AutoSize = true;
            label2.Location = new Point(6, 6);
            label2.MaximumSize = new Size(520, 0);
            label2.Name = "label2";
            label2.Size = new Size(403, 100);
            label2.TabIndex = 0;
            label2.Text = "Matteo Mangiagalli - University of Urbino Carlo Bo, Italy\r\nm.mangiagalli@campus.uniurb.it\r\n\r\nFabrizio Frontalini - University of Urbino Carlo Bo, Italy\r\nfabrizio.frontalini@uniurb.it\r\n\r\nCarla Cristallo - University of Urbino Carlo Bo, Italy\r\nc.cristallo1@campus.uniurb.it\r\n\r\nFabio Francescangeli - University of Fribourg, Switzerland\r\nfabio.francescangeli@unifr.ch";
            //
            // authorsPanel
            //
            authorsPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            authorsPanel.AutoScroll = true;
            authorsPanel.Controls.Add(label2);
            authorsPanel.Location = new Point(6, 32);
            authorsPanel.Name = "authorsPanel";
            authorsPanel.Size = new Size(548, 262);
            authorsPanel.TabIndex = 1;
            //
            // groupBox2
            //
            groupBox2.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            groupBox2.Controls.Add(label3);
            groupBox2.Location = new Point(373, 353);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(559, 260);
            groupBox2.TabIndex = 27;
            groupBox2.TabStop = false;
            groupBox2.Text = "Disclaimer";
            // 
            // label3
            // 
            label3.Dock = DockStyle.Fill;
            label3.Location = new Point(3, 23);
            label3.Name = "label3";
            label3.Size = new Size(533, 209);
            label3.TabIndex = 0;
            label3.Text = resources.GetString("label3.Text");
            //
            // textBox1
            //
            textBox1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            textBox1.Location = new Point(373, 623);
            textBox1.Multiline = true;
            textBox1.Name = "textBox1";
            textBox1.ReadOnly = true;
            textBox1.Size = new Size(559, 130);
            textBox1.TabIndex = 28;
            textBox1.Text = "This software is distributed under the MIT License.\r\nPlease cite it as follows in publications:\r\nMangiagalli M., Frontalini F., Cristallo C., Francescangeli F., ForamEcoQS - Foraminiferal Ecological Quality Status, MIT License, 2024";
            //
            // AboutBox1
            //
            AcceptButton = okButton;
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(980, 840);
            Controls.Add(logoPictureBox);
            Controls.Add(textBox1);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Controls.Add(label1);
            Controls.Add(checkUpdatesButton);
            Controls.Add(okButton);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(4, 5, 4, 5);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AboutBox1";
            Padding = new Padding(12, 14, 12, 14);
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "ForamEcoQS";
            Load += AboutBox1_Load;
            ((System.ComponentModel.ISupportInitialize)logoPictureBox).EndInit();
            authorsPanel.ResumeLayout(false);
            authorsPanel.PerformLayout();
            groupBox1.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button okButton;
        private Button checkUpdatesButton;
        private PictureBox logoPictureBox;
        private Label label1;
        private GroupBox groupBox1;
        private Label label2;
        private GroupBox groupBox2;
        private Label label3;
        private TextBox textBox1;
        private Panel authorsPanel;
    }
}
