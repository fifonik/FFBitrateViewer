using System;
using System.Diagnostics;
using System.IO;


namespace FFBitrateViewer
{
    public enum LogLevel
    {
        DEBUG   = 0,
        INFO    = 1,
        WARNING = 2,
        ERROR   = 3
    }


    public class Logger
    {
        private string              FS;
        private StreamWriter?       Stream;
        private readonly bool       AddTimestamp;
        private readonly bool       Append;
        private readonly bool       AutoFlush;
        private bool                IsOpened    = false;
        private bool                IsDisabled  = false;
        public  LogLevel            MinLevel    { get; private set; }
        public  string              FileName    { get { return Path.GetFileName(FS); } }


        public Logger(LogLevel minLogLevel, string fs, bool append = false, bool timestamp = false, bool autoflush = false)
        {
            MinLevel     = minLogLevel;
            FS           = fs;
            Append       = append;
            AddTimestamp = timestamp;
            AutoFlush    = autoflush;
        }


        public void Disable()
        {
            IsDisabled = true;
        }


       public bool Open(string? fs = null)
        {
            if (IsDisabled) return false;
            if (!string.IsNullOrEmpty(fs) && !string.Equals(fs, FS, StringComparison.InvariantCultureIgnoreCase))
            {
                FS = fs;
                if (IsOpened) Close();
            }
            if (!IsOpened && !string.IsNullOrEmpty(FS))
            {
                Stream = new StreamWriter(FS, Append);
                IsOpened = true;
            }
            return IsOpened;
        }


        public void Log(LogLevel level, string line)
        {
            if (IsDisabled || string.IsNullOrEmpty(line)) return;
            if (level < MinLevel) return;

            string logPrefix   = "";
            string debugPrefix = "";

            switch (level)
            {
                case LogLevel.DEBUG:
                    logPrefix = "DEBUG: ";
                    debugPrefix = logPrefix;
                    break;
                case LogLevel.INFO:
                    debugPrefix = "INFO: ";
                    break;
                case LogLevel.WARNING:
                    logPrefix = "WARNING: ";
                    debugPrefix = logPrefix;
                    break;
                case LogLevel.ERROR:
                    logPrefix = "ERROR: ";
                    debugPrefix = logPrefix;
                    break;
            }
            Debug.WriteLine(debugPrefix + line);
            Log(logPrefix + line);
        }


        //public void Log(List<string> line, char separator = '\t', bool? addTimestamp = null)
        //{
        //    Log(string.Join(separator.ToString(), line), addTimestamp);
        //}


        public void Log(string line, bool? addTimestamp = null)
        {
            if (!Open()) return;
            if ((addTimestamp ?? AddTimestamp) == true)
            {
                DateTime now = DateTime.Now;
                Stream?.WriteLine($"{now:yyyy-MM-dd HH:mm:ss}\t" + line);
            }
            else
            {
                Stream?.WriteLine(line);
            }
            if (AutoFlush) Stream?.Flush();
        }


        //public void LogCSV(DataDictionary pairs, int frame, string? exclude = null, char separator = '\t')
        //{
        //    if (!Open()) return;

        //    if (frame == 0)
        //    {
        //        string header = "frame";
        //        foreach (string s in pairs.Keys)
        //        {
        //            if (string.IsNullOrEmpty(exclude) || !exclude.Equals(s))
        //            {
        //                if (header != "") header += separator;
        //                header += s;
        //            }
        //        }
        //        Log(header);
        //    }

        //    string line = "" + frame;
        //    foreach (object? o in pairs.Values)
        //    {
        //        string? s = o?.ToString();
        //        if (string.IsNullOrEmpty(exclude) || !exclude.Equals(s))
        //        {
        //            if (line != "") line += separator;
        //            line += s;
        //        }
        //    }
        //    Log(line);
        //}


        //public void LogCSV(List<DataDictionary> data, string? frameNoKey = null, char separator = '\t')
        //{
        //    for(int frame = 0; frame < data.Count; ++frame) LogCSV(data[frame], frame, frameNoKey, separator);
        //}


        public void Close()
        {
            if (!IsOpened || Stream == null) return;
            IsOpened = false;
            Stream.Flush();
            Stream.Close();
            Stream.Dispose();
        }
    }
}
