using OxyPlot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;

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
        public string?                             FN                      { get { return Get<string?>();    }     private set { Set(value);  } } // File Name
        private Frames                             Frames                  { get;                                  set; } = new Frames();
        public int?                                FramesCount             { get { return Get<int?>(); }           private set { Set(value); } }
        //public int?                                DropTarget              { get { return Get<int?>(); }           set { Set(value); } } // 1 -- top, 2 - bottom
        public double?                             Duration                { get { return MediaInfo?.Video0?.Duration ?? MediaInfo?.Duration; } }
        public bool                                IsExists                { get { return Get<bool>(); }           private set { Set(value); OnPropertyChanged(nameof(IsReady));  } }
        public bool                                IsEnabled               { get { return Get<bool>(); }           set { Set(value);         OnPropertyChanged(nameof(IsReady));  } }
        public bool                                IsMediaInfoLoading      { get { return Get<bool>(); }           private set { Set(value); } }
        public bool                                IsFramesLoading         { get { return Get<bool>(); }           private set { Set(value); } }
        public bool                                IsFramesLoaded          { get { return Get<bool>(); }           private set { Set(value); } }
        public bool                                IsPreviewable           { get; private set; }
        public bool                                IsReady                 { get { return IsExists && IsEnabled && MediaInfo != null; } }
        public MediaInfo?                          MediaInfo               { get { return Get<MediaInfo>();  }     private set { Set(value); OnPropertyChanged(nameof(IsReady)); OnPropertyChanged(nameof(Duration)); } }

        public FileItem(string? fs = null, bool enabled = true)
        {
            IsEnabled = enabled;
            MediaInfo = null;
            if (!string.IsNullOrEmpty(fs)) FS = fs;

        }


        private void BitRatesCalc()
        {
            var bitRates = Frames.BitRatesCals();
            BitRateAvg = bitRates.Avg;
            BitRateMax = bitRates.Max;
            BitRateMin = bitRates.Min;
        }


        public void FramesClear()
        {
            BitRateAvg  = null;
            BitRateMax  = null;
            BitRateMin  = null;
            FramesCount = null;
            Frames.Clear();
            IsFramesLoaded = false;
        }


        public List<DataPoint> FramesDataPointsGet(string? plotViewType)
        {
            return Frames.DataPointsGet(plotViewType);
        }


        public void FramesGet(CancellationToken cancellationToken, Action<int?, Frame, int>? action = null)
        {
            if (!IsReady || string.IsNullOrEmpty(FS)) return;
            IsFramesLoading = true;
            IsFramesLoaded = false;

            int line = 0;
            FF.FramesGet(FS, cancellationToken, o => {
                ++line;
                if (o is Frame frame)
                {
                    var pos = Frames.Add(frame);
                    action?.Invoke(pos, frame, line);
                }
            });

            if (!cancellationToken.IsCancellationRequested)
            {
                BitRatesCalc();
                FramesCount = Frames.FramesCount;
                IsFramesLoaded = true;
            }

            IsFramesLoading = false;
        }


        public void FramesIsAdjustStartTimeSet(bool isAdjustStartTime)
        {
            if (isAdjustStartTime == Frames.IsAdjustStartTime) return;
            Frames.IsAdjustStartTimeSet(isAdjustStartTime);
            BitRatesCalc();
        }


        public double? FramesMaxXGet(string? plotViewType)
        {
            return Frames.MaxXGet(plotViewType);
        }


        public int FramesMaxYGet(string? plotViewType)
        {
            return Frames.MaxYGet(plotViewType);
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
                // Cannot just clear & reload media info as it will not updated in MediaInfoBox
                MediaInfo          = new MediaInfo(FS);
                Frames.StartTime   = MediaInfo.StartTime;
                IsMediaInfoLoading = false;
            }
        }

    }
}
