//MIT License
// macOS / Cocoa entry point.

using System;

namespace ForamEcoQS.Mac
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            return AppRunner.Run(args, new Eto.Mac.Platform());
        }
    }
}
