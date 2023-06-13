using System.IO;

namespace FFBitrateViewer
{
    class Global
    {
        private static string _tempDir = Helpers.NormalizeDirSpec(Path.GetTempPath());
        public static string TempDir { get => _tempDir; set => Helpers.NormalizeDirSpec(value); }
    }
}
