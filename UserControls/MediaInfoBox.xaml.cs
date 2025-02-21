using System.Windows;
using System.Windows.Controls;


namespace FFBitrateViewer.UserControls
{
    public partial class MediaInfoBox : UserControl
    {
        public MediaInfo MediaInfo
        {
            get { return (MediaInfo)GetValue(InfoProperty); }
            set { SetValue(InfoProperty, value); }
        }
        public Frames Frames
        {
            get { return (Frames)GetValue(FramesProperty); }
            set { SetValue(FramesProperty, value); }
        }
        public static readonly DependencyProperty InfoProperty        = DependencyProperty.Register("MediaInfo",   typeof(MediaInfo), typeof(MediaInfoBox));
        public static readonly DependencyProperty FramesProperty      = DependencyProperty.Register("Frames",      typeof(Frames),    typeof(MediaInfoBox));

        public MediaInfoBox()
        {
            InitializeComponent();
        }
    }
}
