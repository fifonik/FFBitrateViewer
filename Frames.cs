using OxyPlot;
using System;
using System.Collections.Generic;

namespace FFBitrateViewer
{
    public enum FramePictType
    {
        I,
        P,
        B
    };

    public class FramesBitRates
    {
        public BitRate? Avg { get; set; }
        public BitRate? Avg2 { get; set; }
        public BitRate? Max { get; set; }
        public BitRate? Min { get; set; }
    }


    public class Frame
    {
        public double         Duration     { get; set; }
        public double         EndTime      { get { return StartTime + Duration; } }
        public FramePictType? FrameType    { get; set; } // I, P, B
        public bool           IsOrdered    { get; set; }
        public long?          Pos          { get; set; }
        public int            Size         { get; set; } // Frame size in bytes
        public double         StartTime    { get { return StartTimeRaw ?? 0; } }
        public double?        StartTimeRaw { get; set; }


        public static Frame? CreateFrame(FFProbePacket packet)
        {
            if (packet.DurationTime == null || packet.Size == null) return null;
            return new Frame()
            {
                Duration     = (double)packet.DurationTime,
                FrameType    = packet.Flags?.IndexOf('K') >= 0 ? FramePictType.I : null,
                IsOrdered    = false, // 'Packets' returned by FFProbe are ordered by DTS, not PTS so will need to order them later when adding onto list
                Pos          = packet.Pos,
                Size         = (int)packet.Size,
                StartTimeRaw = packet.PTSTime
            };
        }


        public static Frame? CreateFrame(FFProbeFrame frame)
        {
            if (frame.DurationTime == null || frame.Size == null) return null;

            FramePictType? pictType = frame.PictType?[0] switch
            {
                'I' => FramePictType.I,
                'P' => FramePictType.P,
                'B' => FramePictType.B,
                _   => null
            };

            return new Frame()
            {
                Duration     = (double)frame.DurationTime,
                FrameType    = pictType,
                IsOrdered    = true, // 'Frames' returned by FFProbe are already ordered by BestEffortTimestampTime
                Pos          = frame.Pos,
                Size         = (int)frame.Size,
                StartTimeRaw = frame.BestEffortTimestampTime ?? frame.PTSTime
            };
        }


        // Calculate what size of the frame accounts for specified interval
        public int GetSize(double? intervalStartTime = null, double? intervalEndTime = null)
        {
            // No interval specified
            if (intervalStartTime == null || intervalEndTime == null) return Size;

            // The frame is outside of the interval so none of its size is taken into account
            if (EndTime <= (double)intervalStartTime || StartTime >= (double)intervalEndTime) return 0;

            // The frame is fully inside of the interval so all of its size is taken into account
            if (StartTime >= (double)intervalStartTime && EndTime <= (double)intervalEndTime) return Size;

            // Only a part of the frame is inside the interval, so calculating what part of its size accounts for the size of the interval
            var start = double.Max(StartTime, (double)intervalStartTime);
            var end   = double.Min(EndTime,   (double)intervalEndTime);

            return (int)double.Round(Size * ((end - start) / Duration));
        }


        public List<DataPoint> DataPointGet(string? plotViewType, double startTimeOffset, bool isLast = false)
        {
            List<DataPoint> data = [];
            var y = ScaleY(plotViewType, Size);
            data.Add(new DataPoint(StartTime - startTimeOffset, y));
            if(isLast) data.Add(new DataPoint(EndTime - startTimeOffset, y));
            return data;
        }


        public static double ScaleY(string? plotViewType, int value, int sizeDivider = 1000/* kilo */)
        {
            switch (plotViewType?.ToUpper())
            {
                case "GOP":
                case "SECOND":
                    return (double)value * 8 /* Byte/s => bit/s */ / sizeDivider;
            }
            return (double)value / sizeDivider;
        }
    }

