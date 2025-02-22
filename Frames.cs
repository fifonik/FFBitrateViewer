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
                FrameType    = packet.Flags?.IndexOf("K") >= 0 ? FramePictType.I : null,
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


        public DataPoint DataPointGet(double startTimeOffset, int sizeDivider = 1000 /* kB */)
        {
            return new DataPoint(StartTime - startTimeOffset, Size / sizeDivider);
        }
    }


    public class GOP
    {
        private List<Frame> _frames           { get; set; } = new();
        public  int         BitRate           { get { return Duration == 0 ? 0 : (int)double.Round(Size / (double)Duration); } }
        public  double      Duration          { get { return DurationFixed ?? DurationFrames ?? 0; } }
        public  double?     DurationFixed     { get; private set; } // NULL -- for normal GOPs, not NULL for fixed length GOPs as 'second'
        public  double?     DurationFrames    { get { return FramesCount == 0 ? null : double.Abs(_frames[^1].EndTime - _frames[0].StartTime); } }
        public  double      EndTime           { get { return EndTimeFixed ?? EndTimeFrames ?? 0; } }
        public  double?     EndTimeFixed      { get { return DurationFixed == null ? null : StartTime + DurationFixed; } }
        public  double?     EndTimeFrames     { get { return DurationFrames == null ? null : StartTime + DurationFrames; } }
        public  int         Size              { get; private set; } = 0;
        public  int         FramesCount       { get { return _frames.Count; } }
        public  double      StartTime         { get; private set; }

        public GOP(double startTime, double? duration = null) {
            if(duration < 0) throw new ArgumentException("Must be greater then 0", "fixedDuration"); // todo@ can it be 0?
            DurationFixed = duration;
            StartTime     = startTime;
        }


        public GOP(Frame frame, double? duration = null) : this(frame.StartTime, duration)
        {
        }


        public void Clear()
        {
            _frames.Clear();
            Size = 0;
        }


        public void Add(Frame frame)
        {
            if (DurationFixed == null) // Not SECOND based
            {
                if (frame.FrameType == FramePictType.I)
                {
                    if (FramesCount > 0) throw new ArgumentException("I-frame can only be the first in GOP");
                }
                else
                {
                    // No exception as frames could be added out of order
                    // So it is possible that P-frame will be added 1st and then I-frame with smaller start time will be added 2nd
                    // if (FramesCount == 0) throw new ArgumentException("First frame in GOP must be I-frame");
                }
            }
            _frames.Add(frame);
        }


        public void SizeAdd(Frame frame, double startTimeOffset = 0)
        {
            Size += frame.GetSize(StartTime + startTimeOffset, StartTime + DurationFixed + startTimeOffset);
        }


        public List<DataPoint> DataPointsGet(double startTimeOffset, int sizeDivider = 1000 /* kB */)
        {
            List<DataPoint> data = new();
            if (FramesCount == 0) return data;

            var bitRate = (int)double.Round(8 /* bit => byte */ * (double)BitRate / (double)sizeDivider);
            data.Add(new DataPoint(StartTime - startTimeOffset, bitRate));
            data.Add(new DataPoint(EndTime - startTimeOffset, bitRate));

            return data;
        }
    }


    public class Frames
    {
        private bool        _isCalcStartTime             {  get; set; } = false;
        private List<Frame> _frames                      { get; set; } = new();
        private List<GOP>   _framesByGOP                 { get; set; } = new();
        private List<GOP>   _framesByTime                { get; set; } = new();
        private double?     _framesByTimeDuration        { get; set; }
        private double?     _framesByTimeStartTimeOffset { get; set; }
        private int         _maxSizeFrame                { get; set; } = 0;
        private int         _maxSizeGOP                  { get; set; } = 0;
        private int         _maxSizeTime                 { get; set; } = 0;

        public bool         IsAdjustStartTime            { get; private set; } = true;
        public double?      Duration                     { get { return Count > 0 ? (_frames[^1].EndTime - (IsAdjustStartTime ? StartTime : 0)) : null; } }
        public int          Count                        { get { return _frames.Count; } }
        public double       StartTime                    { get; set; } = 0;
        public double?      FramesDuration               { get { return Count > 0 ? (_frames[^1].EndTime - _frames[0].StartTime) : null; } }
        public double?      FramesEndTime                { get { return Count > 0 ? _frames[^1].EndTime : null; } }
        public double?      FramesStartTime              { get { return Count > 0 ? _frames[0].StartTime : null; } }


        public int? Add(Frame frame, bool? isForceOrder = null)
        {
            var isOrder = isForceOrder == true || !frame.IsOrdered;
            if (frame.Size > _maxSizeFrame) _maxSizeFrame = frame.Size;
            if (isOrder)
            {
                var pos = PosFind(frame);
                if(pos != null) _frames.Insert((int)pos, frame);
                return pos;
            }
            else
            {
                _frames.Add(frame);
                return Count - 1;
            }
        }

        public void Analyze()
        {
            if (_isCalcStartTime)
            {
                CalcFramesStartTime(StartTime);
            }
        }

        public void Clear()
        {
            _frames.Clear();
            _framesByGOP.Clear();
            _framesByTime.Clear();
            _maxSizeFrame = 0;
            _maxSizeGOP   = 0;
            _maxSizeTime  = 0;
            _framesByTimeDuration        = null;
            _framesByTimeStartTimeOffset = null;
        }


        // todo@ caching?
        public List<DataPoint> DataPointsGet(string? plotViewType, int sizeDivider = 1000/* kB */)
        {
            List<DataPoint> data = new();
            var startTimeOffset  = (IsAdjustStartTime ? StartTime : 0);
            switch (plotViewType?.ToUpper() ?? "")
            {
                case "FRAME":
                    foreach (var frame in _frames) data.Add(frame.DataPointGet(startTimeOffset, sizeDivider));
                    break;
                case "GOP":
                    GroupByGOP();
                    foreach (var gop in _framesByGOP) data.AddRange(gop.DataPointsGet(startTimeOffset, sizeDivider));
                    break;
                case "SECOND":
                    GroupByTime(1, startTimeOffset);
                    foreach (var gop in _framesByTime) data.AddRange(gop.DataPointsGet(0, sizeDivider));
                    break;
            }
            return data;
        }


        public void IsAdjustStartTimeSet(bool isAdjustStartTime)
        {
            if (isAdjustStartTime != IsAdjustStartTime)
            {
                IsAdjustStartTime = isAdjustStartTime;
                _framesByGOP.Clear();
                _framesByTime.Clear();
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


        public int MaxYGet(string? plotViewType, int sizeDivider = 1000/* kB */)
        {
            var value           = 0;
            var startTimeOffset = (IsAdjustStartTime ? StartTime : 0);
            switch (plotViewType?.ToUpper() ?? "")
            {
                case "FRAME":
                    value = _maxSizeFrame;
                    break;
                case "GOP":
                    GroupByGOP();
                    value = _maxSizeGOP * 8 /* B/s => b/s */;
                    break;
                case "SECOND":
                    GroupByTime(1, startTimeOffset);
                    value = _maxSizeTime * 8 /* B/s => b/s */;
                    break;
            }
            return (int)double.Round(value / sizeDivider);
        }


        private void GroupByGOP()
        {
            if (_framesByGOP.Count > 0) return;
            _framesByGOP = GroupByGOP(_frames);
            _maxSizeGOP = 0;
            foreach (var gop in _framesByGOP) if (gop.BitRate > _maxSizeGOP) _maxSizeGOP = gop.BitRate;
        }


        private static List<GOP> GroupByGOP(List<Frame> frames)
        {
            List<GOP> result = new();
            if (frames.Count == 0) return result;

            GOP? gop = null;

            foreach (var frame in frames)
            {
                if (gop == null)
                {
                    gop = new(frame);
                    gop.Add(frame);
                    gop.SizeAdd(frame);
                    continue;
                }

                // On every I-frame finalyzing current GOP and creating a new one
                if (frame.FrameType == FramePictType.I)
                {
                    result.Add(gop);
                    gop = new(frame);
                    gop.Add(frame);
                    gop.SizeAdd(frame);
                    continue;
                }

                gop.Add(frame);
                gop.SizeAdd(frame);
            }

            if (gop != null) result.Add(gop);
            return result;
        }


        private void GroupByTime(double duration, double startTimeOffset)
        {
            if (_framesByTime.Count > 0 && _framesByTimeDuration == duration && _framesByTimeStartTimeOffset == startTimeOffset) return; // calculated already
            _framesByTime                = GroupByTime(_frames, duration, startTimeOffset);
            _framesByTimeDuration        = duration;
            _framesByTimeStartTimeOffset = startTimeOffset;
            _maxSizeTime                 = 0;
            foreach (var gop in _framesByTime) if (gop.BitRate > _maxSizeTime) _maxSizeTime = gop.BitRate;
        }


        private static List<GOP> GroupByTime(List<Frame> frames, double duration, double startTimeOffset)
        {
            List<GOP> result = new();

            if (frames.Count == 0 || duration <= 0) return result;

            GOP? gop = null;

            foreach (var frame in frames)
            {
                if (gop == null)
                {
                    gop = new(0, duration);
                    gop.Add(frame);
                    gop.SizeAdd(frame, startTimeOffset);
                    continue;
                }

                // The frame is started in one of the next GOP, so finallizing current GOP and creating a new one
                // It is possible that the frame if far away from prev GOP, so adding a number of blank GOPs if needed
                while ((frame.StartTime - startTimeOffset) > gop.EndTime)
                {
                    result.Add(gop);
                    gop = new(gop.EndTime, duration);
                    gop.Add(frame);
                    gop.SizeAdd(frame, startTimeOffset);
                }

                gop.Add(frame);
                gop.SizeAdd(frame, startTimeOffset);

                // The frame is ended in one of the next GOPs, so finallizing current GOP and creating a new one
                while ((frame.EndTime - startTimeOffset) > gop.EndTime) {
                    result.Add(gop);
                    gop = new(gop.EndTime, duration);
                    gop.Add(frame);
                    gop.SizeAdd(frame, startTimeOffset);
                }
            }

            if (gop != null) result.Add(gop);

            return result;
        }


        public FramesBitRates BitRatesCals()
        {
            if (Duration == null || Duration <= 0) return new();

            var startTimeOffset = (IsAdjustStartTime ? StartTime : 0);
            GroupByTime(1, startTimeOffset);

            int? max = null;
            int? min = null;
            ulong total = 0;
            foreach (var gop in _framesByTime) {
                total += (ulong)gop.Size;
                if (gop.BitRate > 0 && (max == null || gop.BitRate > max)) max = gop.BitRate;
                if (gop.BitRate > 0 && (min == null || gop.BitRate < min)) min = gop.BitRate;
            }

            return new FramesBitRates
            {
                Avg = (total == 0)  ? null : new BitRate((int)double.Round(8/* B/s => b/s */ * (double)total / (double)Duration)),
                Max = (max == null) ? null : new BitRate((int)max * 8/* B/s => b/s */),
                Min = (min == null) ? null : new BitRate((int)min * 8/* B/s => b/s */)
            };
        }


        private int? PosFind(Frame frame)
        {
            // Searching position from the end as usually the frame that we are adding will be somewhere close to the end (but not always the last)
            // todo@ should probably stop loop if found key frame
            if (frame.StartTimeRaw == null)
            {
                // Frame does not have StartTime, use Pos instead and will re-calculate all frames StartTime later
                _isCalcStartTime = true;
                for (int idx = _frames.Count - 1; idx >= 0; --idx)
                {
                    if (frame.Pos == _frames[idx].Pos) return null;
                    if (frame.Pos > _frames[idx].Pos) return idx + 1;
                }
            }
            else
            {
                for (int idx = _frames.Count - 1; idx >= 0; --idx)
                {
                    if (frame.StartTime == _frames[idx].StartTime) return null;
                    if (frame.StartTime > _frames[idx].StartTime) return idx + 1;
                }
            }
            return 0;
        }

        private void CalcFramesStartTime(double startTime = 0)
        {
            for (int idx = 0; idx < _frames.Count; ++idx)
            {
                if(_frames[idx].StartTimeRaw != null)
                {
                    startTime = (double)_frames[idx].EndTime;
                    continue;
                }
                _frames[idx].StartTimeRaw = startTime;
                startTime += _frames[idx].Duration;
            }
        }
    }
}