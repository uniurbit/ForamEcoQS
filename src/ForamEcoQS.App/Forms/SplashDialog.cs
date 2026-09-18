//MIT License
// SplashDialog.cs - Startup splash, shown for three seconds before the main window appears.

using System;
using Eto.Drawing;
using Eto.Forms;
using ForamEcoQS.Compat;

namespace ForamEcoQS
{
    public class SplashDialog : Dialog
    {
        private readonly UITimer _closeTimer;

        public SplashDialog()
        {
            Title = "ForamEcoQS";
            WindowStyle = WindowStyle.None;
            Resizable = false;
            ShowInTaskbar = false;
            ClientSize = new Size(500, 300);
            BackgroundColor = Colors.White;

            // There is no main window to act as an owner during startup.
            // Position explicitly, using Eto screen coordinates (including DPI scaling).
            var workingArea = (Screen.FromPoint(Mouse.Position) ?? Screen.PrimaryScreen).WorkingArea;
            Location = new Point(
                (int)(workingArea.X + (workingArea.Width - ClientSize.Width) / 2),
                (int)(workingArea.Y + (workingArea.Height - ClientSize.Height) / 2));

            var logo = AppResources.Logo;
            Content = logo != null
                ? new ImageView { Image = logo, Size = new Size(500, 300) }
                : (Control)new Label
                {
                    Text = "ForamEcoQS",
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Font = SystemFonts.Bold(24)
                };

            _closeTimer = new UITimer { Interval = 3.0 };
            _closeTimer.Elapsed += (s, e) =>
            {
                _closeTimer.Stop();
                Close();
            };

            Shown += (s, e) => _closeTimer.Start();
            Closed += (s, e) => _closeTimer.Stop();

            // Clicking the splash dismisses it early, as users expect.
            MouseDown += (s, e) =>
            {
                _closeTimer.Stop();
                Close();
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _closeTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
