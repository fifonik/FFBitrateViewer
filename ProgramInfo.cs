using System.Diagnostics;
using System.Globalization;
using System.Reflection;



namespace FFBitrateViewer
{
    public static class ProgramInfo
    {
        public static string?          Name            { get; private set; }
        public static string?          Version         { get; private set; }
        public static FileVersionInfo? VersionInfo     { get; private set; }


        static ProgramInfo()
        {
            var assembly = Assembly.GetExecutingAssembly();
            Name = assembly.GetName().Name;

            var VersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            Version = string.Format(CultureInfo.InvariantCulture, @"{0}.{1}", VersionInfo.ProductMajorPart, VersionInfo.ProductMinorPart);
            if (VersionInfo.ProductBuildPart > 0) Version += "." + VersionInfo.ProductBuildPart.ToString(CultureInfo.InvariantCulture);
            if (VersionInfo.ProductPrivatePart > 0)
            {
                Version += "b";
                if (VersionInfo.ProductPrivatePart > 1) Version += VersionInfo.ProductPrivatePart.ToString(CultureInfo.InvariantCulture);
            }
        }

    }
}
