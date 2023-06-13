using System;
using System.Collections.Generic;
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
        private readonly bool       Timestamp;
        private readonly bool       Append;
        private readonly bool       AutoFlush;
        private bool                IsOpened = false;
        private readonly LogLevel   MinLevel;


        public Logger(LogLevel minLogLevel, string fs, bool append = false, bool timestamp = false, bool autoflush = false)
        {
            MinLevel    = minLogLevel;
            FS          = fs;
            Append      = append;
            Timestamp   = timestamp;
            AutoFlush   = autoflush;
        }


        public LogLevel GetMinLevel()
        {
            return MinLevel;
        }


        public bool IsEmpty()
        {
            return !File.Exists(FS);
        }


        public bool Open(string? fs = null)
        {
            if (!string.IsNullOrEmpty(fs))
            {
                FS = fs;
                if (IsOpened) Close(); // todo@ why?
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
#if DEBUG
            if(level == LogLevel.DEBUG) Debug.WriteLine("DEBUG: " + line);
#endif
            if (level < MinLevel) return;

            string l = "";
            string d = "";

            switch (level)
            {
                case LogLevel.DEBUG:
                    l = "DEBUG: ";
                    d = l;
                    break;
                case LogLevel.INFO:
                    //l = "INFO: ";
                    d = "INFO: ";
                    break;
                case LogLevel.WARNING:
                    l = "WARNING: ";
                    d = l;
                    break;
                case LogLevel.ERROR:
                    l = "ERROR: ";
                    d = l;
                    break;
            }
            Debug.WriteLine(d + line);
            Log(l + line);
        }


        public void Log(List<string> line, char separator = '\t', bool? timestamp = null)
        {
            Log(string.Join(separator.ToString(), line), timestamp);
        }


        public void Log(string line, bool? timestamp = null)
        {
            if (!Open()) return;
            if (timestamp == null) timestamp = Timestamp;
            if (timestamp == true)
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

        public void LogCSV(DataDictionary pairs, int frame, string? exclude = null, char separator = '\t')
        {
            if (!Open()) return;

            if (frame == 0)
            {
                string header = "frame";
                foreach (string s in pairs.Keys)
                {
                    if (string.IsNullOrEmpty(exclude) || !exclude.Equals(s))
                    {
                        if (header != "") header += separator;
                        header += s;
                    }
                }
                Log(header);
            }

            string line = "" + frame;
            foreach (object? o in pairs.Values)
            {
                string? s = o?.ToString();
                if (string.IsNullOrEmpty(exclude) || !exclude.Equals(s))
                {
                    if (line != "") line += separator;
                    line += s;
                }
            }
            Log(line);
        }


        public void LogCSV(List<DataDictionary> data, string? frameNoKey = null, char separator = '\t')
        {
            for(int frame = 0; frame < data.Count; ++frame) LogCSV(data[frame], frame, frameNoKey, separator);
        }



        public void Close()
        {
            if (!IsOpened || Stream == null) return;
            Stream.Flush();
            Stream.Close();
            Stream.Dispose();
            IsOpened = false;
        }
    }
}
