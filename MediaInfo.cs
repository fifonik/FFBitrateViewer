using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;


namespace FFBitrateViewer
{
    public class PInt
    {
        public int X { get; set; }
        public int Y { get; set; }

        public PInt(int x, int y)
        {
            X = x;
            Y = y;
        }


        public static PInt? Find(string item, Regex regex)
        {
            Match m = regex.Match(item);
            if (m.Success && int.TryParse(m.Groups[1].Value, out int x) && int.TryParse(m.Groups[2].Value, out int y)) return new PInt(x, y);
            return null;
        }


        public static PInt? Find(List<string> items, Regex regex)
        {
            foreach (string item in items)
            {
                PInt? v = Find(item, regex);
                if (v != null) return v;
            }
            return null;
        }


        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode();
        }


        public override bool Equals(object? obj)
        {
            return obj is PInt other && X == other.X && Y == other.Y;
        }


        public string ToString(char separator = ' ')
        {
            return X.ToString() + separator + Y.ToString();
        }
    }

    public class UInt
    {
        public string? unitDefault = null;
        public int     Value { get; set; }
        public string? Unit  { get; set; }


        public UInt(int value, string? unit = null) { 
            Value = value; 
            Unit  = unit;
        }


        public static UInt? Find(string item, Regex regex)
        {
            Match m = regex.Match(item);
            if (m.Success && int.TryParse(m.Groups[1].Value, out int value)) return new UInt(value, m.Groups[2].Value);
            return null;
        }


        public static UInt? Find(List<string> items, Regex regex)
        {
            foreach (string item in items)
            {
                UInt? v = Find(item, regex);
                if (v != null) return v;
            }
            return null;
        }


        public override int GetHashCode()
        {
            return Value.GetHashCode() ^ (Unit ?? "").GetHashCode();
        }


        public override bool Equals(object? obj)
        {
            return obj is UInt other && Value == other.Value && Unit == other.Unit;
        }


        public virtual string ToString(char separator = ' ')
        {
            var value = Value;
            var unit  = Unit ?? unitDefault;

            if (unit != null && unit == unitDefault && unit.Length > 1)
            {
                // Converting * to k*
                value = (int)Math.Round((double)value / 1000);
                unit = "k" + unit;
            }

            return value.ToString() + (string.IsNullOrEmpty(unit) ? "" : separator + unit);
        }

    }


    public class UDouble
    {
        public double Value { get; set; }
        public string? Unit  { get; set; }


        public UDouble(double value, string? unit = null)
        {
            Value = value;
            Unit  = unit;
        }


        public static UDouble? Find(string item, Regex regex, string? unit = null)
        {
            Match m = regex.Match(item);
            if (m.Success && Helpers.TryParseDouble(m.Groups[1].Value, out double value)) return new UDouble(value, unit ?? m.Groups[2].Value);
            return null;
        }


        public static UDouble? Find(List<string> items, Regex regex, string? unit = null)
        {
            foreach (string item in items)
            {
                UDouble? v = Find(item, regex, unit);
                if (v != null) return v;
            }
            return null;
        }


        public override int GetHashCode()
        {
            return Value.GetHashCode() ^ (Unit ?? "").GetHashCode();
        }


        public override bool Equals(object? obj)
        {
            return obj is UDouble other && Value == other.Value && Unit == other.Unit;
        }


        public override string ToString()
        {
            return Value.ToString(CultureInfo.InvariantCulture) + " " + Unit;
        }

        public double ToDouble()
        {
            return Value;
        }
    }


    public class BitRate : UInt
    {
        public BitRate(int value, string? unit = null) : base(value, unit) {
            unitDefault = "b/s";
        }
    }


    public class SampleRate : UInt
    {
        public SampleRate(int value, string? unit = null) : base(value, unit) {
            unitDefault = "Hz";
        }
    }


    public enum VideoStreamFormatToStringMode
    {
        // FULL -- null
        CHROMA_SUBSAMPLING,
        COLOR_SPACE,
        COLOR_SPACE_FULL,
        COLOR_RANGE,
        FIELD_TYPE,
        FIELD_TYPE_NAME,
        PIXEL_FORMAT
    }

    public class VideoStreamFormat
    {
        private static readonly Regex PixelFormatRegex = new(@"^(ABGR|ARGB|BGR|GBR|GRAY|RGB|UYVY|YA|YUV|YUVA|YUVJ|YUYV)(\d{1,3})?(.*)$", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        public bool?                  Progressive       { get; set; } // true -- progressive, false -- interlaced, null -- undetected
        public string?                PixelFormat       { get; set; } // http://git.videolan.org/?p=ffmpeg.git;a=blob;f=libavutil/pixfmt.h;hb=HEAD
        public string?                ColorSpace        { get; set; }
        public string?                ChromaSubsampling { get; set; }
        public string?                ColorRange        { get; set; }

        public VideoStreamFormat(FFProbeStream info) {
            ColorRangeSet(info.ColorRange);
            PixelFormatSet(info.PixFmt);
            ProgressiveSet(info.FieldOrder);
        }


        private void PixelFormatSet(string? pixelFormat)
        {
            if(pixelFormat == null) return;
            PixelFormat = pixelFormat;
            var m = PixelFormatRegex.Match(pixelFormat);
            if (m.Success)
            {
                ColorSpaceSet(m.Groups[1].Value);
                ChromaSubsamplingSet(m.Groups[2].Value);
            }
        }


        private void ProgressiveSet(string? value)
        {
            switch (value?.ToUpper()[0])
            {
                case 'P':
                    Progressive = true;
                    break;
                case 'I':
                case 'B':
                case 'T':
                    Progressive = false;
                    break;
            }
        }

        private void ColorRangeSet(string? value)
        {
            value = value?.ToUpper();
            switch (value)
            {
                case "TV":
                case "PC":
                    ColorRange = value;
                    break;
            }
        }


        private void ColorSpaceSet(string? value)
        {
            switch (value?.ToUpper())
            {
                case "YUV":
                case "YUVJ":
                case "YUVY":
                case "YUYV":
                    ColorSpace = "YUV";
                    break;
                case "BGR":
                case "GBR":
                case "RGB":
                    ColorSpace = "RGB";
                    break;
                case "YUVA":
                    ColorSpace = "YUVA";
                    break;
                case "ABGR":
                case "ARGB":
                case "BGRA":
                case "RGBA":
                    ColorSpace = "RGBA";
                    break;
                case "GRAY":
                case "YA":
                    // todo@
                    break;
            }
        }


        private void ChromaSubsamplingSet(string? value)
        {
            switch (value)
            {
                case "420":
                case "422":
                case "440":
                case "444":
                    ChromaSubsampling = value;
                    break;
                default:
                    // todo@
                    break;
            }
        }


        public string? ToString(VideoStreamFormatToStringMode? mode = null)
        {
            switch (mode)
            {
                case null: // FULL
                    var result = new List<string?>
                    {
                        PixelFormat
                    };
                    if (!string.IsNullOrEmpty(ColorRange)) result.Add("(" + ColorRange + ")");
                    return string.Join(" ", result);

                case VideoStreamFormatToStringMode.CHROMA_SUBSAMPLING:
                    return ChromaSubsampling;

                case VideoStreamFormatToStringMode.COLOR_RANGE:
                    return ColorRange?.ToUpper();

                case VideoStreamFormatToStringMode.COLOR_SPACE:
                    return ColorSpace;

                case VideoStreamFormatToStringMode.COLOR_SPACE_FULL:
                    return ColorSpace + ChromaSubsampling;

                case VideoStreamFormatToStringMode.FIELD_TYPE:
                    return Progressive == true ? "p" : (Progressive == false ? "i" : "?");

                case VideoStreamFormatToStringMode.FIELD_TYPE_NAME:
                    return Progressive == true ? "Progressive" : (Progressive == false ? "Interlaced" : null);

                case VideoStreamFormatToStringMode.PIXEL_FORMAT:
                    return PixelFormat;

                default:
                    return null; // todo@ exception
            }
        }
    }

    public enum VideoStreamToStringMode
    {
        // FULL -- null
        SHORT
    }


    public class NDPair
    {
        private static readonly Regex NDPairRegex  = new(@"^(\d+)/(\d+)$", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex RemoveZeroes = new(@"\.?0+$");
        public int?                   Denominator { get; set; }
        public int?                   Numerator   { get; set; }
        public string?                Value       { get; set; }

        public NDPair(string? value)
        {
            Value = value;
            if (Value == null) return;

            var m = NDPairRegex.Match(Value);
            if (m.Success)
            {
                if (int.TryParse(m.Groups[1].Value, out int n) && int.TryParse(m.Groups[2].Value, out int d) && n > 0 && d > 0)
                {
                    Numerator   = n;
                    Denominator = d;
                }
            }
        }


        public override string? ToString() {
            var d = ToDouble();
            if (d == null) return null;
            return RemoveZeroes.Replace(((double)d).ToString("0.000", CultureInfo.InvariantCulture), "");
        }
        public double? ToDouble() { return Numerator > 0 && Denominator > 0 ? (double)Numerator / Denominator : null; }
    }


    public class BaseStream
    {

        //public string? Info;
        public string?             CodecName            { get; set; }
        public string?             CodecTag             { get; set; }
        public string?             CodecTagString       { get; set; }
        public double?             Duration             { get; set; }
        public long?               DurationTS           { get; set; }
        public NDPair?             FrameRateAvg         { get; set; }
        public NDPair?             FrameRateR           { get; set; } // todo@ what is this?
        public string?             Id                   { get; set; }
        public int?                Index                { get; set; }
        public long?               StartPTS             { get; set; }
        public double?             StartTime            { get; set; }
        public NDPair?             TimeBase             { get; set; }
        public BaseStream(FFProbeStream info) {
            //Info = info;
            CodecName      = info.CodecName;
            CodecTag       = info.CodecTag;
            CodecTagString = info.CodecTagString;
            Duration       = info.Duration;
            DurationTS     = info.DurationTS;
            FrameRateAvg   = new NDPair(info.FrameRateAvg);
            FrameRateR     = new NDPair(info.FrameRateR);
            Id             = info.Id;
            Index          = info.Index;
            StartPTS       = info.StartPTS;
            StartTime      = info.StartTime;
            TimeBase       = new NDPair(info.TimeBase);
        }

    }


    public class VideoStream : BaseStream
    {
        public BitRate?            BitRate              { get; set; }
        public bool                IsBitrateCalculated  { get; set; }
        public string?             Encoder              { get; set; }
        public VideoStreamFormat   Format               { get; set; }
        //public UDouble?            FPS                  { get; set; }
        public string?             Profile              { get; set; }
        public PInt?               Resolution           { get; set; }
        //public UDouble?            TBR                  { get; set; }

        public VideoStream(FFProbeStream info) : base(info)
        {
            Format     = new VideoStreamFormat(info);
            Profile    = info.Profile;
            if (info.BitRate != null)                      BitRate    = new BitRate((int)info.BitRate);
            if (info.Width != null && info.Height != null) Resolution = new PInt((int)info.Width, (int)info.Height);
            //todo@ TBR
        }


        public string ToString(VideoStreamToStringMode? mode = null)
        {
            var result = new List<string>();
            var sb = new StringBuilder(15);
            switch (mode)
            {
                case null: // FULL
                    if (Resolution != null) sb.Append(Resolution.ToString('x'));

                    if (FrameRateAvg?.Value != null) sb.Append("-" + FrameRateAvg.ToString());
                    sb.Append(Format?.ToString(VideoStreamFormatToStringMode.FIELD_TYPE));
                    result.Add(sb.ToString());

                    if (Format != null)
                    {
                        var s = Format.ToString();
                        if (!string.IsNullOrEmpty(s)) result.Add(s);
                    }

                    if (BitRate != null) result.Add(BitRate.ToString());
                    break;

                case VideoStreamToStringMode.SHORT:
                    if (Resolution != null) sb.Append(Resolution.Y);
                    if (FrameRateAvg?.Value != null) sb.Append("-" + FrameRateAvg.ToString());
                    sb.Append(Format?.ToString(VideoStreamFormatToStringMode.FIELD_TYPE));
                    if (sb.Length > 0) result.Add(sb.ToString());

                    if (Format != null)
                    {
                        string? s;
                        s = Format.ToString(VideoStreamFormatToStringMode.COLOR_SPACE_FULL);
                        if (!string.IsNullOrEmpty(s)) result.Add(s);
                        s = Format.ToString(VideoStreamFormatToStringMode.COLOR_RANGE);
                        if (!string.IsNullOrEmpty(s)) result.Add(s);
                    }
                    break;

                default:
                    break; // todo@ exception?
            }
            return string.Join(", ", result);
        }
    }


    public class AudioStream : BaseStream
    {
        public BitRate? BitRate        { get; set; }
        public string?  Channels       { get; set; }
        public string?  Encoder        { get; set; }
        public UInt?    Frequency      { get; set; }

        public AudioStream(FFProbeStream info) : base(info)
        {
            if (info.BitRate != null)       BitRate   = new BitRate((int)info.BitRate);
            if (info.ChannelLayout != null) Channels  = info.ChannelLayout;
            if (info.SampleRate != null)    Frequency = new SampleRate((int)info.SampleRate);
        }


        public override string ToString()
        {
            var result = new List<string>();
            if (Encoder != null)   result.Add(Encoder);
            if (Channels != null)  result.Add(Channels);
            if (BitRate != null)   result.Add(BitRate.ToString());
            if (Frequency != null) result.Add(Frequency.ToString());
            return string.Join(", ", result);
        }
    }

    public class SubtitleStream : BaseStream
    {
        public SubtitleStream(FFProbeStream info) : base(info) { }


        public override string ToString()
        {
            return ""; // todo@
        }
    }


    public class MediaInfo
    {
        public int                  Status  = 0;
        public string               Message = "";
        public bool                 IsFilled        { get; set; } = false;
        public double?              Duration        { get; set; }
        public double               StartTime       { get; set; } = 0;
        public BitRate?             BitRate         { get; set; }
        public List<VideoStream>    VideoStreams    { get; set; }
        public List<AudioStream>    AudioStreams    { get; set; }
        public List<SubtitleStream> SubtitleStreams { get; set; }
        public VideoStream?         Video0          { get { return VideoStreams.Count == 0 ? null : VideoStreams[0]; } }

        public MediaInfo()
        {
            VideoStreams    = new List<VideoStream>();
            AudioStreams    = new List<AudioStream>();
            SubtitleStreams = new List<SubtitleStream>();
        }

        public MediaInfo(string fs) : this()
        {
            FF.MediaInfoGet(this, fs);
        }

    }


}
