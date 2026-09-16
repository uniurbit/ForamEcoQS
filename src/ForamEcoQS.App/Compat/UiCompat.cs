//MIT License
// UiCompat.cs - Small shims that keep the ported UI code close to the WinForms original.

using System;
using System.IO;
using System.Reflection;
using Eto.Drawing;
using Eto.Forms;

namespace ForamEcoQS.Compat
{
    /// <summary>
    /// WinForms named the message box icon <c>MessageBoxIcon</c>; Eto calls it
    /// <see cref="MessageBoxType"/>. These constants let the ported call sites stay readable.
    /// </summary>
    public static class MessageBoxIcon
    {
        public const MessageBoxType None = MessageBoxType.Information;
        public const MessageBoxType Information = MessageBoxType.Information;
        public const MessageBoxType Warning = MessageBoxType.Warning;
        public const MessageBoxType Error = MessageBoxType.Error;
        public const MessageBoxType Question = MessageBoxType.Question;
    }

    /// <summary>
    /// Message box helper that defaults the parent window, so dialogs stay attached to the
    /// application on GTK and macOS where a parentless dialog can end up behind the main window.
    /// </summary>
    public static class MessageBox
    {
        private static Control Parent => UiContext.ActiveWindow;

        public static DialogResult Show(string text)
            => Show(text, string.Empty, MessageBoxButtons.OK, MessageBoxType.Information);

        public static DialogResult Show(string text, string caption)
            => Show(text, caption, MessageBoxButtons.OK, MessageBoxType.Information);

        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons)
            => Show(text, caption, buttons, MessageBoxType.Information);

        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxType type)
        {
            var parent = Parent;
            return parent != null
                ? Eto.Forms.MessageBox.Show(parent, text, caption, buttons, type)
                : Eto.Forms.MessageBox.Show(text, caption, buttons, type);
        }

        public static DialogResult Show(Control parent, string text, string caption, MessageBoxButtons buttons, MessageBoxType type)
        {
            return parent != null
                ? Eto.Forms.MessageBox.Show(parent, text, caption, buttons, type)
                : Show(text, caption, buttons, type);
        }
    }

    /// <summary>
    /// Tracks the window that should own modal dialogs. Windows register themselves on show.
    /// </summary>
    public static class UiContext
    {
        private static Window _active;

        public static Window ActiveWindow
        {
            get
            {
                if (_active != null && _active.Loaded)
                {
                    return _active;
                }
                return Application.Instance?.MainForm;
            }
        }

        public static void Register(Window window)
        {
            if (window == null)
            {
                return;
            }

            window.GotFocus += (s, e) => _active = window;
            window.Shown += (s, e) => _active = window;
            window.Closed += (s, e) =>
            {
                if (ReferenceEquals(_active, window))
                {
                    _active = null;
                }
            };
        }
    }

    /// <summary>Embedded logo and application icon, loaded once.</summary>
    public static class AppResources
    {
        private static Bitmap _logo;
        private static Icon _icon;

        public static Bitmap Logo => _logo ??= Load<Bitmap>("ForamEcoQS.Resources.logo.png", s => new Bitmap(s));

        public static Icon AppIcon => _icon ??= LoadIcon();

        private static Icon LoadIcon()
        {
            // .ico support varies by backend; fall back to the PNG logo when it cannot be read.
            try
            {
                var icon = Load<Icon>("ForamEcoQS.Resources.favicon.ico", s => new Icon(s));
                if (icon != null)
                {
                    return icon;
                }
            }
            catch (Exception)
            {
            }

            try
            {
                return Logo == null ? null : new Icon(1f, Logo);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static T Load<T>(string resourceName, Func<Stream, T> factory) where T : class
        {
            try
            {
                using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
                return stream == null ? null : factory(stream);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    /// <summary>Colour helpers used across the ported forms.</summary>
    public static class AppColors
    {
        public static readonly Color Teal = Color.FromArgb(95, 158, 160);
        public static readonly Color SteelBlue = Color.FromArgb(70, 130, 180);
        public static readonly Color SeaGreen = Color.FromArgb(46, 139, 87);
        public static readonly Color CornflowerBlue = Color.FromArgb(100, 149, 237);
        public static readonly Color Orange = Color.FromArgb(255, 165, 0);
        public static readonly Color IndianRed = Color.FromArgb(205, 92, 92);

        /// <summary>Converts an Eto colour to the ARGB triplet ClosedXML expects.</summary>
        public static ClosedXML.Excel.XLColor ToXLColor(this Color color)
            => ClosedXML.Excel.XLColor.FromArgb(color.Rb, color.Gb, color.Bb);
    }

    /// <summary>Common file dialog filters, expressed once.</summary>
    public static class Filters
    {
        public static FileFilter Excel => new FileFilter("Excel Files", ".xlsx", ".xls");
        public static FileFilter Xlsx => new FileFilter("Excel Workbook", ".xlsx");
        public static FileFilter Csv => new FileFilter("CSV Files", ".csv");
        public static FileFilter Json => new FileFilter("JSON File", ".json");
        public static FileFilter Png => new FileFilter("PNG Image", ".png");
        public static FileFilter Jpeg => new FileFilter("JPEG Image", ".jpg", ".jpeg");
        public static FileFilter Pdf => new FileFilter("PDF Document", ".pdf");
        public static FileFilter Svg => new FileFilter("SVG Image", ".svg");
        public static FileFilter Text => new FileFilter("Text Files", ".txt");
        public static FileFilter All => new FileFilter("All Files", ".*");
    }

    /// <summary>Shorthand builders used by the ported layout code.</summary>
    public static class UiHelpers
    {
        /// <summary>Builds a flat, coloured action button like the ones the original used.</summary>
        public static AccentButton ActionButton(string text, Color background, EventHandler<EventArgs> onClick = null, int width = 0)
        {
            var button = new AccentButton
            {
                Text = text,
                AccentColor = background
            };
            if (width > 0)
            {
                button.Width = width;
            }
            if (onClick != null)
            {
                button.Click += onClick;
            }
            return button;
        }

        /// <summary>Applies the application icon and registers the window for dialog parenting.</summary>
        public static T Prepare<T>(this T window) where T : Window
        {
            var icon = AppResources.AppIcon;
            if (icon != null)
            {
                window.Icon = icon;
            }
            UiContext.Register(window);
            return window;
        }

        /// <summary>Ensures the file name carries the extension the chosen filter implies.</summary>
        public static string EnsureExtension(string fileName, string extension)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return fileName;
            }
            return Path.HasExtension(fileName)
                ? fileName
                : fileName + extension;
        }
    }
}
