using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace FFBitrateViewer
{
    public class PropInfo
    {
        public char Type { get; set; }
        public string Name { get; set; }
        public PropInfo(char type, string name)
        {
            Type = type;
            Name = name;
        }
    }

    public class VersionInfo : Bindable
    {
        public int?         Status          { get { return Get<int?>();     }  set { Set(value); OnPropertyChanged(nameof(IsOK)); OnPropertyChanged(nameof(VerString)); OnPropertyChanged(nameof(InfoString)); } }
        public string?      Message         { get { return Get<string?>();  }  set { Set(value); OnPropertyChanged(nameof(InfoString)); } }
        public bool         IsLocal         { get { return Get<bool>();     }  set { Set(value); OnPropertyChanged(nameof(InfoString)); } }
        public string?      Version         { get { return Get<string?>();  }  set { Set(value); OnPropertyChanged(nameof(InfoString)); } }
        public string?      VersionShort    { get { return Get<string?>();  }  set { Set(value); OnPropertyChanged(nameof(VerString));  } }
        public bool         IsOK            { get { return Status == 0; }   }
        public string?      VerString       { get { return Status == null ? "—" : (Status == 0 ? BuildVerString() : "not found"); } }
        public string?      InfoString      { get { return Status == 0 ? BuildInfoString() : Message; } }


        public void Load()
        {
            //System.Threading.Thread.Sleep(1000);
            var status = FF.VersionInfoGet();
            if (status.Code == 0)
            {
                FF.VersionInfoParse(this, status.Code, status.StdOut);
            }
            else
            {
                Status  = status.Code;
                Message = status.StdErr;
            }
        }


        private string  BuildInfoString() {
            return "Version " + Version + ", found in " + (IsLocal ? "program dir" : "%PATH%");
        }


        private string? BuildVerString()
        {
            return VersionShort;
        }

    }


    public class FF
    {
        private static readonly NumberStyles      DoubleStyle       = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowLeadingWhite;
        private static readonly Regex             VersionRegex      = new(@"^ff\S+\sversion\s+(.+)\sCopyright", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex             VersionShortRegex = new(@"^\D*(\d+\.\d+(?:\.\d+)?)",          RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        public static readonly string             Executable        = "ffprobe";

        public static string? FramesInfoTemplate  { get; set; }
        public static int?    FramesInfoTimeout   { get; set; }
        public static string? MediaInfoTemplate   { get; set; }
        public static int?    MediaInfoTimeout    { get; set; }
        public static string? VersionInfoTemplate { get; set; }
        public static int?    VersionInfoTimeout  { get; set; }


        public static void Init(ProgramConfig config)
        {
            FramesInfoTemplate  = config.FramesInfo.Template;
            FramesInfoTimeout   = config.FramesInfo.Timeout;

            MediaInfoTemplate   = config.MediaInfo.Template;
            MediaInfoTimeout    = config.MediaInfo.Timeout;

            VersionInfoTemplate = config.VersionInfo.Template;
            VersionInfoTimeout  = config.VersionInfo.Timeout;
        }


        public static ExecStatus FramesGet(string fs, CancellationToken? cancellationToken = null, Action<object?>? entryActionOK = null, Action<string?>? entryActionError = null)
        {
            Log.Write(LogLevel.DEBUG, "FF.FramesGet.Started: " + fs);

            var dict = new Dictionary<string, string>
            {
                { "src",     CommandLine.EscapeArgument(fs, "src") },
                { "stream",  "0" }, // todo@ allow to select stream?
                { "threads", Helpers.Threads.ToString() }
            };

            ExecStatus status;
            try
            {
                if (FramesInfoTemplate == null) throw new ArgumentNullException("FramesInfoTemplate is empty");
                var args = Helpers.Subst(FramesInfoTemplate, dict, SubstType.CMD);
                status = Execute.Exec(Executable, args, FramesInfoTimeout, cancellationToken, line => { entryActionOK?.Invoke(FramesGetParseLine(line)); }, entryActionError);
                Log.Write(LogLevel.DEBUG, "FF.FramesGet.Finished: OK");
            }
            catch (Exception e)
            {
                status = new ExecStatus()
                {
                    Code   = -1,
                    StdErr = e.Message
                };
                Log.Write(LogLevel.WARNING, "FF.FramesGet.Finished: FAIL", status.Code.ToString(), status.StdErr);
            }
            return status;
        }


        private static Frame? FramesGetParseLine(string? line)
        {
            if (string.IsNullOrEmpty(line)) return null;
            var p = line.IndexOf('|');
            if (p < 1 || p == line.Length - 1) return null;

            var parts = line[(p + 1)..].Split('|');

            var entryType = line[0..p];
            switch (entryType)
            {
                case "frame":
                    {
                        var ffframe = new FFProbeFrame();

                        foreach (var part in parts)
                        {
                            if (string.IsNullOrEmpty(part)) continue;
                            p = part.IndexOf('=');
                            if (p < 1 || p == part.Length - 1) continue;
                            var k = part[0..p].Trim();
                            var v = part[(p + 1)..].Trim();
                            if (string.IsNullOrEmpty(v)) continue;

                            PropInfo? info = null;
                            switch (k) 
                            {
                                case "best_effort_timestamp_time":
                                    info = new('d', "BestEffortTimestampTime");
                                    break;
                                case "pict_type":
                                    info = new('s', "PictType");
                                    break;
                                case "pkt_duration_time":
                                    info = new('d', "DurationTime");
                                    break;
                                case "pkt_pts_time":
                                    info = new('d', "PTSTime");
                                    break;
                                case "pkt_size":
                                    info = new('i', "Size");
                                    break;
                            }

                            if (info != null) ObjectPropSet(ffframe, info, v);
                        }

                        var frame = Frame.CreateFrame(ffframe);
                        if (frame != null) return frame;
                    }
                    break;

                case "packet":
                    {
                        var ffpacket = new FFProbePacket();

                        foreach (var part in parts)
                        {
                            if (string.IsNullOrEmpty(part)) continue;
                            p = part.IndexOf('=');
                            if (p < 1 || p == part.Length - 1) continue;
                            var k = part[0..p].Trim();
                            var v = part[(p + 1)..].Trim();
                            if (string.IsNullOrEmpty(v)) continue;

                            PropInfo? info = null;
                            switch (k)
                            {
                                case "duration_time":
                                    info = new('d', "DurationTime");
                                    break;
                                case "flags":
                                    info = new('s', "Flags");
                                    break;
                                case "pts_time":
                                    info = new('d', "PTSTime");
                                    break;
                                case "size":
                                    info = new('i', "Size");
                                    break;
                            }

                            if (info != null) ObjectPropSet(ffpacket, info, v);
                        }

                        var frame = Frame.CreateFrame(ffpacket);
                        if (frame != null) return frame;
                    }
                    break;
            }

            return null;
        }


        // todo@ move to Helpers.SetProperty
        public static bool ObjectPropSet(object o, PropInfo info, string? value)
        {
            switch (info.Type)
            {
                case 'b':
                    if (bool.TryParse(value, out bool b))
                    {
                        o.SetProperty(info.Name, b);
                        return true;
                    }
                    break;
                case 'd':
                    if (double.TryParse(value, DoubleStyle, CultureInfo.InvariantCulture, out double d))
                    {
                        o.SetProperty(info.Name, d);
                        return true;
                    }
                    break;
                case 'i':
                    if (int.TryParse(value, out int i))
                    {
                        o.SetProperty(info.Name, i);
                        return true;
                    }
                    break;
                case 's':
                    o.SetProperty(info.Name, value);
                    return true;
            }

            return false;
        }


        public static ExecStatus MediaInfoGet(string fs)
        {
            Log.Write(LogLevel.DEBUG, "FF.MediaInfoGet.Started: " + fs);

            var dict = new Dictionary<string, string>
            {
                { "src",     CommandLine.EscapeArgument(fs, "src") },
                { "threads", Helpers.Threads.ToString() }
            };

            ExecStatus status;
            try
            {
                if (MediaInfoTemplate == null) throw new ArgumentNullException("MediaInfoTemplate is empty");
                var args = Helpers.Subst(MediaInfoTemplate, dict, SubstType.CMD);
                status = Execute.Exec(Executable, args, MediaInfoTimeout);
                Log.Write(LogLevel.DEBUG, "FF.MediaInfoGet.Finished: OK");
            }
            catch (Exception e)
            {
                status = new ExecStatus()
                {
                    Code   = -1,
                    StdErr = e.Message
                };
                Log.Write(LogLevel.WARNING, "FF.MediaInfoGet.Finished: FAIL", status.Code.ToString(), status.StdErr);
            }
            return status;
        }


        public static void MediaInfoGet(MediaInfo info, string fs)
        {
            var status = MediaInfoGet(fs);
            if (status.Code == 0)
            {
                MediaInfoParse(info, status.StdOut);
            }
            else
            {
                info.Status  = status.Code;
                info.Message = status.StdErr;
            }
        }


        /*
            ffprobe -hide_banner -print_format json -loglevel fatal -show_error -show_format -show_streams -show_private_data c:\Vegas\MainconceptBug2\2012-11-22_095841.m2ts
            =>
            {
                "streams": [
                    {
                        "index": 0,
                        "codec_name": "h264",
                        "codec_long_name": "H.264 / AVC / MPEG-4 AVC / MPEG-4 part 10",
                        "profile": "High",
                        "codec_type": "video",
                        "codec_tag_string": "HDMV",
                        "codec_tag": "0x564d4448",
                        "width": 1920,
                        "height": 1080,
                        "coded_width": 1920,
                        "coded_height": 1080,
                        "closed_captions": 0,
                        "has_b_frames": 1,
                        "sample_aspect_ratio": "1:1",
                        "display_aspect_ratio": "16:9",
                        "pix_fmt": "yuv420p",
                        "level": 42,
                        "chroma_location": "left",
                        "field_order": "progressive",
                        "refs": 1,
                        "is_avc": "false",
                        "nal_length_size": "0",
                        "id": "0x1011",
                        "r_frame_rate": "50/1",
                        "avg_frame_rate": "50/1",
                        "time_base": "1/90000",
                        "start_pts": 37315,
                        "start_time": "0.414611",
                        "duration_ts": 1161000,
                        "duration": "12.900000",
                        "bits_per_raw_sample": "8",
                        "disposition": {...}
                    },
                    {
                        "index": 1,
                        "codec_name": "ac3",
                        "codec_long_name": "ATSC A/52A (AC-3)",
                        "codec_type": "audio",
                        "codec_tag_string": "AC-3",
                        "codec_tag": "0x332d4341",
                        "sample_fmt": "fltp",
                        "sample_rate": "48000",
                        "channels": 6,
                        "channel_layout": "5.1(side)",
                        "bits_per_sample": 0,
                        "dmix_mode": "-1",
                        "ltrt_cmixlev": "-1.000000",
                        "ltrt_surmixlev": "-1.000000",
                        "loro_cmixlev": "-1.000000",
                        "loro_surmixlev": "-1.000000",
                        "id": "0x1100",
                        "r_frame_rate": "0/0",
                        "avg_frame_rate": "0/0",
                        "time_base": "1/90000",
                        "start_pts": 33715,
                        "start_time": "0.374611",
                        "duration_ts": 1169280,
                        "duration": "12.992000",
                        "bit_rate": "384000",
                        "disposition": {...}
                    },
                    {
                        "index": 2,
                        "codec_name": "hdmv_pgs_subtitle",
                        "codec_long_name": "HDMV Presentation Graphic Stream subtitles",
                        "codec_type": "subtitle",
                        "codec_tag_string": "[144][0][0][0]",
                        "codec_tag": "0x0090",
                        "width": 1920,
                        "height": 1080,
                        "id": "0x1200",
                        "r_frame_rate": "0/0",
                        "avg_frame_rate": "0/0",
                        "time_base": "1/90000",
                        "start_pts": 33715,
                        "start_time": "0.374611",
                        "disposition": {...}
                    }
                ],
                "format": {
                    "filename": "c:\\Vegas\\MainconceptBug2\\2012-11-22_095841.m2ts",
                    "nb_streams": 3,
                    "nb_programs": 1,
                    "format_name": "mpegts",
                    "format_long_name": "MPEG-TS (MPEG-2 Transport Stream)",
                    "start_time": "0.374611",
                    "duration": "12.992000",
                    "size": "43671552",
                    "bit_rate": "26891349",
                    "probe_score": 50
                }
            }
        */
        private static void MediaInfoParse(MediaInfo info, string text)
        {
            Log.Write(LogLevel.DEBUG, "FF.MediaInfoParse.Started: " + text);

            var data = JsonConvert.DeserializeObject<FFProbeJsonOutput>(text) ?? throw new InvalidOperationException("Could not parse media info (no input)");

            var duration = data.GetDuration();
            if (duration != null) info.Duration = duration;

            if (data.Format?.StartTime != null) info.StartTime = (double)data.Format.StartTime;

            if (data.Format?.BitRate != null) info.BitRate = new BitRate((int)data.Format.BitRate);

            if (data.Streams != null)
            {
                foreach( var stream in data.Streams)
                {
                    switch (stream.CodecType?.ToUpper())
                    {
                        case "VIDEO":
                            if (stream.CodecName?.ToUpper() == "MJPEG") continue; // Attached pics are also added as Video Streams with CodecName = mjpeg (could be png?)
                            var vstream = new VideoStream(stream);
                            Log.Write(LogLevel.DEBUG, "FF.MediaInfoParse.VideoStream: " + vstream.ToString());
                            info.VideoStreams.Add(vstream);
                            break;
                        case "AUDIO":
                            var astream = new AudioStream(stream);
                            Log.Write(LogLevel.DEBUG, "FF.MediaInfoParse.AudioStream: " + astream.ToString());
                            info.AudioStreams.Add(astream);
                            break;
                        case "SUBTITLE":
                            var sstream = new SubtitleStream(stream);
                            Log.Write(LogLevel.DEBUG, "FF.MediaInfoParse.SubtitleStream: " + sstream.ToString());
                            info.SubtitleStreams.Add(sstream);
                            break;
                    }
                }
            }

            if (info.Video0 != null && info.Video0.BitRate == null && info.BitRate != null && info.VideoStreams.Count == 1)
            {
                // Bitrate is often null for video stream, but available for file & audio streams.
                // So when we only have one video stream -- trying to calculate it as (file bitrate - audio bitrate)
                var bitrate = info.BitRate.Value;
                bool isCalculated = true;
                foreach(var astream in info.AudioStreams)
                {
                    if (astream.BitRate == null)
                    {
                        isCalculated = false;
                        break;
                    }
                    else
                    {
                        bitrate -= astream.BitRate.Value;
                    }
                }
                if (isCalculated) {
                    info.Video0.BitRate             = new BitRate(bitrate);
                    info.Video0.IsBitrateCalculated = true;
                }
            }

            info.IsFilled = true;

            Log.Write(LogLevel.DEBUG, "FF.MediaInfoParse.Finished: OK");
        }


        public static ExecStatus VersionInfoGet()
        {
            Log.Write(LogLevel.DEBUG, "FF.VersionInfoGet: Started");

            ExecStatus status;
            try
            {
                if(VersionInfoTemplate == null) throw new ArgumentNullException("VersionInfoTemplate is empty");
                status = Execute.Exec(Executable, VersionInfoTemplate, VersionInfoTimeout);
                Log.Write(LogLevel.DEBUG, "FF.VersionInfoGet: Finished (OK)");
            }
            catch (Exception e)
            {
                status = new ExecStatus()
                {
                    Code   = -1,
                    StdErr = e.Message
                };
                Log.Write(LogLevel.WARNING, "FF.VersionInfoGet: Finished (FAIL)", status.Code.ToString(), status.StdErr);
            }

            return status;
        }


        /*
        > ffprobe -version
        ffprobe version 4.4-full_build-www.gyan.dev Copyright (c) 2007-2021 the FFmpeg developers
          built with gcc 10.2.0 (Rev6, Built by MSYS2 project)
          configuration: --enable-gpl --enable-version3 --enable-shared --disable-w32threads --disable-autodetect --enable-fontconfig --enable-iconv --enable-gnutls --enable-libxml2 --enable-gmp --enable-lzma --enable-libsnappy --enable-zlib --enable-librist --enable-libsrt --enable-libssh --enable-libzmq --enable-avisynth --enable-libbluray --enable-libcaca --enable-sdl2 --enable-libdav1d --enable-libzvbi --enable-librav1e --enable-libsvtav1 --enable-libwebp --enable-libx264 --enable-libx265 --enable-libxvid --enable-libaom --enable-libopenjpeg --enable-libvpx --enable-libass --enable-frei0r --enable-libfreetype --enable-libfribidi --enable-libvidstab --enable-libvmaf --enable-libzimg --enable-amf --enable-cuda-llvm --enable-cuvid --enable-ffnvcodec --enable-nvdec --enable-nvenc --enable-d3d11va --enable-dxva2 --enable-libmfx --enable-libglslang --enable-vulkan --enable-opencl --enable-libcdio --enable-libgme --enable-libmodplug --enable-libopenmpt --enable-libopencore-amrwb --enable-libmp3lame --enable-libshine --enable-libtheora --enable-libtwolame --enable-libvo-amrwbenc --enable-libilbc --enable-libgsm --enable-libopencore-amrnb --enable-libopus --enable-libspeex --enable-libvorbis --enable-ladspa --enable-libbs2b --enable-libflite --enable-libmysofa --enable-librubberband --enable-libsoxr --enable-chromaprint
          libavutil      56. 70.100 / 56. 70.100
          libavcodec     58.134.100 / 58.134.100
          libavformat    58. 76.100 / 58. 76.100
          libavdevice    58. 13.100 / 58. 13.100
          libavfilter     7.110.100 /  7.110.100
          libswscale      5.  9.100 /  5.  9.100
          libswresample   3.  9.100 /  3.  9.100
          libpostproc    55.  9.100 / 55.  9.100

        ffprobe version n5.0.1-4-ga5ebb3d25e-20220506 Copyright (c) 2007-2022 ...
        ffprobe version N-101295-ga5b737e625 Copyright (c) 2007-2021 ...
        ffprobe version 2021-01-12-git-ca21cb1e36-essentials_build-www.gyan.dev Copyright (c) 2007-2021 ...
        */
        public static void VersionInfoParse(VersionInfo info, int status, string text)
        {
            Log.Write(LogLevel.DEBUG, "FF.VersionInfoParse: Started");

            Match match = VersionRegex.Match(text);
            if (!match.Success)
            {
                info.Status = -1;
                info.Message = "Could not parse output (no version found)";
                //throw new InvalidOperationException("Could not parse output (no version found)");
            }

            info.Status       = status;
            info.Message      = text;
            info.Version      = match.Groups[1].Value;
            info.VersionShort = match.Groups[1].Value;

            match = VersionShortRegex.Match(info.Version);
            if (match.Success) info.VersionShort = match.Groups[1].Value;

            info.IsLocal = File.Exists(AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar + Executable);

            Log.Write(LogLevel.DEBUG, "FF.VersionInfoParse: Finished (OK)");
        }


    }
}
