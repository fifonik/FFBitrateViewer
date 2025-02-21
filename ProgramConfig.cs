using System.Text.RegularExpressions;

namespace FFBitrateViewer
{
    public class FFProbeFramesInfoConfig
    {
        // -select_streams {{stream}} -show_entries frame=pkt_pos,best_effort_timestamp_time,pict_type,pkt_dts_time,pkt_duration_time,pkt_pts_time,pkt_size
        // While getting information about frames/packets using compact format instead of json as it is:
        // - Smaller;
        // - Easier to parse while reading stdout.
        public string Template { get; set; } = "-hide_banner -threads {{threads}} -print_format compact -loglevel fatal -show_error -select_streams v:{{stream}} -show_entries packet=pos,dts_time,duration_time,pts_time,size,flags {{src}}";

        public int    Timeout  { get; set; } = 60_000; // milliseconds
    }


    public class FFProbeMediaInfoConfig
    {
        // ? "-show_entries stream_tags=duration" is required as without this the duration is not always returned
        // "-count_frames -count_packets" -- calculate frames & packets counts per stream and (returned in nb_frames/nb_packets), but this is much slower.
        // "-find_stream_info" -- fill-in missing information by actually read the streams instead of just parsing the header(s). It helps with corrupted files.
        // "-probesize 10000000" and "-analyzeduration 2000000" cab be used with -find_stream_info
        public string Template { get; set; } = "-hide_banner -threads {{threads}} -print_format json=compact=1 -loglevel fatal -show_error -show_format -show_streams -show_entries stream_tags=duration {{src}}";

        public int    Timeout  { get; set; } = 30_000; // milliseconds
    }


    public class FFProbeVersionInfoConfig
    {
        // -hide_banner does not work with -version, also the version info is in the banner
        public string Template { get; set; } = "-version";
        public int    Timeout  { get; set; } = 30_000; // milliseconds
    }


    public class ProgramConfig
    {
        private static readonly string  DefaultVideoFilesList = "*.264;*.avi;*.avs;*.h264;*.hevc;*.m2ts;*.m4v;*.mkv;*.mov;*.mp4;*.mpeg;*.mpg;*.mts;*.mxf;*.ts;*.webm";
        private static readonly Regex   VideoFilesListRegexp  = new Regex(@"^(\*\.[a-z0-9]{1,}(?:$|;))+", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private string                  _videoFilesList = DefaultVideoFilesList;
        public FFProbeFramesInfoConfig  FramesInfo     { get; set; } = new();
        public FFProbeMediaInfoConfig   MediaInfo       { get; set; } = new();
        //public PlotParams               Plots           { get; set; } = new();
        public FFProbeVersionInfoConfig VersionInfo     { get; set; } = new();
        public string                   VideoFilesList  { get { return _videoFilesList; } set { _videoFilesList = string.IsNullOrEmpty(value) || !IsVideoFilesListValid(value) ? DefaultVideoFilesList : value; } }
        public string?                  TempDir         { get; set; }


        public static ProgramConfig LoadFromFile(string fs)
        {
            var result = Json.FileRead<ProgramConfig>(fs) ?? new ProgramConfig();

            result.FramesInfo  ??= new FFProbeFramesInfoConfig();
            result.MediaInfo   ??= new FFProbeMediaInfoConfig();
            //result.Plots       ??= new PlotParams();
            result.VersionInfo ??= new FFProbeVersionInfoConfig();

            return result;
        }

        public static bool IsVideoFilesListValid(string videoFilesList)
        {
            Match match = VideoFilesListRegexp.Match(videoFilesList);
            if(!match.Success) Log.Write(LogLevel.WARNING, "VideoFilesList specified in conf has invalid format and ignored");
            return match.Success;
        }
    }

}
