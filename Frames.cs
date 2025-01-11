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
        public int            Size         { get; set; } // Frame size in bytes
        //public int            SizeInGOP    { get; set; }
        public double         StartTime    { get; set; }


        public static Frame? CreateFrame(FFProbePacket packet)
        {
            if (packet.DurationTime == null || packet.Size == null || packet.PTSTime == null) return null;
            return new Frame()
            {
                Duration  = (double)packet.DurationTime,
                FrameType = packet.Flags?.IndexOf("K") >= 0 ? FramePictType.I : null,
                IsOrdered = false, // 'Packets' returned by FFProbe are ordered by DTS, not PTS so will need to order them later when adding onto list
                Size      = (int)packet.Size,
                StartTime = (double)packet.PTSTime
            };
        }


        public static Frame? CreateFrame(FFProbeFrame frame)
        {
            if (frame.DurationTime == null || frame.Size == null || (frame.BestEffortTimestampTime == null && frame.PTSTime == null)) return null;

            FramePictType? pictType = frame.PictType?[0] switch
            {
                'I' => FramePictType.I,
                'P' => FramePictType.P,
                'B' => FramePictType.B,
                _   => null
            };

            return new Frame()
            {
                Duration  = (double)frame.DurationTime,
                FrameType = pictType,
                IsOrdered = true, // 'Frames' returned by FFProbe are already ordered by BestEffortTimestampTime
                Size      = (int)frame.Size,
                StartTime = frame.BestEffortTimestampTime ?? frame.PTSTime ?? 0
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
        public  double?     DurationFrames    { get { return _frames.Count == 0 ? null : double.Abs(_frames[^1].EndTime - _frames[0].StartTime); } }
        public  double      EndTime           { get { return EndTimeFixed ?? EndTimeFrames ?? 0; } }
        public  double?     EndTimeFixed      { get { return DurationFixed == null ? null : StartTime + DurationFixed; } }
        public  double?     EndTimeFrames     { get { return DurationFrames == null ? null : StartTime + DurationFrames; } }
        public  int         Size              { get; private set; } = 0;
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
            if (DurationFixed == null) // GOP based
            {
                if (frame.FrameType == FramePictType.I)
                {
                    if (_frames.Count > 0) throw new ArgumentException("I-frame can only be the first in GOP");
                }
                else
                {
                    // No exception as frames could be added out of order
                    // So it is possible that P-frame will fe added 1st and then I-frame with smaller start time will be added
                    // if (_frames.Count == 0) throw new ArgumentException("First frame in GOP must be I-frame");
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
            if (_frames.Count == 0) return data;

            var bitRate = (int)double.Round(8 /* bit => byte */ * (double)BitRate / (double)sizeDivider);
            data.Add(new DataPoint(StartTime - startTimeOffset, bitRate));
            data.Add(new DataPoint(EndTime - startTimeOffset, bitRate));

            return data;
        }
    }


    public class Frames
    {
        private List<Frame> _frames                      { get; set; } = new();
        private List<GOP>   _framesByGOP                 { get; set; } = new();
        private List<GOP>   _framesByTime                { get; set; } = new();
        private double?     _framesByTimeDuration        { get; set; }
        private double?     _framesByTimeStartTimeOffset { get; set; }

        public bool         IsAdjustStartTime            { get; private set; } = true;
        public double?      Duration                     { get { return _frames.Count > 0 ? (_frames[^1].EndTime - (IsAdjustStartTime ? StartTime : 0)) : null; } } // todo@ should not duration be the same regardless IsAdjustStartTime?
        public int?         FramesCount                  { get { return _frames.Count; } }
        public double       StartTime                    { get; set; } = 0;


        public int Add(Frame frame, bool? isForceOrder = null)
        {
            var isOrder = isForceOrder == true || !frame.IsOrdered;
            if (isOrder)
            {
                var pos = PosFind(frame);
                _frames.Insert(pos, frame);
                return pos;
            }
            else
            {
                _frames.Add(frame);
                return _frames.Count - 1;
            }
        }


        public void Clear()
        {
            _frames.Clear();
            _framesByGOP.Clear();
            _framesByTime.Clear();
        }


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
            int value           = 0;
            var startTimeOffset = (IsAdjustStartTime ? StartTime : 0);
            switch (plotViewType?.ToUpper() ?? "")
            {
                case "FRAME":
                    foreach (var frame in _frames) if (frame.Size > value) value = frame.Size;
                    break;
                case "GOP":
                    GroupByGOP();
                    foreach (var gop in _framesByGOP) if (gop.BitRate > value) value = gop.BitRate;
                    value *= 8; // to kB
                    break;
                case "SECOND":
                    GroupByTime(1, startTimeOffset);
                    foreach (var gop in _framesByTime) if (gop.BitRate > value) value = gop.BitRate;
                    value *= 8; // to kB
                    break;
            }
            return (int)double.Round(value / sizeDivider);
        }


        private void GroupByGOP()
        {
            if (_framesByGOP.Count > 0) return;
            _framesByGOP = GroupByGOP(_frames);
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

                // On every I-frame finalizing current GOP and creating a new one
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
                Avg = (total == 0)  ? null : new BitRate((int)double.Round(8/* bit => byte */ * (double)total / (double)Duration)),
                Max = (max == null) ? null : new BitRate((int)max * 8/* bit => byte */),
                Min = (min == null) ? null : new BitRate((int)min * 8/* bit => byte */)
            };
        }


        private int PosFind(Frame frame)
        {
            // Searching position from the end as usually the frame that we are adding will be somewhere close to the end (but not always the last)
            for (int idx = _frames.Count - 1; idx >= 0; --idx)
            {
                // todo@ should probably stop if found key frame
                if (frame.StartTime > _frames[idx].StartTime) return idx + 1;
            }
            return 0;
        }
    }
}