    public class GOP
    {
        private List<Frame> Frames              { get; set; } = [];
#if DEBUG
        private List<int>   FramesSizes         { get; set; } = []; // for debugging only: what size was added by the associated frame
#endif
        public bool         IsEmpty             { get { return Frames.Count == 0; } }
        public  bool        IsRealGOP           { get { return FixedTimeDuration == null; } }
        public  int         Size                { get; private set; } = 0;
        public  int         BitRate             { get { return Duration == 0 ? 0 : (int)double.Round(Size / (double)Duration); } }
        public double       EdgeStartTime       { get { return IsRealGOP ? FramesStartTime - StartTimeOffset : FixedTimeStartTime; } }
        public double       EdgeEndTime         { get { return IsRealGOP ? FramesEndTime   - StartTimeOffset : FixedTimeEndTime;   } }
        public  double      StartTimeRaw        { get; private set; } = 0;
        public  double      StartTimeOffset     { get; private set; } = 0;
        public  double      StartTime           { get { return IsRealGOP ? FramesStartTime - StartTimeOffset : Math.Max(FixedTimeStartTime, FramesStartTime - StartTimeOffset); } }
        public  double      EndTime             { get { return IsRealGOP ? FramesEndTime   - StartTimeOffset : Math.Min(FixedTimeEndTime,   FramesEndTime   - StartTimeOffset); } }
        public  double      Duration            { get { return EndTime - StartTime; } }
        public  double?     FixedTimeDuration   { get; private set; } // NULL -- for real GOPs, GOP duration (in seconds) for fixed length GOPs as 'per second'
        public  double      FixedTimeStartTime  { get { return StartTimeRaw; } }
        public  double      FixedTimeEndTime    { get { return StartTimeRaw + (FixedTimeDuration ?? 0); } }
        public  double      FramesDuration      { get { return FramesEndTime - FramesStartTime; } }
        public  double      FramesStartTime     { get { return IsEmpty ? StartTimeRaw : Frames[0].StartTime; } }
        public  double      FramesEndTime       { get { return IsEmpty ? StartTimeRaw : Frames[^1].EndTime;  } }

        public GOP(double startTimeOffset, Frame? frame, double? startTime = null, double? duration = null)
        {
            if (duration < 0) throw new ArgumentException("Must be greater then 0", nameof(duration)); // todo@ can it be 0?
            FixedTimeDuration = duration;
            StartTimeOffset   = startTimeOffset;
            StartTimeRaw      = startTime ?? frame?.StartTime ?? 0;
            if (frame != null) Add(frame);
        }


        public void Clear()
        {
            Frames.Clear();
            Size = 0;
        }


        public void Add(Frame frame)
        {
            //if (IsEmpty) StartTimeRaw = frame?.StartTime ?? 0;

            if (FixedTimeDuration == null) // Not SECOND based
            {
                if (frame.FrameType == FramePictType.I)
                {
                    if (!IsEmpty) throw new ArgumentException("I-frame can only be the first in GOP");
                }
                else
                {
                    // No exception as frames could be added out of order
                    // So it is possible that P-frame will be added 1st and then I-frame with smaller start time will be added
                }
            }
            Frames.Add(frame);
            var size = SizeGet(frame);
#if DEBUG
            FramesSizes.Add(size);
#endif
            Size += size;
        }


        private int SizeGet(Frame frame)
        {
            // FixedTimeDuration is null for real GOPs (not by time GOP) and the 2nd param will become null. This will make the whole frame size to be accounted to the GOP size
            //return frame.GetSize(StartTime + StartTimeOffset, StartTime + StartTimeOffset + FixedTimeDuration);
            return frame.GetSize(EdgeStartTime + StartTimeOffset, EdgeStartTime + StartTimeOffset + FixedTimeDuration);
        }


        public List<DataPoint> DataPointsGet(string? plotViewType, bool isLast = false)
        {
            List<DataPoint> data = [];
            if (IsEmpty) return data;

            var bitRate = Frame.ScaleY(plotViewType, BitRate);

            data.Add(new DataPoint(StartTime, bitRate));
            if(isLast) data.Add(new DataPoint(EndTime, bitRate));

            return data;
        }
    }


    public class GOPsBy
    {
        protected List<Frame>   Frames          { get; private set; }   = [];
        protected List<GOP>     GOPs            { get; set; }           = [];
        public    int?          MaxSize         { get; private set; }
        public    int?          MinSize         { get; private set; }
        public    ulong?        TotalSize       { get; private set; }
        public    double        StartTimeOffset { get; protected set; } = 0;


        public GOPsBy() { }


        protected void CalcMinMax()
        {
            int? max     = null;
            int? min     = null;
            ulong? total = null;
            foreach (var gop in GOPs)
            {
                if (total == null)
                {
                    total = (ulong)gop.Size;
                }
                else
                {
                    total += (ulong)gop.Size;
                }
                if (max == null || gop.BitRate > max) max = gop.BitRate;
                if (min == null || gop.BitRate < min) min = gop.BitRate;
            }
            MaxSize   = max;
            MinSize   = min;
            TotalSize = total;
        }


