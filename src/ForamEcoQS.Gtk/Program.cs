//MIT License
// Linux / GTK entry point.

using System;

namespace ForamEcoQS.Gtk
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            return AppRunner.Run(args, new Eto.GtkSharp.Platform());
        }
    }
}
