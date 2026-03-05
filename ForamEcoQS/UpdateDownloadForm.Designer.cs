// MIT License
namespace ForamEcoQS
{
    partial class UpdateDownloadForm
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
            statusLabel = new Label();
            fileNameLabel = new Label();
            downloadProgressBar = new ProgressBar();
            cancelButton = new Button();
            closeButton = new Button();
            logListBox = new ListBox();
            SuspendLayout();
            //
            // statusLabel
            //
            statusLabel.AutoSize = true;
            statusLabel.Location = new Point(16, 15);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(221, 20);
            statusLabel.TabIndex = 0;
            statusLabel.Text = "Scaricamento aggiornamento...";
            //
            // fileNameLabel
            //
            fileNameLabel.AutoSize = true;
            fileNameLabel.Location = new Point(16, 40);
            fileNameLabel.Name = "fileNameLabel";
            fileNameLabel.Size = new Size(61, 20);
            fileNameLabel.TabIndex = 4;
            fileNameLabel.Text = "File: ...";
            //
            // downloadProgressBar
            //
            downloadProgressBar.Location = new Point(16, 70);
            downloadProgressBar.Name = "downloadProgressBar";
            downloadProgressBar.Size = new Size(470, 27);
            downloadProgressBar.Style = ProgressBarStyle.Continuous;
            downloadProgressBar.TabIndex = 1;
            //
            // cancelButton
            //
            cancelButton.Location = new Point(273, 199);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(104, 31);
            cancelButton.TabIndex = 2;
            cancelButton.Text = "Annulla";
            cancelButton.UseVisualStyleBackColor = true;
            cancelButton.Click += cancelButton_Click;
            //
            // closeButton
            //
            closeButton.Enabled = false;
            closeButton.Location = new Point(383, 199);
            closeButton.Name = "closeButton";
            closeButton.Size = new Size(104, 31);
            closeButton.TabIndex = 3;
            closeButton.Text = "Chiudi";
            closeButton.UseVisualStyleBackColor = true;
            closeButton.Click += closeButton_Click;
            //
            // logListBox
            //
            logListBox.FormattingEnabled = true;
            logListBox.HorizontalScrollbar = true;
            logListBox.ItemHeight = 20;
            logListBox.Location = new Point(16, 110);
            logListBox.Name = "logListBox";
            logListBox.Size = new Size(470, 84);
            logListBox.TabIndex = 5;
            //
            // UpdateDownloadForm
            //
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(502, 248);
            ControlBox = false;
            Controls.Add(logListBox);
            Controls.Add(closeButton);
            Controls.Add(cancelButton);
            Controls.Add(downloadProgressBar);
            Controls.Add(fileNameLabel);
            Controls.Add(statusLabel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "UpdateDownloadForm";
            ShowIcon = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Aggiornamento in corso";
            Shown += UpdateDownloadForm_Shown;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label statusLabel;
        private Label fileNameLabel;
        private ProgressBar downloadProgressBar;
        private Button cancelButton;
        private Button closeButton;
        private ListBox logListBox;
    }
}