        public void Clear()
        {
            GOPs.Clear();
            MaxSize         = null;
            MinSize         = null;
            TotalSize       = null;
            StartTimeOffset = 0;
        }


        public List<DataPoint> DataPointsGet(string? plotViewType)
        {
            List<DataPoint> data = [];
            for (var idx = 0; idx < GOPs.Count; ++idx) data.AddRange(GOPs[idx].DataPointsGet(plotViewType, idx == GOPs.Count - 1));
            return data;
        }


        public void SetFrames(List<Frame> frames)
        {
            Frames = frames;
        }
    }


    public class GOPsByGOP : GOPsBy
    {
        public GOPsByGOP() : base() { }


        public bool Calc(double startTimeOffset)
        {
            if (Frames.Count == 0 || (GOPs.Count > 0 && StartTimeOffset == startTimeOffset)) return false; // no data or calculated already

            StartTimeOffset = startTimeOffset;

            GOP? gop = null;

            foreach (var frame in Frames)
            {
                if (gop == null)
                {
                    gop = new(StartTimeOffset, frame);
                    continue;
                }

                // On every I-frame finalyzing current GOP and creating a new one
                if (frame.FrameType == FramePictType.I)
                {
                    GOPs.Add(gop);
                    gop = new(StartTimeOffset, frame);
                    continue;
                }

                gop.Add(frame);
            }

            if (gop != null) GOPs.Add(gop);

            CalcMinMax();

            return true;
        }
    }


    public class GOPsByTime : GOPsBy
    {
        public double IntervalDuration { get; private set; } = 1;


        public GOPsByTime(double intervalDuration) : base() {
            IntervalDuration = intervalDuration;
        }


        public bool Calc(double startTimeOffset)
        {
            if (Frames.Count == 0 || (GOPs.Count > 0 && StartTimeOffset == startTimeOffset)) return false; // no data or calculated already

            StartTimeOffset = startTimeOffset;

            GOP gop = new(StartTimeOffset, null, 0, IntervalDuration);

            foreach (var frame in Frames)
            {
                // The frame is started in one of the next GOP, so finalizing current GOP and creating a new one
                // It is possible that the frame if far away from current GOP, so adding more then one GOP if needed
                while ((frame.StartTime - StartTimeOffset) >= gop.FixedTimeEndTime)
                {
                    GOPs.Add(gop);
                    gop = new(StartTimeOffset, null, gop.FixedTimeEndTime, IntervalDuration);
                }

                gop.Add(frame);

                // The frame is not ending in current GOP, so finalizing current one and creating a new one
                // It is possible that the frame will belongs to more then one GOP
                while ((frame.EndTime - StartTimeOffset) > gop.FixedTimeEndTime)
                {
                    GOPs.Add(gop);
                    gop = new(StartTimeOffset, frame, gop.FixedTimeEndTime, IntervalDuration);
                }
            }

            if (gop != null) GOPs.Add(gop);

            CalcMinMax();

            return true;
        }
    }


    public class Frames
    {
        public int          Count                           { get { return FramesList.Count; } }
        public double?      Duration                        { get { return FramesList.Count > 0 ? (FramesList[^1].EndTime - (IsAdjustStartTime ? StartTime : 0)) : null; } }
        private List<Frame> FramesList                      { get; set; } = [];
        private GOPsByGOP   FramesByGOP                     { get; set; } = new();
        private GOPsByTime  FramesByTime                    { get; set; } = new(1);
        public double?      FramesDuration                  { get { return FramesList.Count > 0 ? (FramesList[^1].EndTime - FramesList[0].StartTime) : null; } }
        public double?      FramesEndTime                   { get { return FramesList.Count > 0 ? FramesList[^1].EndTime : null; } }
        public double?      FramesStartTime                 { get { return FramesList.Count > 0 ? FramesList[0].StartTime : null; } }
        public bool         IsAdjustStartTime               { get; private set; } = true;
        private bool        IsCalcStartTime                 { get; set; } = false;
        private int         MaxFrameSize                    { get; set; } = 0;
        public double       StartTime                       { get; set; } = 0;


        public int? Add(Frame frame, bool? isForceOrder = null)
        {
            var isOrder = isForceOrder == true || !frame.IsOrdered;
            if (frame.Size > MaxFrameSize) MaxFrameSize = frame.Size;
            if (isOrder)
            {
                var pos = PosFind(frame);
                if(pos != null) FramesList.Insert((int)pos, frame);
                return pos;
            }
            else
            {
                FramesList.Add(frame);
                return FramesList.Count - 1;
            }
        }


