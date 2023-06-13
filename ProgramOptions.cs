//using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections.Generic;


namespace FFBitrateViewer
{
    public class FileItemPO
    {
        //[JsonProperty("enabled")]
        public bool IsEnabled { get; set; } = true;
        //[JsonProperty("filespec")]
        public string? FS { get; set; }

        public FileItemPO() { }
        public FileItemPO(string fs, bool enabled = true)
        {
            FS        = fs;
            IsEnabled = enabled;
        }
        public FileItemPO(FileItem file)
        {
            FS        = file.FS;
            IsEnabled = file.IsEnabled;
        }
    }


    public class ProgramOptions
    {
        private static readonly char Separator = '|'; // For serializing lists (Files)

        public bool?            AdjustStartTimeOnPlot { get; set; }
        public bool?            LogCommands           { get; set; }
        public string?          TempDir               { get; set; }
        //[JsonProperty("files")]
        public List<FileItemPO> Files                 { get; set; }

        public ProgramOptions()
        {
            Files = new List<FileItemPO>();
        }


        public void Add(ProgramOptions options)
        {
            if (options.Files.Count > 0)
            {
                Files.Clear();
                Files.AddRange(options.Files);
            }
            if (options.AdjustStartTimeOnPlot != null) AdjustStartTimeOnPlot = options.AdjustStartTimeOnPlot;
            if (options.LogCommands != null)           LogCommands           = options.LogCommands;
        }


        public static ProgramOptions LoadFromSettings()
        {
            var result = new ProgramOptions();

            var source = Properties.Settings.Default;

            result.AdjustStartTimeOnPlot = string.Equals(source[nameof(AdjustStartTimeOnPlot)]?.ToString(), "True", StringComparison.OrdinalIgnoreCase);
            result.LogCommands           = string.Equals(source[nameof(LogCommands)]?.ToString(), "True", StringComparison.OrdinalIgnoreCase);

            result.Files.Clear();
            result.Files.AddRange(FilesDeserialize(source[nameof(Files)]?.ToString()));

            return result;
        }


        public void SaveToSettings()
        {
            var target = Properties.Settings.Default;

            target[nameof(AdjustStartTimeOnPlot)] = AdjustStartTimeOnPlot == true;
            target[nameof(LogCommands)]           = LogCommands == true;

            target[nameof(Files)]                 = FilesSerialize(Files);

            target.Save();
        }


        private static string FilesSerialize(List<FileItemPO> files)
        {
            var items = new List<string>();
            foreach (var file in files) items.Add((file.IsEnabled ? "1" : "0") + file.FS);
            return string.Join(Separator.ToString(), items);
        }


        private static List<FileItemPO> FilesDeserialize(string? serialized)
        {
            var result = new List<FileItemPO>();
            if (serialized != null)
            {
                string[] lines = serialized.Split(Separator);
                foreach (var line in lines) if (!string.IsNullOrEmpty(line)) result.Add(new FileItemPO(line[1..], line[0] == '1'));
            }
            return result;
        }

    }


    // FFBitrateViewer.exe
    // [-adjust-start-time-on-plot]
    // [-exit]
    // [-log-commands]
    // [-log-level=(DEBUG|ERROR|INFO|WARNING)]
    // [-run]
    // [-temp-dir=<dirspec>]
    // /path/to/file1.mp4 [/path/to/file2.mp4] [...]

    public class ArgsOptions : ProgramOptions
    {
        public bool     IsFilled { get { return Files.Count > 0; } }
        public LogLevel LogLevel { get; set; } = LogLevel.INFO;
        public bool     Exit     { get; set; }
        public bool     Run      { get; set; }

        public ArgsOptions(string[] args) : base()
        {
            int count = args.Length;
            int i = 1; /*skipping executable path in 0*/
            for (; i < count; ++i)
            {
                string s = args[i];
                if (s.StartsWith("--"))
                {
                    s = s[2..];
                }
                else if (s[0] == '-' || s[0] == '/')
                {
                    s = s[1..];
                } else {
                    break; // Not an option -- exiting options processing loop. The next parameters will be treated as file name
                }

                var parts      = s.Split('=');
                string? svalue = (parts.Length > 1) ? parts[1].Trim() : null;
                bool bvalue    = string.IsNullOrEmpty(svalue) || string.Equals(svalue, "1") || string.Equals(svalue, "true", StringComparison.OrdinalIgnoreCase);

                switch (parts[0].ToUpper())
                {
                    case "ADJUST-START-TIME-ON-PLOT":
                        AdjustStartTimeOnPlot = bvalue;
                        continue;
                    case "EXIT":
                        Exit = bvalue;
                        continue;
                    case "LOG-COMMANDS":
                        LogCommands = bvalue;
                        continue;
                    case "LOG-LEVEL":
                        if (!string.IsNullOrEmpty(svalue))
                        {
                            switch (svalue.ToUpper())
                            {
                                case "DEBUG":
                                    LogLevel = LogLevel.DEBUG;
                                    continue;
                                case "ERROR":
                                    LogLevel = LogLevel.ERROR;
                                    continue;
                                case "INFO":
                                    LogLevel = LogLevel.INFO;
                                    continue;
                                case "WARNING":
                                    LogLevel = LogLevel.WARNING;
                                    break;
                            }
                        }
                        // todo@ warning?
                        break;
                    case "RUN":
                        Run = bvalue;
                        continue;
                    case "TEMP-DIR":
                        if (!string.IsNullOrEmpty(svalue))
                        {
                            svalue = svalue.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                            if (!Directory.Exists(svalue)) Directory.CreateDirectory(svalue);
                            TempDir = svalue;
                            continue;
                        }
                        break; // warning?
                }
            }

            while (i < count) Files.Add(new FileItemPO(args[i++]));
        }
    }



}
