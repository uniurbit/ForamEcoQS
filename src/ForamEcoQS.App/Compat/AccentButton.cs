//MIT License
// AccentButton.cs - A flat, accent-coloured push button.
//
// The original WinForms UI relied on Button.BackColor to colour its action buttons. Eto maps
// that onto the native control, and GTK's themed buttons ignore it - which left white text on
// a light background. Drawing the button ourselves keeps the intended look identical on GTK,
// WPF and Cocoa.

using System;
using Eto.Drawing;
using Eto.Forms;

namespace ForamEcoQS.Compat
{
    public class AccentButton : Drawable
    {
        private const int CornerRadius = 4;
        private const int HorizontalPadding = 14;
        private const int DefaultHeight = 30;

        private string _text = string.Empty;
        private Color _accentColor = Color.FromArgb(70, 130, 180);
        private bool _hovered;
        private bool _pressed;

        public AccentButton()
        {
            CanFocus = true;
            Paint += OnPaint;

            MouseEnter += (s, e) => { _hovered = true; Invalidate(); };
            MouseLeave += (s, e) => { _hovered = false; _pressed = false; Invalidate(); };

            MouseDown += (s, e) =>
            {
                if (e.Buttons != MouseButtons.Primary || !Enabled)
                {
                    return;
                }
                _pressed = true;
                Focus();
                Invalidate();
                e.Handled = true;
            };

            MouseUp += (s, e) =>
            {
                if (e.Buttons != MouseButtons.Primary)
                {
                    return;
                }
                bool wasPressed = _pressed;
                _pressed = false;
                Invalidate();
                if (wasPressed && Enabled)
                {
                    PerformClick();
                }
                e.Handled = true;
            };

            KeyDown += (s, e) =>
            {
                if (!Enabled || (e.Key != Keys.Space && e.Key != Keys.Enter))
                {
                    return;
                }
                PerformClick();
                e.Handled = true;
            };

            GotFocus += (s, e) => Invalidate();
            LostFocus += (s, e) => Invalidate();
        }

        public event EventHandler<EventArgs> Click;

        public string Text
        {
            get => _text;
            set
            {
                _text = value ?? string.Empty;
                UpdateMinimumSize();
                Invalidate();
            }
        }

        public Color AccentColor
        {
            get => _accentColor;
            set
            {
                _accentColor = value;
                Invalidate();
            }
        }

        public void PerformClick() => Click?.Invoke(this, EventArgs.Empty);

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            Invalidate();
        }

        /// <summary>
        /// Sizes the button to its caption. Eto lays a Drawable out from MinimumSize, so the
        /// measurement has to be pushed there rather than returned from a size override.
        /// </summary>
        private void UpdateMinimumSize()
        {
            var font = SystemFonts.Default();
            var textSize = font.MeasureString(string.IsNullOrEmpty(_text) ? " " : _text);
            MinimumSize = new Size(
                (int)Math.Ceiling(textSize.Width) + HorizontalPadding * 2,
                Math.Max(DefaultHeight, (int)Math.Ceiling(textSize.Height) + 10));
        }

        private void OnPaint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.AntiAlias = true;

            var bounds = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }

            Color fill = _accentColor;
            Color textColor = Colors.White;

            if (!Enabled)
            {
                // Wash the accent out towards the window background so it reads as disabled.
                fill = Color.Blend(_accentColor, Colors.White, 0.55f);
                textColor = Color.FromArgb(245, 245, 245);
            }
            else if (_pressed)
            {
                fill = Color.Blend(_accentColor, Colors.Black, 0.18f);
            }
            else if (_hovered)
            {
                fill = Color.Blend(_accentColor, Colors.White, 0.12f);
            }

            var path = GraphicsPath.GetRoundRect(bounds, CornerRadius);
            g.FillPath(fill, path);
            g.DrawPath(Color.Blend(fill, Colors.Black, 0.12f), path);

            if (HasFocus && Enabled)
            {
                var focusRect = new RectangleF(bounds.X + 2, bounds.Y + 2, bounds.Width - 4, bounds.Height - 4);
                g.DrawPath(Color.FromArgb(255, 255, 255, 150), GraphicsPath.GetRoundRect(focusRect, CornerRadius - 1));
            }

            if (string.IsNullOrEmpty(_text))
            {
                return;
            }

            var font = SystemFonts.Default();
            var textSize = g.MeasureString(font, _text);
            g.DrawText(font, textColor,
                (Width - textSize.Width) / 2f,
                (Height - textSize.Height) / 2f,
                _text);
        }
    }
}
