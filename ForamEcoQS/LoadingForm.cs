//MIT License

using System;
using System.Drawing;
using System.Windows.Forms;

namespace ForamEcoQS
{
    public class LoadingForm : Form
    {
        private readonly ProgressBar _progressBar;
        private readonly Label _statusLabel;

        public LoadingForm()
        {
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Loading...";
            Size = new Size(400, 120);
            ControlBox = false;
            MaximizeBox = false;
            MinimizeBox = false;

            _statusLabel = new Label
            {
                Text = "Loading Excel file...",
                Location = new Point(20, 15),
                Size = new Size(350, 20),
                AutoSize = false
            };

            _progressBar = new ProgressBar
            {
                Location = new Point(20, 40),
                Size = new Size(345, 25),
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30
            };

            Controls.Add(_statusLabel);
            Controls.Add(_progressBar);
        }

        public void SetStatus(string status)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(SetStatus), status);
                return;
            }
            _statusLabel.Text = status;
            Application.DoEvents();
        }

        public void SetProgress(int percent)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<int>(SetProgress), percent);
                return;
            }
            if (_progressBar.Style == ProgressBarStyle.Marquee)
            {
                _progressBar.Style = ProgressBarStyle.Continuous;
            }
            _progressBar.Value = Math.Min(100, Math.Max(0, percent));
            Application.DoEvents();
        }
    }
}
