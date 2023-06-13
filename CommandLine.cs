using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;


namespace FFBitrateViewer
{
    // Source: http://csharptest.net/529/how-to-correctly-escape-command-line-arguments-in-c/
    public class CommandLine
    {
        private static readonly Regex invalidChar = new Regex("[\x00\x0a\x0d]"); // these can not be escaped
        private static readonly Regex needsQuotes = new Regex(@"\s|""");         // contains whitespace or two quote characters
        private static readonly Regex escapeQuote = new Regex(@"(\\*)(""|$)");   // one or more '\' followed with a quote or end of string

        public static string EscapeArgument(string? arg, string argNameForExceptions)
        {
            if (arg == null) throw new ArgumentNullException(argNameForExceptions);
            if (invalidChar.IsMatch(arg)) throw new ArgumentOutOfRangeException(argNameForExceptions);

            if (arg == string.Empty) return "\"\"";
            else if (!needsQuotes.IsMatch(arg)) return arg;
            else return '"' + (escapeQuote.Replace(arg, m => m.Groups[1].Value + m.Groups[1].Value + (m.Groups[2].Value == "\"" ? "\\\"" : ""))) + '"';
        }


        public static string EscapeArguments(params string[] args)
        {
            StringBuilder arguments = new();
            for (int i = 0; args != null && i < args.Length; ++i)
            {
                if (args[i] == null) throw new ArgumentNullException("args[" + i + "]");
                if (invalidChar.IsMatch(args[i])) throw new ArgumentOutOfRangeException("args[" + i + "]");

                if (args[i] == string.Empty) { arguments.Append("\"\""); }
                else if (!needsQuotes.IsMatch(args[i])) { arguments.Append(args[i]); }
                else
                {
                    arguments.Append('"');
                    arguments.Append(escapeQuote.Replace(args[i], m => m.Groups[1].Value + m.Groups[1].Value + (m.Groups[2].Value == "\"" ? "\\\"" : "")));
                    arguments.Append('"');
                }
                if (i + 1 < args.Length) arguments.Append(' ');
            }
            return arguments.ToString();
        }


        public static string EscapeArguments(List<string> args)
        {
            return EscapeArguments(args.ToArray());
        }
    }
}

