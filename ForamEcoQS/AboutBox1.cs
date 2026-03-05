// MIT License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ForamEcoQS
{
    partial class AboutBox1 : Form
    {
        public AboutBox1()
        {
            InitializeComponent();
            this.Text = String.Format("About {0}", AssemblyTitle);

            // Set version dynamically
            label1.Text = $"ForamEcoQS - Foraminiferal Ecological Quality Status - v{UpdateChecker.CurrentVersionString}";
        }

        private async void checkUpdatesButton_Click(object sender, EventArgs e)
        {
            // Disable button while checking
            checkUpdatesButton.Enabled = false;
            checkUpdatesButton.Text = "Checking...";

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
                    string message = $"A new version is available!\n\n" +
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
                        using var downloadForm = new UpdateDownloadForm(result.UpdateInfo.DownloadUrl);
                        downloadForm.ShowDialog(this);
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
                // Re-enable button
                checkUpdatesButton.Enabled = true;
                checkUpdatesButton.Text = "Check for Updates";
            }
        }

        #region Assembly attribute accessors

        public string AssemblyTitle
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0)
                {
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    if (titleAttribute.Title != "")
                    {
                        return titleAttribute.Title;
                    }
                }
                return Assembly.GetExecutingAssembly().GetName().Name;
            }
        }

        public string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        public string AssemblyDescription
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyDescriptionAttribute)attributes[0]).Description;
            }
        }

        public string AssemblyProduct
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyProductAttribute)attributes[0]).Product;
            }
        }

        public string AssemblyCopyright
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
            }
        }

        public string AssemblyCompany
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCompanyAttribute)attributes[0]).Company;
            }
        }
        #endregion

       

        private void textBoxDescription_TextChanged(object sender, EventArgs e)
        {

        }

        private void AboutBox1_Load(object sender, EventArgs e)
        {

        }
    }
}
