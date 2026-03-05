using System;
using System.Drawing;
using System.Windows.Forms;
using ForamEcoQS.Properties;
using Timer = System.Windows.Forms.Timer;

namespace ForamEcoQS
{
    public class SplashForm : Form
    {
        private readonly Timer _closeTimer;

        public SplashForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;
            ShowInTaskbar = false;
            Size = new Size(500, 300);

            PictureBox logoBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                Image = Resources.logo,
                SizeMode = PictureBoxSizeMode.Zoom
            };

            Controls.Add(logoBox);

            _closeTimer = new Timer
            {
                Interval = 3000
            };
            _closeTimer.Tick += HandleTimerTick;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _closeTimer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _closeTimer.Dispose();
            }

            base.Dispose(disposing);
        }

        private void HandleTimerTick(object? sender, EventArgs e)
        {
            _closeTimer.Stop();
            Close();
        }
    }
}