        public void Analyze()
        {
            if (IsCalcStartTime) FillFramesStartTime(StartTime);
            FramesByGOP.SetFrames(FramesList);
            FramesByTime.SetFrames(FramesList);
        }


        // todo@ caching?
        public List<DataPoint> DataPointsGet(string? plotViewType)
        {
            List<DataPoint> data = [];
            var startTimeOffset  = IsAdjustStartTime ? StartTime : 0;
            switch (plotViewType?.ToUpper() ?? "")
            {
                case "FRAME":
                    for (var idx = 0; idx < FramesList.Count; ++idx) data.AddRange(FramesList[idx].DataPointGet(plotViewType, startTimeOffset, idx == FramesList.Count - 1));
                    break;
                case "GOP":
                    FramesByGOP.Calc(startTimeOffset);
                    data.AddRange(FramesByGOP.DataPointsGet(plotViewType));
                    break;
                case "SECOND":
                    FramesByTime.Calc(startTimeOffset);
                    data.AddRange(FramesByTime.DataPointsGet(plotViewType));
                    break;
            }
            return data;
        }


        public void IsAdjustStartTimeSet(bool isAdjustStartTime)
        {
            if (isAdjustStartTime != IsAdjustStartTime)
            {
                IsAdjustStartTime = isAdjustStartTime;
                FramesByGOP.Clear();
                FramesByTime.Clear();
            }
        }


        public double? MaxXGet(string? plotViewType)
        {
            switch (plotViewType?.ToUpper() ?? "")
            {
                case "SECOND":
                    return Duration == null ? null : Math.Ceiling((double)Duration);
                default:
                    return Duration;
            }
        }


        public int MaxYGet(string? plotViewType)
        {
            var value           = 0;
            var startTimeOffset = (IsAdjustStartTime ? StartTime : 0);
            switch (plotViewType?.ToUpper() ?? "")
            {
                case "FRAME":
                    value = MaxFrameSize;
                    break;
                case "GOP":
                    FramesByGOP.Calc(startTimeOffset);
                    value = FramesByGOP.MaxSize ?? 0;
                    break;
                case "SECOND":
                    FramesByTime.Calc(startTimeOffset);
                    value = FramesByTime.MaxSize ?? 0;
                    break;
            }
            return (int)double.Round(Frame.ScaleY(plotViewType, value));
        }


        public FramesBitRates BitRatesCals()
        {
            if (Duration == null || Duration <= 0) return new();

            var startTimeOffset = (IsAdjustStartTime ? StartTime : 0);
            FramesByTime.Calc(startTimeOffset);

            return new FramesBitRates
            {
                Avg = (FramesByTime.TotalSize == null) ? null : new BitRate((int)double.Round((double)FramesByTime.TotalSize / (double)Duration)),
                Max = (FramesByTime.MaxSize   == null) ? null : new BitRate((int)FramesByTime.MaxSize),
                Min = (FramesByTime.MinSize   == null) ? null : new BitRate((int)FramesByTime.MinSize)
            };
        }


        private int? PosFind(Frame frame)
        {
            // Searching position from the end as usually the frame that we are adding will be somewhere close to the end (but not always the last)
            if (frame.StartTimeRaw == null)
            {
                // Frame does not have StartTime, use Pos instead to order and we will re-calculate all frames StartTime late
                IsCalcStartTime = true;
                for (int idx = FramesList.Count - 1; idx >= 0; --idx)
                {
                    if (frame.Pos == FramesList[idx].Pos) return null;
                    if (frame.Pos > FramesList[idx].Pos) return idx + 1;
                }
            }
            else
            {
                for (int idx = FramesList.Count - 1; idx >= 0; --idx)
                {
                    if (frame.StartTime == FramesList[idx].StartTime) return null;
                    if (frame.StartTime > FramesList[idx].StartTime) return idx + 1;
                }
            }
            return 0;
        }


        private void FillFramesStartTime(double startTime = 0)
        {
            foreach(var frame in FramesList)
            {
                if (frame.StartTimeRaw == null)
                {
                    frame.StartTimeRaw = startTime;
                    startTime += frame.Duration;
                }
                else
                {
                    startTime = frame.EndTime;
                }
            }
        }
    }
}