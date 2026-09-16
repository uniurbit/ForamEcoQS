//MIT License
// Windows / WPF entry point.

using System;

namespace ForamEcoQS.Wpf
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            return AppRunner.Run(args, new Eto.Wpf.Platform());
        }
    }
}
