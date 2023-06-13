namespace FFBitrateViewer
{
    public static class Log
    {
        private static Logger?  Logger        = null;
        public  static bool     IsLogCommands = false;


        public static void Init(Logger logger, bool isLogCommands = false)
        {
            Logger        = logger;
            IsLogCommands = isLogCommands;
        }


        public static void Close()
        {
            Logger?.Close();
        }


        public static bool LogLevelIs(LogLevel logLevel)
        {
            return Logger?.GetMinLevel() == logLevel;
        }


        public static void Write(LogLevel logLevel, string line)
        {
            Logger?.Log(logLevel, line);
        }


        public static void Write(LogLevel logLevel, params string[] values)
        {
            Write(logLevel, string.Join(", ", values));
        }


        public static void WriteCommand(string executable, string? args)
        {
            if(IsLogCommands) Write(LogLevel.INFO, executable + " " + args);
        }

    }
}
