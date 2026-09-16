//MIT License
// AboutBox.cs - Credits, licence, citation and the update checker entry point.

using System;
using System.Reflection;
using Eto.Drawing;
using Eto.Forms;
using ForamEcoQS.Compat;
using MessageBox = ForamEcoQS.Compat.MessageBox;

namespace ForamEcoQS
{
    public class AboutBox : Dialog
    {
        private const string AuthorsText =
            "Matteo Mangiagalli - University of Urbino Carlo Bo, Italy\n" +
            "m.mangiagalli@campus.uniurb.it\n\n" +
            "Fabrizio Frontalini - University of Urbino Carlo Bo, Italy\n" +
            "fabrizio.frontalini@uniurb.it\n\n" +
            "Carla Cristallo - University of Urbino Carlo Bo, Italy\n" +
            "c.cristallo1@campus.uniurb.it\n\n" +
            "Fabio Francescangeli - University of Fribourg, Switzerland\n" +
            "fabio.francescangeli@unifr.ch";

        private const string DisclaimerText =
            "ForamEcoQS is free and open-source research software distributed under the MIT License.\n\n" +
            "The software makes use of the following open-source libraries:\n" +
            "- Eto.Forms: cross-platform user interface toolkit, BSD-3-Clause License\n" +
            "- OxyPlot for plotting, MIT License\n" +
            "- ExcelDataReader: open-source XLS reader, MIT License\n" +
            "- ClosedXML: open-source XLSX writer, MIT License";

        private const string LicenseText =
            "This software is distributed under the MIT License.\n\n" +
            "How to cite:\n" +
            "Mangiagalli, M., Frontalini, F., Cristallo, C., Francescangeli, F., 2026. " +
            "ForamEcoQS: An analytical software suite for foraminiferal ecological quality status assessment. " +
            "SoftwareX 35, 102921.\n" +
            "https://doi.org/10.1016/j.softx.2026.102921";

        private readonly Button _checkUpdatesButton;

        public AboutBox()
        {
            Title = $"About {AssemblyTitle}";
            ClientSize = new Size(900, 720);
            this.Prepare();

            var versionLabel = new Label
            {
                Text = $"ForamEcoQS - Foraminiferal Ecological Quality Status - v{UpdateChecker.CurrentVersionString}",
                Font = SystemFonts.Bold(10)
            };

            var logo = AppResources.Logo;
            Control logoControl = logo != null
                ? new ImageView { Image = logo, Size = new Size(320, 340) }
                : new Panel();

            var authorsGroup = new GroupBox
            {
                Text = "Authors",
                Content = new Scrollable
                {
                    Border = BorderType.None,
                    Content = new Label { Text = AuthorsText }
                }
            };

            var disclaimerGroup = new GroupBox
            {
                Text = "Disclaimer",
                Content = new Scrollable
                {
                    Border = BorderType.None,
                    Content = new Label { Text = DisclaimerText }
                }
            };

            var licenseBox = new TextArea
            {
                Text = LicenseText,
                ReadOnly = true,
                Wrap = true,
                Height = 150
            };

            _checkUpdatesButton = new Button { Text = "Check for Updates", Width = 160 };
            _checkUpdatesButton.Click += CheckUpdatesButton_Click;

            var okButton = new Button { Text = "OK", Width = 100 };
            okButton.Click += (s, e) => Close();
            DefaultButton = okButton;
            AbortButton = okButton;

            var rightColumn = new TableLayout
            {
                Spacing = new Size(0, 10),
                Rows =
                {
                    new TableRow(versionLabel),
                    new TableRow(authorsGroup) { ScaleHeight = true },
                    new TableRow(disclaimerGroup) { ScaleHeight = true },
                    new TableRow(licenseBox)
                }
            };

            Content = new TableLayout
            {
                Padding = new Padding(12, 14),
                Spacing = new Size(12, 10),
                Rows =
                {
                    new TableRow(
                        new TableCell(logoControl),
                        new TableCell(rightColumn, true)) { ScaleHeight = true },
                    new TableRow(new TableCell(new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8,
                        HorizontalContentAlignment = HorizontalAlignment.Right,
                        Items = { _checkUpdatesButton, okButton }
                    }, true), null)
                }
            };
        }

        private async void CheckUpdatesButton_Click(object sender, EventArgs e)
        {
            _checkUpdatesButton.Enabled = false;
            _checkUpdatesButton.Text = "Checking...";

            try
            {
                var result = await UpdateChecker.CheckForUpdatesAsync();

                if (!result.Success)
                {
                    MessageBox.Show(
                        result.ErrorMessage ?? "Unknown error occurred.",
                        "Update Check Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                if (result.IsUpdateAvailable && result.UpdateInfo != null)
                {
                    string message = "A new version is available!\n\n" +
                        $"Current version: {result.CurrentVersion}\n" +
                        $"Latest version: {result.LatestVersion}\n";

                    if (!string.IsNullOrEmpty(result.UpdateInfo.ReleaseDate))
                    {
                        message += $"Release date: {result.UpdateInfo.ReleaseDate}\n";
                    }

                    if (!string.IsNullOrEmpty(result.UpdateInfo.ReleaseNotes))
                    {
                        message += $"\nWhat's new:\n{result.UpdateInfo.ReleaseNotes}\n";
                    }

                    message += "\nWould you like to download the update?";

                    var dialogResult = MessageBox.Show(
                        message,
                        "Update Available",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);

                    if (dialogResult == DialogResult.Yes && !string.IsNullOrEmpty(result.UpdateInfo.DownloadUrl))
                    {
                        using var downloadDialog = new UpdateDownloadDialog(result.UpdateInfo.DownloadUrl);
                        downloadDialog.ShowModal(this);
                    }
                }
                else
                {
                    MessageBox.Show(
                        $"You are running the latest version ({result.CurrentVersion}).",
                        "No Updates Available",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            finally
            {
                _checkUpdatesButton.Enabled = true;
                _checkUpdatesButton.Text = "Check for Updates";
            }
        }

        #region Assembly attribute accessors

        public static string AssemblyTitle
        {
            get
            {
                var assembly = Assembly.GetExecutingAssembly();
                var titleAttribute = assembly.GetCustomAttribute<AssemblyTitleAttribute>();
                if (titleAttribute != null && !string.IsNullOrEmpty(titleAttribute.Title))
                {
                    return titleAttribute.Title;
                }
                return assembly.GetName().Name;
            }
        }

        public static string AssemblyVersion => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";

        public static string AssemblyDescription =>
            Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description ?? string.Empty;

        public static string AssemblyProduct =>
            Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? string.Empty;

        public static string AssemblyCopyright =>
            Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? string.Empty;

        public static string AssemblyCompany =>
            Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? string.Empty;

        #endregion
    }
}
