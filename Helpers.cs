using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;

namespace FFBitrateViewer
{
    public enum SubstType
    {
        CMD,
        NONE
    };


    class Helpers
    {
        private static readonly NumberStyles DoubleStyle           = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowLeadingWhite;
        private static readonly Regex        RemoveUnusedRegex2    = new Regex(@"(?:^| )-[a-zA-Z_:-]+ ('?)\{\{[^\}]+\}\}\1(?=$| )", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        //private static readonly Regex        CommaSplitRegex       = new Regex(@"((?:[^,\(]+(?:\([^\)]*\))?)+)(?:,|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly char[]       DirSeparators         = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        public static readonly  int          Threads               = (Environment.ProcessorCount > 2) ? Environment.ProcessorCount - 1 : 0;


        public static string Subst(string template, Dictionary<string, string> pairs, SubstType substType)
        {
            string s = template;

            foreach (var pair in pairs) s = s.Replace("{{" + pair.Key + "}}", pair.Value);

            switch (substType)
            {
                case SubstType.CMD:
                    s = RemoveUnusedRegex2.Replace(s, "");
                    s = s.Replace(",null", "").Replace("null,", "");
                    break;

                case SubstType.NONE:
                    // Do nothing
                    break;
            }

            return s.Trim();
        }


        public static string NormalizeDirSpec(string ds)
        {
            return string.IsNullOrEmpty(ds) ? "" : ds.TrimEnd(DirSeparators) + Path.DirectorySeparatorChar;
        }


        public static string WindowsPath2UnixPath(string? fs, string? escape = null)
        {
            // fs = @"C:\path\to\file.ext";
            // fs = @"\\bin\shared\path\to\file.txt";
            if (string.IsNullOrEmpty(fs)) return "";
            string? root = Path.GetPathRoot(fs)?.TrimEnd(DirSeparators).Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);   // => "C:"
            string? path = (root == null) ? fs : fs[root.Length..];                                                                             // => "/path/to/file.ext"
            if (root != null && !string.IsNullOrEmpty(escape)) root = root.Replace(@":", escape + @":");
            return root + path.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);                                            // => C:/path/to/file.txt
        }


        public static Dictionary<string, string?> ParseLine(string line, char pairsSeparator = ' ', char kvSeparator = ':', bool uppercaseKey = false)
        {
            // Not StringDictionary as it does not preserve keys' case
            var result = new Dictionary<string, string?>();

            string l = line.Trim();
            if (!string.IsNullOrEmpty(l))
            {
                string[] pairs = line.Split(pairsSeparator);

                foreach (string pair in pairs)
                {
                    string s = pair.Trim();
                    if (string.IsNullOrEmpty(s)) continue;

                    int p = s.IndexOf(kvSeparator);
                    if (p > 0 && p < s.Length - 1)
                    {
                        string k = s.Substring(0, p).Trim();
                        string v = s[(p + 1)..].Trim();
                        result.Add(uppercaseKey ? k.ToUpper() : k, v);
                    }
                }
            }

            return result;
        }


        public static bool TryParseDouble(string s, out double result, bool withInfinity = false)
        {
            if (withInfinity)
            {
                if (s.StartsWith("inf", StringComparison.OrdinalIgnoreCase) || s.StartsWith("+inf", StringComparison.OrdinalIgnoreCase))
                {
                    result = double.PositiveInfinity;
                    return true;
                }
                if (s.StartsWith("-inf", StringComparison.OrdinalIgnoreCase))
                {
                    result = double.NegativeInfinity;
                    return true;
                }
            }

            if (double.TryParse(s, DoubleStyle, CultureInfo.CurrentCulture, out double d))
            {
                result = d;
                return true;
            }

            if (double.TryParse(s, DoubleStyle, CultureInfo.InvariantCulture, out d))
            {
                result = d;
                return true;
            }

            result = 0;
            return false;
        }


        // https://blog.codinghorror.com/shortening-long-file-paths/
        [DllImport("shlwapi.dll", CharSet = CharSet.Auto)]
        private static extern bool PathCompactPathEx([Out] StringBuilder pszOut, string szPath, int cchMax, int dwFlags);

        public static string PathShortener(string path, int length)
        {
            StringBuilder sb = new(length + 1);
            PathCompactPathEx(sb, path, length, 0);
            return sb.ToString();
        }


        public static List<string> Split(string s, char separator = ',')
        {
            var result = new List<string>();
            foreach (var x in s.Split(separator)) result.Add(x.Trim());
            return result;
        }


        public static bool IsIntegralNumber(ref object value)
        {
            return value is sbyte
                || value is byte
                || value is short
                || value is ushort
                || value is int
                || value is uint
                || value is long
                || value is ulong
            ;
        }


        public static bool IsFloatingPointNumber(ref object value)
        {
            return value is float
                || value is double
                || value is decimal
            ;
        }

        public static bool IsNumber(ref object value)
        {
            return IsIntegralNumber(ref value) || IsFloatingPointNumber(ref value);
        }

    }


    public static class Extensions
    {
        public static T? GetProperty<T>(this object obj, string propName)
        {
            return (T?)obj.GetType().GetProperty(propName)?.GetValue(obj, null);
        }


        public static void SetProperty(this object obj, string propName, object? value)
        {
            obj.GetType().GetProperty(propName)?.SetValue(obj, value);
        }


        public static void AddRange<TKey, TValue>(this IDictionary<TKey, TValue> source, IDictionary<TKey, TValue> collection)
        {
            if (collection == null) throw new ArgumentNullException("Collection is null"); // todo@ should we silently return

            foreach (var item in collection) if (!source.ContainsKey(item.Key)) source.Add(item.Key, item.Value);
        }


        // Helper to search up the VisualTree
        public static T? FindAncestor<T>(this DependencyObject current) where T : DependencyObject
        {
            do
            {
                if (current is T t) return t;
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return null;
        }


        // Helper to search value for element up the VisualTree
        public static void SetAncestorValue<TParent>(this DependencyObject child, DependencyProperty property, object value) where TParent : DependencyObject
        {
            TParent? parent = child.FindAncestor<TParent>();
            parent?.SetValue(property, value);
        }

    }

}
