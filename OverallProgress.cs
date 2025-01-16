using System;


namespace FFBitrateViewer
{
    public class OverallProgress : Bindable
    {
        public  bool    IsActive            { get { return Get<bool>(); }    private set { Set(value); } }
        private bool    IsIndeterminate     { get { return Get<bool>(); }    set { Set(value); } }
        public  bool    IsIndeterminateXAML { get { return IsIndeterminate && IsActive; } }
        public  int     Current             { get { return Get<int>(); }     set { Set(value); /* PercentUpdate(); */ } }
        public  int     Max                 { get { return Get<int>(); }     set { Set(value); /* PercentUpdate(); */ } }
        public  double  CurrentPercent      { get { return Get<double>(); }  private set { Set(value); } }
        public  string? Text                { get { return Get<string?>(); } private set { Set(value); } }
        public  double  ProcessedDuration   { get { return Get<double>();  } set { Set(value); } }


        public OverallProgress(bool indeterminate = false) {
            IsIndeterminate = indeterminate;
            Max = 100;
            Current = 0;
            ProcessedDuration = 0;
            //IsActive = true;
            //Text = "123";
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
            Current = 0;
            ProcessedDuration = 0;
        }


        public void FileProgressSet(int fileIndex, Frame? frame = null)
        {
            string s = "Processing file: " + (fileIndex + 1);
            if (frame != null)
            {
                s += ", time: " + TimeSpan.FromSeconds(frame.StartTime).ToString(@"hh\:mm\:ss");
                Current = (int)(ProcessedDuration + frame.StartTime);
            }
            Text = s;
        }
    }


}
