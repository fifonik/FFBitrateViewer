using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;


namespace FFBitrateViewer
{
    public class LinkItem
    {
        public string?   Name    { get; set; }
        public string?   Link    { get; set; }
    }

    public class AboutViewModel : Bindable
    {
        public string?   ProgramAuthor  { get { return Get<string>(); }   private set { Set(value); } }
        public string?   ProgramDesc    { get { return Get<string>(); }   private set { Set(value); } }
        public LinkItem? ProgramHome    { get { return Get<LinkItem>(); } private set { Set(value); } }
        public string?   ProgramTitle   { get { return Get<string>(); }   private set { Set(value); } }
        public string?   ProgramVersion { get { return Get<string>(); }   private set { Set(value); } }
        public string?   WindowTitle    { get { return Get<string>(); }   private set { Set(value); } }

        public AboutViewModel()
        {
            ProgramHome = new LinkItem
            {
                Link    = "https://github.com/fifonik/FFBitrateViewer",
                Name    = "github.com/fifonik/FFBitrateViewer"
            };


            ProgramAuthor   = "fifonik";
            ProgramDesc     = "FFBitrateViewer allows you to see video stream bit rate distribution.";
            ProgramTitle    = ProgramInfo.Name + " – another FFProbe GUI";
            ProgramVersion  = ProgramInfo.Version;

            WindowTitle = "About " + ProgramInfo.Name;
        }
    }



    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            DataContext = new AboutViewModel();
        }


        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }


        private void ProgramHome_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
