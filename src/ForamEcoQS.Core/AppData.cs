//MIT License
// AppData.cs - Single definition of where ForamEcoQS keeps its per-user files.
//
// Environment.SpecialFolder.ApplicationData resolves to %APPDATA% on Windows,
// ~/.config on Linux and ~/.config (via XDG) on macOS, so the same code works everywhere.

using System;
using System.IO;

namespace ForamEcoQS
{
    public static class AppData
    {
        private static string _directory;

        /// <summary>
        /// The per-user ForamEcoQS directory, created on first access.
        /// </summary>
        public static string Directory
        {
            get
            {
                if (_directory != null)
                {
                    return _directory;
                }

                string root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                if (string.IsNullOrEmpty(root))
                {
                    // Some minimal Linux containers report no ApplicationData folder.
                    root = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
                }

                string path = Path.Combine(root, "ForamEcoQS");
                try
                {
                    System.IO.Directory.CreateDirectory(path);
                }
                catch (Exception)
                {
                    // Fall back to the application folder when the home directory is read-only.
                    path = AppDomain.CurrentDomain.BaseDirectory;
                }

                _directory = path;
                return _directory;
            }
        }

        /// <summary>Full path of a file inside the per-user ForamEcoQS directory.</summary>
        public static string File(string fileName) => Path.Combine(Directory, fileName);
    }
}
