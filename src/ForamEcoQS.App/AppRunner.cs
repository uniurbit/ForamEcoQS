//MIT License
// AppRunner.cs - Shared startup for every platform head.
//
// Each platform project (GTK, WPF, macOS) is a thin executable that supplies its Eto platform
// and delegates here, so the startup sequence lives in one place.

using System;
using Eto.Forms;

namespace ForamEcoQS
{
    public static class AppRunner
    {
        /// <summary>
        /// Runs ForamEcoQS. With command line arguments it behaves as a headless batch tool and
        /// never initialises a UI platform; otherwise it shows the splash screen and main window.
        /// </summary>
        public static int Run(string[] args, Eto.Platform platform)
        {
            if (args != null && args.Length > 0)
            {
                return CliRunner.Run(args);
            }

            // Legacy .xls files need the Windows code pages, on every operating system.
            ExcelDataLoader.EnsureEncodingProvider();

            var application = new Application(platform);

            application.UnhandledException += (s, e) =>
            {
                try
                {
                    var error = e.ExceptionObject as Exception;
                    MessageBox.Show(
                        "An unexpected error occurred:\n\n" + (error?.Message ?? e.ExceptionObject?.ToString()),
                        "ForamEcoQS", MessageBoxButtons.OK, MessageBoxType.Error);
                }
                catch (Exception)
                {
                    // Never let the error reporter take the application down.
                }
            };

            application.Initialized += (s, e) =>
            {
                ShowSplash();

                var mainWindow = new MainWindow();
                application.MainForm = mainWindow;
                mainWindow.Show();
            };

            application.Run();
            return 0;
        }

        private static void ShowSplash()
        {
            try
            {
                using var splash = new SplashDialog();
                splash.ShowModal();
            }
            catch (Exception)
            {
                // A splash screen is never worth failing startup over.
            }
        }
    }
}
