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
        public int? FramesCount
        {
            get { return (int?)GetValue(FramesCountProperty); }
            set { SetValue(FramesCountProperty, value); }
        }
        public static readonly DependencyProperty InfoProperty        = DependencyProperty.Register("MediaInfo",   typeof(MediaInfo), typeof(MediaInfoBox));
        public static readonly DependencyProperty FramesCountProperty = DependencyProperty.Register("FramesCount", typeof(int?),      typeof(MediaInfoBox));

        public MediaInfoBox()
        {
            InitializeComponent();
        }
    }
}
