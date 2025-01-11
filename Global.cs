using System;
using System.IO;
using System.Windows;

namespace FFBitrateViewer
{
    class Global
    {
        private static string _tempDir = Helpers.NormalizeDirSpec(Path.GetTempPath());
        public static string TempDir { get => _tempDir; }

        public static void SetTempDir(string tempDir)
        {
            string dir = Helpers.NormalizeDirSpec(tempDir);

            if (!Directory.Exists(dir))
            {
                try
                {
                    Directory.CreateDirectory(dir);
                }
                catch (Exception)
                {
                    MessageBox.Show("FFBitrateViewer unable to create temp directory:\n" + dir, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(0);
                }
            }

            // Checking write permissions
            try
            {
                using (FileStream fs = File.Create(Path.Combine(dir, Path.GetRandomFileName()), 1, FileOptions.DeleteOnClose)) { }
            }
            catch (Exception)
            {
                MessageBox.Show("FFBitrateViewer unable to create file in temp directory:\n" + dir, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
            }

            _tempDir = dir;
        }
    }
}
