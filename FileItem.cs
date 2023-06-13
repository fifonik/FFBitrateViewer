using System;
using System.IO;
using System.Threading;

namespace FFBitrateViewer
{
    public class FileItem : Bindable
    {
        public BitRate?                            BitRateAvg              { get { return Get<BitRate?>(); }           private set { Set(value); } }
        public BitRate?                            BitRateMax              { get { return Get<BitRate?>(); }           private set { Set(value); } }
        public string?                             FS { // File Spec
            get { return Get<string?>(); }
            set {
                Set(value);
                IsExists    = !string.IsNullOrEmpty(value) && File.Exists(value);
                FN          = Path.GetFileName(FS);
                // todo@ why? OnPropertyChanged(nameof(FS));
            }
        }
        public string?                             FN                      { get { return Get<string?>();    }     private set { Set(value);  } } // File Name
        public Frames                              Frames                  { get;                                  private set; } = new Frames();
        public int?                                FramesCount             { get { return Get<int?>(); }           private set { Set(value); } }
        public int?                                DropTarget              { get { return Get<int?>(); }           set { Set(value); } } // 1 -- top, 2 - bottom
        public bool                                IsExists                { get { return Get<bool>();       }     private set { Set(value); OnPropertyChanged(nameof(IsExistsAndEnabled)); OnPropertyChanged(nameof(IsReady));  } }
        public bool                                IsEnabled               { get { return Get<bool>();       }     set { Set(value);         OnPropertyChanged(nameof(IsExistsAndEnabled)); OnPropertyChanged(nameof(IsReady));  } }
        public bool                                IsExistsAndEnabled      { get { return IsExists && IsEnabled; } }
        public bool                                IsMediaInfoLoading      { get { return Get<bool>(); }           private set { Set(value); } }
        public bool                                IsDataLoading           { get { return Get<bool>(); }           private set { Set(value); } }
        public bool                                IsReady                 { get { return IsExists && IsEnabled; } }
        public MediaInfo?                          MediaInfo               { get { return Get<MediaInfo>();  }     private set { Set(value); OnPropertyChanged(nameof(IsReady)); } }

        public FileItem(string? fs = null, bool enabled = true)
        {
            IsEnabled = enabled;
            MediaInfo = null;
            if (!string.IsNullOrEmpty(fs)) FS = fs;

        }

        public void FramesClear()
        {
            BitRateAvg  = null;
            BitRateMax  = null;
            FramesCount = null;
            Frames.Clear();
        }


        public void FramesGet(CancellationToken cancellationToken, Action<int?, Frame>? action = null)
        {
            if (string.IsNullOrEmpty(FS)) return;
            IsDataLoading = true;

            FF.FramesGet(FS, cancellationToken, o => {
                if (o is Frame frame)
                {
                    var pos = Frames.Add(frame);
                    action?.Invoke(pos, frame);
                }
            });

            var bitrateAvg = Frames.GetBitRateAvg();
            if (bitrateAvg != null) BitRateAvg  = new BitRate((int)bitrateAvg);

            var bitrateMax = Frames.GetBitRateMax();
            if (bitrateMax != null) BitRateMax = new BitRate((int)bitrateMax);

            FramesCount = Frames.Items.Count;

            IsDataLoading = false;
        }


        public int? GetBitRateFromStream()
        {
            return MediaInfo?.Video0?.BitRate?.Value;
        }


        public int? GetBitRateFromFile()
        {
            return MediaInfo?.BitRate?.Value;
        }


        public double? GetDuration()
        {
            return Frames.GetDuration() ?? GetDurationFromStream() ?? GetDurationFile();
        }


        public double? GetDurationFromStream()
        {
            return (MediaInfo?.Video0?.Duration > 0) ? (MediaInfo.Video0.Duration - (MediaInfo?.Video0?.StartTime ?? 0)) : null;
        }


        public double? GetDurationFile()
        {
            return (MediaInfo?.Duration > 0) ? (MediaInfo.Duration - (MediaInfo?.StartTime ?? 0)) : null;
        }

/*
        public double GetPacketsOffset()
        {
            return (MediaInfo?.StartTime ?? 0) + (MediaInfo?.Video0?.StartTime ?? 0);
        }
*/

        public void MediaInfoClear()
        {
            MediaInfo = null;
        }


        public void MediaInfoGet()
        {
            if (!string.IsNullOrEmpty(FS) && IsExists)
            {
                IsMediaInfoLoading = true;
                // Cannot just clear & reload media info as it will not updated in MediaInfoBox
                var info           = new MediaInfo(FS);
                MediaInfo          = info;
                Frames.StartTime   = info.StartTime;
                IsMediaInfoLoading = false;
            }
        }

    }
}
