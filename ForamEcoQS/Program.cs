//MIT License

namespace ForamEcoQS
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                int exitCode = CliRunner.Run(args);
                Environment.Exit(exitCode);
                return;
            }

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            using (SplashForm splash = new SplashForm())
            {
                splash.ShowDialog();
            }

            Application.Run(new ForamEcoQS());
        }
    }
}
