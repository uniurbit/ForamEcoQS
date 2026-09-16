//MIT License
// LoadingDialog.cs - Indeterminate/percentage progress window shown while a workbook loads.

using System;
using Eto.Drawing;
using Eto.Forms;
using ForamEcoQS.Compat;

namespace ForamEcoQS
{
    public class LoadingDialog : Form
    {
        private readonly ProgressBar _progressBar;
        private readonly Label _statusLabel;

        public LoadingDialog()
        {
            Title = "Loading...";
            Resizable = false;
            Maximizable = false;
            Minimizable = false;
            ClientSize = new Size(400, 90);
            this.Prepare();

            _statusLabel = new Label { Text = "Loading Excel file..." };
            _progressBar = new ProgressBar { Indeterminate = true, MinValue = 0, MaxValue = 100 };

            Content = new TableLayout
            {
                Padding = new Padding(20, 15),
                Spacing = new Size(0, 8),
                Rows =
                {
                    new TableRow(_statusLabel),
                    new TableRow(_progressBar),
                    null
                }
            };
        }

        /// <summary>Progress callback that can be handed straight to <see cref="ExcelDataLoader"/>.</summary>
        public void Report(string status, int percent)
        {
            Application.Instance.Invoke(() =>
            {
                if (!string.IsNullOrEmpty(status))
                {
                    _statusLabel.Text = status;
                }

                if (percent < 0)
                {
                    _progressBar.Indeterminate = true;
                }
                else
                {
                    _progressBar.Indeterminate = false;
                    _progressBar.Value = Math.Min(100, Math.Max(0, percent));
                }
            });
        }

        public void SetStatus(string status) => Report(status, -1);

        public void SetProgress(int percent) => Report(null, percent);
    }
}
