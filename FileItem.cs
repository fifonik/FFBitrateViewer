using OxyPlot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace FFBitrateViewer
{
    public class FileItem : Bindable
    {
        public BitRate?                            BitRateAvg              { get { return Get<BitRate?>(); }       private set { Set(value); } }
        public BitRate?                            BitRateMax              { get { return Get<BitRate?>(); }       private set { Set(value); } }
        public BitRate?                            BitRateMin              { get { return Get<BitRate?>(); }       private set { Set(value); } }
        public string?                             FS { // File Spec
            get { return Get<string?>(); }
            set {
                Set(value);
                IsExists    = !string.IsNullOrEmpty(value) && File.Exists(value);
                FN          = Path.GetFileName(FS);
                var ext = Path.GetExtension(value);
                IsPreviewable = !string.IsNullOrEmpty(ext) && !string.IsNullOrEmpty(Helpers.GetAssociatedProgram(ext));
            }
        }
        public string?                             FN                      { get { return Get<string?>(); }        private set { Set(value); } } // File Name
        public  Frames?                            Frames                  { get { return Get<Frames?>(); }        private set { Set(value); OnPropertyChanged(nameof(Duration)); } }
        //public int?                                DropTarget              { get { return Get<int?>();  }          set { Set(value); } } // 1 -- top, 2 - bottom
        public double?                             Duration                { get { return MediaInfo?.Video0?.Duration ?? MediaInfo?.Duration ?? Frames?.Duration ?? Frames?.FramesDuration; } }
        public bool                                IsAdjustStartTime       { get { return Get<bool>();    }        set { Set(value); FramesIsAdjustStartTimeSet(value); } }
        public bool                                IsExists                { get { return Get<bool>();    }        private set { Set(value); OnPropertyChanged(nameof(IsReady));  } }
        public bool                                IsEnabled               { get { return Get<bool>();    }        set { Set(value);         OnPropertyChanged(nameof(IsReady));  } }
        public bool                                IsMediaInfoLoading      { get { return Get<bool>();    }        private set { Set(value); } }
        public bool                                IsFramesLoading         { get { return Get<bool>();    }        private set { Set(value); } }
        public bool                                IsFramesLoaded          { get { return Get<bool>();    }        private set { Set(value); } }
        public bool                                IsPreviewable           { get; private set; }
        public bool                                IsReady                 { get { return IsExists && IsEnabled && MediaInfo != null; } }
        public MediaInfo?                          MediaInfo               { get { return Get<MediaInfo>();  }     private set { Set(value); OnPropertyChanged(nameof(IsReady)); OnPropertyChanged(nameof(Duration)); } }

        public FileItem(string? fs = null, bool enabled = true)
        {
            Frames    = null;
            IsEnabled = enabled;
            MediaInfo = null;
            if (!string.IsNullOrEmpty(fs)) FS = fs;

        }


        private void BitRatesCalc(Frames frames)
        {
            var bitRates = frames.BitRatesCals();
            BitRateAvg = bitRates.Avg;
            BitRateMax = bitRates.Max;
            BitRateMin = bitRates.Min;
        }


        public void FramesClear()
        {
            BitRateAvg  = null;
            BitRateMax  = null;
            BitRateMin  = null;
            Frames      = null;
            IsFramesLoaded = false;
        }


        public List<DataPoint> FramesDataPointsGet(string? plotViewType)
        {
            return Frames == null ? new List<DataPoint>() : Frames.DataPointsGet(plotViewType);
        }


        public void FramesGet(CancellationToken cancellationToken, Action<int?, Frame, int>? action = null)
        {
            // Cannot just clear & reload Frames as it will not updated in MediaInfoBox
            if (!IsReady || string.IsNullOrEmpty(FS)) return;
            IsFramesLoading = true;
            IsFramesLoaded = false;

            var frames = new Frames();
            frames.IsAdjustStartTimeSet(IsAdjustStartTime);
            if (MediaInfo != null) frames.StartTime = MediaInfo.StartTime;

            int line = 0;
            FF.FramesGet(FS, cancellationToken, o => {
                ++line;
                if (o is Frame frame)
                {
                    var pos = frames.Add(frame);
                    if(pos != null) action?.Invoke(pos, frame, line);
                }
            });

            if (!cancellationToken.IsCancellationRequested)
            {
                frames.Analyze();
                BitRatesCalc(frames);
                Frames         = frames;
                IsFramesLoaded = true;
            }

            IsFramesLoading = false;
        }


        private void FramesIsAdjustStartTimeSet(bool isAdjustStartTime)
        {
            if (Frames != null)
            {
                if (isAdjustStartTime == Frames.IsAdjustStartTime) return;
                Frames.IsAdjustStartTimeSet(isAdjustStartTime);
                BitRatesCalc(Frames);
            }
        }


        public double? FramesMaxXGet(string? plotViewType)
        {
            return Frames?.MaxXGet(plotViewType);
        }


        public int FramesMaxYGet(string? plotViewType)
        {
            return Frames?.MaxYGet(plotViewType) ?? 0;
        }


        public void MediaInfoClear()
        {
            MediaInfo = null;
        }


        public void MediaInfoGet()
        {
            if (!string.IsNullOrEmpty(FS) && IsExists)
            {
                IsMediaInfoLoading = true;
                // Cannot just clear & reload MediaInfo as it will not updated in MediaInfoBox
                MediaInfo          = new MediaInfo(FS);
                IsMediaInfoLoading = false;
                if (Frames != null) Frames.StartTime = MediaInfo.StartTime;
            }
        }

    }
}
