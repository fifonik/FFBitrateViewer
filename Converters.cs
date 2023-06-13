using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;


namespace FFBitrateViewer
{
    public static class ConverterHelper
    {
        public static string Unknown = "-unknown-";
        public static string NA      = "N/A";
        public static string Error   = "Error";
        public static SolidColorBrush ColorGood                 = new SolidColorBrush(Colors.LawnGreen);
        public static SolidColorBrush ColorBad                  = new SolidColorBrush(Colors.Pink);
        public static SolidColorBrush ColorDurationAvailable    = SystemColors.WindowTextBrush;
        public static SolidColorBrush ColorDurationUnavailable  = new SolidColorBrush(Colors.Red);
        public static SolidColorBrush ColorTransparent          = new SolidColorBrush(Colors.Transparent);
        public static SolidColorBrush ColorDropTarget           = new SolidColorBrush(Colors.Black);
    }


    public class BitRateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is BitRate bitrate) ? bitrate.ToString() : ConverterHelper.Unknown;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
    }


    public class DoubleFormatterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                string format = string.IsNullOrEmpty((string)parameter) ? "0.000000" : (string)parameter;
                return d.ToString(format, CultureInfo.InvariantCulture);
            }
            return "";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
    }


    public class FSConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int maxLength = 92;
            string s = (string)value;
            return !string.IsNullOrEmpty(s) && s.Length > maxLength ? Helpers.PathShortener(s, maxLength) : s;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
    }


    public class FileDropTargetBackgroundConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 4) return ConverterHelper.ColorTransparent;

            var dropPos = values[0] as string;
            var thisPos = values[2] as string;

            return (values[1] is FileItem dropFile && values[3] is FileItem thisFile && dropPos == thisPos && dropFile == thisFile) ? ConverterHelper.ColorDropTarget : ConverterHelper.ColorTransparent;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
    }


    public class MediaInfoBitRateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MediaInfo info)
            {
                if (info.Video0?.BitRate != null) return info.Video0.BitRate.ToString();
                if (info.BitRate != null && info.BitRate.Value != 0) return info.BitRate.ToString() + "*";
            }
            return ConverterHelper.Unknown;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
    }


    public class MediaInfoColorRangeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? s = value is MediaInfo info ? info.Video0?.Format?.ToString(VideoStreamFormatToStringMode.COLOR_RANGE) : null;
            return string.IsNullOrEmpty(s) ? ConverterHelper.Unknown : s;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
    }


    public class MediaInfoCompactConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? s = value is MediaInfo info ? info.Video0?.ToString(VideoStreamToStringMode.SHORT) : null;
            return string.IsNullOrEmpty(s) ? ConverterHelper.NA : s;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
    }


    public class MediaInfoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? s = value is MediaInfo info ? info.Video0?.ToString() : null;
            return string.IsNullOrEmpty(s) ? ConverterHelper.Unknown : s;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
    }


    public class MediaInfoFileBitRateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is MediaInfo info && info.BitRate != null) ? info.BitRate.ToString() : ConverterHelper.Unknown;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
    }


    public class MediaInfoFileDurationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MediaInfo info && info.Duration != null)
            {
                string result = TimeSpan.FromSeconds((double)info.Duration).ToString(@"hh\:mm\:ss\.ff");
                if (result != "00:00:00.00") return result;
            }
            return ConverterHelper.Unknown;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
    }


    public class MediaInfoFileStartConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MediaInfo info)
            {
                string result = TimeSpan.FromSeconds((double)info.StartTime).ToString(@"hh\:mm\:ss\.ff");
                if (result != "00:00:00.00") return result;
            }
            return ConverterHelper.Unknown;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
    }


    public class MediaInfoFrameRateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? s = value is MediaInfo info && info.Video0?.FrameRateAvg != null ? (info.Video0?.FrameRateAvg?.ToString() + " fps (" + info.Video0?.FrameRateAvg?.Value + ")"): null;
            return string.IsNullOrEmpty(s) ? ConverterHelper.Unknown : s;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
    }


    public class MediaInfoFramesCountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is int i) ? i : ConverterHelper.Unknown;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
    }


    public class MediaInfoColorSpaceFullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? s = value is MediaInfo info ? info.Video0?.Format?.ToString(VideoStreamFormatToStringMode.COLOR_SPACE_FULL) : null;
            return string.IsNullOrEmpty(s) ? ConverterHelper.Unknown : s;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
    }


    public class MediaInfoColorSpaceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? s = value is MediaInfo info ? info.Video0?.Format?.ToString(VideoStreamFormatToStringMode.COLOR_SPACE) : null;
            return string.IsNullOrEmpty(s) ? ConverterHelper.Unknown : s;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
    }


    public class MediaInfoChromaSubsamplingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? s = value is MediaInfo info ? info.Video0?.Format?.ToString(VideoStreamFormatToStringMode.CHROMA_SUBSAMPLING) : null;
            return string.IsNullOrEmpty(s) ? ConverterHelper.Unknown : s;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
    }


    public class MediaInfoDurationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MediaInfo info)
            {
                double? d = null;
                if (info.Video0?.Duration != null)      d = info.Video0.Duration;
                if (d == null && info.Duration != null) d = info.Duration;
                if (d != null)
                {
                    string result = TimeSpan.FromSeconds((double)d).ToString(@"hh\:mm\:ss\.ff");
                    if (result != "00:00:00.00") return result + (info.Video0?.Duration == null ? "*" : "");
                }
            }
            return ConverterHelper.Unknown;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
    }


    public class MediaInfoFieldTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? s = value is MediaInfo info ? info.Video0?.Format?.ToString(VideoStreamFormatToStringMode.FIELD_TYPE_NAME) : null;
            return string.IsNullOrEmpty(s) ? ConverterHelper.Unknown : s;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
    }


    public class MediaInfoResolutionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? s = value is MediaInfo info ? info.Video0?.Resolution?.ToString('x') : null;
            return string.IsNullOrEmpty(s) ? ConverterHelper.Unknown : s;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
    }

    public class MediaInfoStreamBitRateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is MediaInfo info && info.Video0?.BitRate != null) ? info.Video0.BitRate.ToString() : ConverterHelper.Unknown;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
    }


    public class MediaInfoStreamDurationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MediaInfo info && info.Video0?.Duration != null)
            {
                string result = TimeSpan.FromSeconds((double)info.Video0.Duration).ToString(@"hh\:mm\:ss\.ff");
                if (result != "00:00:00.00") return result;
            }
            return ConverterHelper.Unknown;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
    }


    public class MediaInfoStreamStartConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MediaInfo info)
            {
                string result = TimeSpan.FromSeconds((double)info.StartTime).ToString(@"hh\:mm\:ss\.ff");
                if (result != "00:00:00.00") return result;
            }
            return ConverterHelper.Unknown;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
    }


    public class MediaInfoStreamsInfoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MediaInfo info)
            {
                var items = new List<string>
                {
                    "Video: " + info.VideoStreams.Count,
                    "Audio: " + info.AudioStreams.Count,
                    "Subtitle: " + info.SubtitleStreams.Count
                };
                return string.Join(", ", items);
            }
            return ConverterHelper.Unknown;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
    }


    public class NotNullToVisibileConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null || (value is string @string && string.IsNullOrEmpty(@string)) ? Visibility.Hidden : Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
    }


    public class NotNullToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(value == null || (value is string @string && string.IsNullOrEmpty(@string)));
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
    }


    public class StringToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var paramVal = parameter as string;
            if (paramVal == null) paramVal = "";
            var objVal = value as string;
            if (objVal == null) objVal = "";
            return string.Equals(paramVal, objVal, StringComparison.OrdinalIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool bVal && bVal) ? (string)parameter : "";
        }
    }


}
