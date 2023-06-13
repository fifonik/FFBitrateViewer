using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace FFBitrateViewer
{
    public class FFProbeFormat
    {
        [JsonProperty("bit_rate")]
        public int? BitRate { get; set; }

        /// <summary>Approximate duration in seconds (stream can start *after* the 00:00:00 timecode).</summary>
        [JsonProperty("duration")]
        public double? Duration { get; set; }

        [JsonProperty("filename")]
        public string? FileName { get; set; }

        [JsonProperty("format_long_name")]
        public string? FormatLongName { get; set; }

        [JsonProperty("format_name")]
        public string? FormatName { get; set; }

        [JsonProperty("probe_score")]
        public int? ProbeScore { get; set; }

        [JsonProperty("nb_programs")]
        public int? ProgramsCount { get; set; }

        [JsonProperty("size")]
        public long? Size { get; set; }

        [JsonProperty("start_time")]
        public double? StartTime { get; set; }

        [JsonProperty("nb_streams")]
        public int? StreamsCount { get; set; }

        /// <summary>Container and format tags/metadata, not stream-specific tags.</summary>
        [JsonProperty("tags")]
        public Dictionary<string, string?>? Tags { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object?>? ExtensionData { get; set; }
    }


    public class FFProbeFrame
    {
        [JsonProperty("best_effort_timestamp")]
        public int? BestEffortTimestamp { get; set; }

        [JsonProperty("best_effort_timestamp_time")]
        public double? BestEffortTimestampTime { get; set; }

        [JsonProperty("channel_layout")]
        public string? ChannelLayout { get; set; }

        [JsonProperty("channels")]
        public int? Channels { get; set; }

        [JsonProperty("coded_picture_number")]
        public int? CodedPictureNumber { get; set; }

        [JsonProperty("chroma_location")]
        public string? ChromaLocation { get; set; }

        [JsonProperty("display_picture_number")]
        public int? DisplayPictureNumber { get; set; }

        [JsonProperty("pkt_dts")]
        public int? DTS { get; set; }

        [JsonProperty("pkt_dts_time")]
        public double? DTSTime { get; set; }

        [JsonProperty("pkt_duration")]
        public int? Duration { get; set; }

        [JsonProperty("pkt_duration_time")]
        public double? DurationTime { get; set; }

        [JsonProperty("height")]
        public int? Height { get; set; }

        [JsonProperty("interlaced_frame")]
        public bool? InterlacedFrame { get; set; } // todo@ type

        [JsonProperty("key_frame")]
        public bool? IsKeyFrame { get; set; } // todo@ type

        [JsonProperty("media_type")]
        public string? MediaType { get; set; }

        [JsonProperty("nb_samples")]
        public int? NBSamples { get; set; }

        [JsonProperty("pict_type")]
        public string? PictType { get; set; } // I, P, B

        [JsonProperty("pix_fmt")]
        public string? PixFmt { get; set; }

        [JsonProperty("pkt_pos")]
        public int? Pos { get; set; }

        [JsonProperty("pkt_pts")]
        public int? PTS { get; set; }

        [JsonProperty("pkt_pts_time")]
        public double? PTSTime { get; set; }

        [JsonProperty("repeat_pict")]
        public bool? RepeatPict { get; set; } // todo@ type

        [JsonProperty("sample_aspect_ratio")]
        public string? SAR { get; set; }

        [JsonProperty("pkt_size")]
        public int? Size { get; set; }

        [JsonProperty("sample_fmt")]
        public string? SampleFmt { get; set; }

        [JsonProperty("stream_index")]
        public int? StreamIndex { get; set; }

        [JsonProperty("top_field_first")]
        public bool? TopFieldFirst { get; set; } // todo@ type

        [JsonProperty("width")]
        public int? Width { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object?>? ExtensionData { get; set; }
    }


    public class FFProbePacket
    {
        [JsonProperty("codec_type")]
        public string? CodecType { get; set; }

        // decoding time stamp -- how packets stored in stream
        [JsonProperty("dts")]
        public int? DTS { get; set; }

        [JsonProperty("dts_time")]
        public double? DTSTime { get; set; }

        [JsonProperty("duration")]
        public int? Duration { get; set; }

        [JsonProperty("duration_time")]
        public double? DurationTime { get; set; }

        [JsonProperty("flags")]
        public string? Flags { get; set; }

        // presentation time stamp -- how packets should be displayed
        [JsonProperty("pts")]
        public int? PTS { get; set; }

        [JsonProperty("pts_time")]
        public double? PTSTime { get; set; }

        [JsonProperty("size")]
        public int? Size { get; set; }

        [JsonProperty("stream_index")]
        public int? StreamIndex { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object?>? ExtensionData { get; set; }
    }


    public class FFProbeStream
    {
        [JsonProperty("bit_rate")]
        public int? BitRate { get; set; }

        [JsonProperty("bits_per_sample")]
        public int? BitsPerSample { get; set; }

        [JsonProperty("bits_per_raw_sample")]
        public string? BitsPerSampleRaw { get; set; }

        [JsonProperty("channel_layout")]
        public string? ChannelLayout { get; set; }

        [JsonProperty("channels")]
        public int? Channels { get; set; }

        [JsonProperty("chroma_location")]
        public string? ChromaLocation { get; set; }

        [JsonProperty("codec_name")]
        public string? CodecName { get; set; }

        /// <summary>H.264 / AVC / MPEG-4 AVC / MPEG-4 part 10</summary>
        [JsonProperty("codec_long_name")]
        public string? CodecLongName { get; set; }

        [JsonProperty("codec_type")]
        public string? CodecType { get; set; }

        [JsonProperty("codec_tag")]
        public string? CodecTag { get; set; }

        /// <summary>Video codec's FourCC or audio codec's TwoCC</summary>
        [JsonProperty("codec_tag_string")]
        public string? CodecTagString { get; set; }

        [JsonProperty("coded_height")]
        public int? CodedHeight { get; set; }

        [JsonProperty("coded_width")]
        public int? CodedWidth { get; set; }

        [JsonProperty("color_range")]
        public string? ColorRange { get; set; }

        [JsonProperty("display_aspect_ratio")]
        public string? DAR { get; set; }

        [JsonProperty("duration")]
        public double? Duration { get; set; }

        /// <summary>Duration expressed in integer time-base units (https://video.stackexchange.com/questions/27546/difference-between-duration-ts-and-duration-in-ffprobe-output</summary>
        [JsonProperty("duration_ts")]
        public long? DurationTS { get; set; }

        [JsonProperty("field_order")]
        public string? FieldOrder { get; set; }

        [JsonProperty("nb_frames")]
        public int? FramesCount { get; set; }

        [JsonProperty("r_frame_rate")]
        public string? FrameRateR { get; set; }

        [JsonProperty("avg_frame_rate")]
        public string? FrameRateAvg { get; set; }

        [JsonProperty("height")]
        public int? Height { get; set; }

        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("index")]
        public int Index { get; set; }

        [JsonProperty("is_avc")]
        public bool? IsAVC { get; set; }

        [JsonProperty("has_b_frames")]
        public bool? IsHasBFrames { get; set; } // todo@ type

        [JsonProperty("level")]
        public int? Level { get; set; }

        [JsonProperty("nb_packets")]
        public int? PacketsCount { get; set; }

        [JsonProperty("pix_fmt")]
        public string? PixFmt { get; set; }

        [JsonProperty("profile")]
        public string? Profile { get; set; }

        [JsonProperty("refs")]
        public int? Refs { get; set; }

        [JsonProperty("sample_fmt")]
        public string? SampleFormat { get; set; }

        [JsonProperty("sample_rate")]
        public int? SampleRate { get; set; }

        [JsonProperty("sample_aspect_ratio")]
        public string? SAR { get; set; }

        [JsonProperty("start_pts")]
        public long? StartPTS { get; set; }

        [JsonProperty("start_time")]
        public double? StartTime { get; set; }

        /// <summary>Stream-specific tags/metadata. See <see cref="KnownFFProbeVideoStreamTags"/>.</summary>
        [JsonProperty("tags")]
        public Dictionary<string, string?>? Tags { get; set; }

        /// <summary>Values like &quot;1/600&quot;. See https://stackoverflow.com/questions/43333542/what-is-video-timescale-timebase-or-timestamp-in-ffmpeg </summary>
        [JsonProperty("time_base")]
        public string? TimeBase { get; set; }

        [JsonProperty("width")]
        public int? Width { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object?>? ExtensionData { get; set; }
    }


    /// <summary>Known tag-names for <see cref="FFProbeStream"/>'s <see cref="FFProbeStream.Tags"/>.</summary>
    public static class KnownFFProbeVideoStreamTags
    {
        /// <summary>Tag value is ISO 8601 datetime</summary>
        public const string CreationTime = "creation_time";

        /// <summary>Values like H.264</summary>
        public const string Encoder = "encoder";

        /// <summary>Tag value is a decimal integer value in degrees, e.g. &quot;90&quot;.</summary>
        public const string Rotate = "rotate";
    }


    public class FFProbeJsonOutput
    {
        /// <summary>Information about the container</summary>
        [JsonProperty("format")]
        public FFProbeFormat? Format { get; set; }

        /// <summary>Information about frames</summary>
        //[JsonProperty("frames")]
        //public List<FFProbeFrame>? Frames { get; set; }

        /// <summary>Information about packets</summary>
        [JsonProperty("packets")]
        public List<FFProbePacket>? Packets { get; set; }

        /// <summary>Information about streams</summary>
        [JsonProperty("streams")]
        public List<FFProbeStream>? Streams { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object?>? ExtensionData { get; set; }
    }


    public static class FFProbeOutputExtensions
    {
        public static FFProbeStream? GetFirstVideoStream(this FFProbeJsonOutput ffProbeOutput)
        {
            return GetFirstStreamByType(ffProbeOutput, "video");
        }


        public static FFProbeStream? GetFirstAudioStream(this FFProbeJsonOutput ffProbeOutput)
        {
            return GetFirstStreamByType(ffProbeOutput, "audio");
        }


        public static FFProbeStream? GetFirstStreamByType(this FFProbeJsonOutput ffProbeOutput, string type)
        {
            if (ffProbeOutput.Streams != null)
            {
                foreach (var stream in ffProbeOutput.Streams)
                {
                    if (string.Equals(stream.CodecType, type, StringComparison.OrdinalIgnoreCase)) return stream;
                }
            }
            return null;
        }


        public static int? GetRotation(this FFProbeStream ffProbeStream)
        {
            if (ffProbeStream.Tags != null)
            {
                if (ffProbeStream.Tags.TryGetValue(KnownFFProbeVideoStreamTags.Rotate, out string? rotateTagValue))
                {
                    if (int.TryParse(rotateTagValue, NumberStyles.Any, CultureInfo.InvariantCulture, out int rotateTagValueInt)) return rotateTagValueInt;
                }
            }

            return null;
        }


        /// <summary>If the <see cref="FFProbeJsonOutput.Format"/> has a duration, that's returned - otherwise max first stream duration is returned. Returns null if none of the streams (nor the <see cref="FFProbeJsonOutput.Format"/>) has a duration value set.</summary>
        public static double? GetDuration(this FFProbeJsonOutput ffProbeOutput)
        {
            double? result = null;
            if (ffProbeOutput.Format != null && ffProbeOutput.Format.Duration.HasValue) result = ffProbeOutput.Format.Duration.Value;

            if (result == null && ffProbeOutput.Streams != null)
            {
                foreach (FFProbeStream stream in ffProbeOutput.Streams)
                {
                    double? duration = stream.Duration;
                    double start = stream.StartTime.HasValue ? (double)stream.StartTime : 0;
                    // todo@ should start is taken into consideration?
                    if (duration.HasValue && (result == null && (duration + start) > result)) result = duration + start;
                }
            }

            return result;
        }
    }

}
