using OxyPlot;
using System.Collections.Generic;


namespace FFBitrateViewer
{
    public enum FramePictType
    {
        I,
        P,
        B
    };


    public class Frame
    {
        public int?           BitRate   { get; set; }
        public double         Duration  { get; set; }
        public FramePictType? FrameType { get; set; } // I, P, B
        public bool           IsOrdered { get; set; }
        public int            Size      { get; set; }
        public double         StartTime { get; set; }


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
            if (frame.DurationTime == null || frame.Size == null || frame.BestEffortTimestampTime == null) return null;

            FramePictType? pictType = frame.PictType?[0] switch
            {
                'I' => FramePictType.I,
                'P' => FramePictType.P,
                'B' => FramePictType.B,
                _ => null
            };

            return new Frame()
            {
                Duration  = (double)frame.DurationTime,
                FrameType = pictType,
                IsOrdered = true, // 'Frames' returned by FFProbe are already ordered by BestEffortTimestampTime
                Size      = (int)frame.Size,
                StartTime = (double)frame.BestEffortTimestampTime
            };
        }

/*
        public int GetSizeInInterval(double intervalStart, double intervalDuration)
        {
            if (intervalDuration <= 0 || Duration == 0 || Size == 0) return 0;

            if ((StartTime + Duration) <= intervalStart || StartTime >= (intervalStart + intervalDuration)) return 0;    // The frame is outside of the interval
            if (StartTime >= intervalStart && (StartTime + Duration) <= (intervalStart + intervalDuration)) return Size; // The frame is fully inside the interval

            var start = double.Max((double)StartTime, intervalStart);
            var end   = double.Min((double)StartTime + Duration, intervalStart + intervalDuration);

            return (int)double.Round((end - start) / Duration * Size);
        }
*/
    }


    public class Frames
    {
        private bool       isBitRateCalculated = false;
        public bool        IsAdjustStartTime { get; private set; } = true;
        public List<Frame> Items             { get; set; } = new();
        public double      StartTime         { get; set; } = 0;


        public int Add(Frame frame, bool? isForceOrder = null)
        {
            isForceOrder ??= !frame.IsOrdered;
            if (isForceOrder == true)
            {
                var pos = PosFind(frame);
                Items.Insert(pos, frame);
                return pos;
            }
            else
            {
                Items.Add(frame);
                return Items.Count - 1;
            }
        }


        public void Clear()
        {
            Items.Clear();
            isBitRateCalculated = false;
        }


        public List<DataPoint> DataPointsGet(string? plotViewType)
        {
            var data      = new List<DataPoint>();
            var startTime = (IsAdjustStartTime ? StartTime : 0);
            switch (plotViewType?.ToUpper() ?? "")
            {
                case "FRAME":
                    foreach (var frame in Items) data.Add(new DataPoint(frame.StartTime - startTime, frame.Size / 1000));
                    break;
                case "SECOND":
                    CalcBitRateFromItems(1, startTime);
                    foreach (var frame in Items) data.Add(new DataPoint(frame.StartTime - startTime, (frame.BitRate ?? 0) / 1000));
                    break;
            }
            return data;
        }


        public double? DurationGet()
        {
            return Items.Count > 0 ? (Items[^1].StartTime + Items[^1].Duration - (IsAdjustStartTime ? StartTime : 0)) : null;
        }


        public void IsAdjustStartTimeSet(bool isAdjustStartTime)
        {
            IsAdjustStartTime = isAdjustStartTime;
            isBitRateCalculated = false;
        }


        // todo@ cache
        public int MaxYGet(string? plotViewType)
        {
            int value = 0;
            switch (plotViewType?.ToUpper() ?? "")
            {
                case "FRAME":
                    foreach (var frame in Items) if (frame.Size > value) value = frame.Size;
                    break;
                case "SECOND":
                    value = BitRateMaxCalc() ?? 0;
                    break;
            }
            return (int)double.Round(value / 1000);
        }


        public int? BitRateAvgCalc()
        {
            var d = DurationGet();
            if (d == null) return null;
            ulong value = 0;
            foreach (var frame in Items) value += (ulong)frame.Size;
            return (int)double.Round(value / (double)d * 8);
        }


        public int? BitRateMaxCalc()
        {
            int value = -1;
            CalcBitRateFromItems(1, IsAdjustStartTime ? StartTime : 0);
            foreach (var frame in Items) if (frame.BitRate > value) value = (int)frame.BitRate;
            return value >= 0 ? value : null;
        }


        private int PosFind(Frame frame)
        {
            for (int idx = Items.Count - 1; idx >= 0; --idx)
            {
                // todo@ should probably stop if found key frame
                if (frame.StartTime > Items[idx].StartTime) return idx + 1;
            }
            return 0;
        }


        // Size that packet adds into interval's total size is calculated based on percentage of time the packet is within interval
        // Intervals:  AAAAAAAAAABBBBBBBBBBCCCCCCCCCCDDDDDDDDDDEEEEEEEEEEFFFFFFFFFF
        // Packets:       111122223333444455555555555555555555555556666
        // Packet 1: 100% in interval A
        // Packet 2: 3/4 in interval A and 1/4 in interval B
        // Packet 3, 4: 100% in interval B
        // Packet 5: 1/25 in interval B, 10/25 in interval C, 10/25 in interval D, 4/25 in interval E
        // Packet 6: 100% in interval E
        // etc

        public void CalcBitRateFromItems(double intervalDuration = 1, double intervalStartTime = 0)
        {
            if (isBitRateCalculated || Items.Count == 0 || intervalDuration == 0) return;

            int bitrate;
            //int max              = -1;
            int intervalSize     = 0;
            int nextIntervalSize = 0;

            var frames = new List<int>();

            for (int i = 0; i < Items.Count; ++i)
            {
                var frame     = Items[i];
                var duration  = frame.Duration;
                var size      = frame.Size;
                var startTime = frame.StartTime;

                if (duration > intervalDuration) // The packet is longer than interval
                {
                    int fullIntervalsCount  = (int)double.Truncate(duration / intervalDuration);     // => 2
                    int sizePerFullInterval = (int)double.Round(size * intervalDuration / duration); // => x * 10 / 25
                    duration  %= intervalDuration;                                                   // => 5
                    size      -= fullIntervalsCount * sizePerFullInterval;
                    startTime += fullIntervalsCount * intervalDuration;

                    //if (sizePerFullInterval > max) max = sizePerFullInterval; // todo@ Show it somehow
                }

                if (startTime > (intervalStartTime + intervalDuration))
                {
                    // A new interval is just started

                    // Updating BitRate for frames in prev. interval
                    bitrate = (int)double.Round(intervalSize / intervalDuration * 8);
                    foreach (var f in frames) Items[f].BitRate = bitrate;
                    frames.Clear();

                    //if (intervalSize > max) max = intervalSize;
                    intervalStartTime += intervalDuration;
                    intervalSize       = nextIntervalSize;
                }

                if ((startTime + duration) < (intervalStartTime + intervalDuration))
                {
                    // The packet is ended in the current interval, so its size is fully accounted to current interval
                    intervalSize     += size;
                    nextIntervalSize  = 0;
                }
                else
                {
                    // The packet is ended in the next interval, so only part of the packet's size is accounted to size of the current interval
                    int sizeForPart  = (int)double.Round(size * ((intervalStartTime + intervalDuration) - startTime) / intervalDuration);
                    intervalSize     += sizeForPart;
                    nextIntervalSize = size - sizeForPart;
                }
                frames.Add(i);
            }

            //if (intervalSize > max) max = intervalSize; // last part

            bitrate = (int)double.Round(intervalSize / intervalDuration * 8);
            foreach (var f in frames) Items[f].BitRate = bitrate;
            frames.Clear();

            isBitRateCalculated = true;
        }

    }
}