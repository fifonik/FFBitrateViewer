using System;
using System.Collections.ObjectModel;


namespace FFBitrateViewer
{
    public class OverallProgress : Bindable
    {
        private int     FilesWeightTotal        = 1;
        private int     FilesWeightProcessed    = 0;
        private int     CurrentFileWeight       = 1;
        private int     FramesTotal             = 0;
        private int     FramesProcessed         = 0;
        public bool     IsActive        { get { return Get<bool>(); }    private set { Set(value); } }
        public bool     IsIndeterminate { get { return Get<bool>(); }    private set { Set(value); } }
        public int      Current         { get { return Get<int>(); }     private set { Set(value); } }
        public int      Max             { get { return Get<int>(); }     private set { Set(value); } }
        public double   CurrentPercent  { get { return Get<double>(); }  private set { Set(value); } }
        public string?  Text            { get { return Get<string?>(); } private set { Set(value); } }


        public OverallProgress() {
            //IsActive = true;
            //IsIndeterminate = true;
            //Text = "123";
        }


        public void Init()
        {
            FilesWeightTotal        = 0;
            FilesWeightProcessed    = 0;
            CurrentFileWeight       = 1;
            FramesTotal             = 0;
            FramesProcessed         = 0;
            MaxUpdate();
            CurrentUpdate();
        }


        public void Init(int max)
        {
            Reset();
            Max = max;
            CurrentPercentUpdate();
        }


        public void Start()
        {
            IsActive = true;
        }


        public void Stop()
        {
            IsActive = false;
        }


        public void Show(string text)
        {
            Text = text;
            IsIndeterminate = true;
            Start();
        }


        public void Hide()
        {
            Text = null;
            IsIndeterminate = false;
            Stop();
        }


        public void Reset()
        {
            FilesWeightTotal        = 1;
            FilesWeightProcessed    = 0;
            CurrentFileWeight       = 1;
            FramesTotal             = 0;
            FramesProcessed         = 0;
            MaxUpdate();
            CurrentUpdate();
        }


        public void Inc()
        {
            ++Current;
            CurrentPercentUpdate();
        }


        public void FramesTotalSet(int frames)
        {
            if (frames < 0) return;
            FramesTotal = frames;
            CurrentUpdate();
        }


        public void FramesProcessedSet(int frame)
        {
            if (frame < 0 || FramesTotal == 0) return;
            FramesProcessed = frame;
            CurrentUpdate();
        }


        private void CurrentUpdate()
        {
            Current = 100 * FilesWeightProcessed + ((FramesTotal != 0) ? CurrentFileWeight * (int)Math.Round((double)100 * FramesProcessed / FramesTotal) : 0);
            CurrentPercentUpdate();
        }


        private void CurrentPercentUpdate()
        {
            CurrentPercent = Max == 0 ? 0 : (Current / (double)Max);
        }


        private void MaxUpdate()
        {
            Max = 100 * FilesWeightTotal;
            CurrentPercentUpdate();
        }
    }


}
