using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Utilities;

namespace FFBitrateViewer
{
    public class ListViewItemDropTarget
    {
        public int?         Index   { get; set; } = null;
        public ListViewItem Item    { get; set; }
        public ListViewItemDropTarget(ListViewItem item) { Item = item; }
    }


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ArgsOptions? argsOptions;
        private int? dragFileSrcIndex = null;
        private ProgramConfig? programConfig;
        private ProgramOptions? programOptions;
        private Point startPoint = new();
        public  MainViewModel vm;

        public MainWindow()
        {
            Initialize();

            InitializeComponent();

            Dispatcher.ShutdownStarted += new EventHandler(Dispatcher_ShutdownStarted);

            vm = new MainViewModel
            {
                IsAutoRun = argsOptions?.IsFilled == true && argsOptions?.Run == true
            };
            DataContext = vm;

            InitializeCommands();
        }


        private void Initialize()
        {
            argsOptions = new ArgsOptions(Environment.GetCommandLineArgs());
            bool logCommands;
            if (argsOptions.IsFilled)
            {
                programOptions = new ProgramOptions();
                logCommands = argsOptions.LogCommands == true;
            }
            else
            {
                programOptions = ProgramOptions.LoadFromSettings();
                logCommands = programOptions.LogCommands == true;
            }
            if (!string.IsNullOrEmpty(argsOptions.TempDir)) Global.TempDir = argsOptions.TempDir;

            Log.Init(new Logger(argsOptions.LogLevel, FileSpecBuild("log"), true/*append*/, true/*add timestamp*/, true/*auto flush*/), logCommands || argsOptions.LogLevel == LogLevel.DEBUG);

            Log.Write(LogLevel.DEBUG, "Started");

            // To debug non-english culture
            //CultureInfo ci = new CultureInfo("fr-FR");
            //Thread.CurrentThread.CurrentCulture = ci;
            //Thread.CurrentThread.CurrentUICulture = ci;

            Log.Write(LogLevel.DEBUG, "System culture:" + Thread.CurrentThread.CurrentCulture.ToString());

            string fs = FileSpecBuild("conf");
            if (File.Exists(fs))
            {
                try
                {
                    programConfig = ProgramConfig.LoadFromFile(fs);
                    if (!string.IsNullOrEmpty(programConfig.TempDir) && string.IsNullOrEmpty(argsOptions?.TempDir)) Global.TempDir = programConfig.TempDir;
                }
                catch (Exception ex)
                {
                    Log.Write(LogLevel.ERROR, "Cannot process configuration file " + fs + ": " + ex.Message);
                    Log.Close();
                }
            }
            programConfig ??= new ProgramConfig();
            FF.Init(programConfig);

            /*
            if (ProgramConfig.Plots.Series.Count > 0)
            {
                var serieStyleList = new List<SerieStyle>();
                foreach (var item in ProgramConfig.Plots.Series) serieStyleList.Add(item.SerieStyle);
                MyPlotModel.PlotStyles = serieStyleList;
            }
            */

            Log.Write(LogLevel.DEBUG, "Initialize finished");
        }


        private void InitializeCommands()
        {
            Log.Write(LogLevel.DEBUG, "InitializeCommands started");

            vm.AboutShowCmd = new RelayCommand(
                param =>
                {
                    var about = new AboutWindow();
                    about.ShowDialog();
                },
                param => { return true; }
            );

            vm.ExecStartCmd = new RelayCommand(
                param =>
                {
                    if (!vm.IsRunning && vm.IsReady && vm.IsFilesReady() && vm.IsOptionsReady())
                    {
                        vm.FilesProcess();
                    }
                },
                param => { return !vm.IsRunning && vm.IsReady && vm.IsFilesReady() && vm.IsOptionsReady(); }
            );

            vm.ExecStopCmd = new RelayCommand(
                param =>
                {
                    if (vm.IsRunning)
                    {
                        vm.FilesProcessCancel();
                    }
                },
                param => { return vm.IsRunning; }
            );

            vm.ExecToggleCmd = new RelayCommand(
                param =>
                {
                    if (vm.ExecStopCmd?.CanExecute(null) == true)
                    {
                        vm.FilesProcessCancel();
                    }
                    else if (vm.ExecStartCmd?.CanExecute(null) == true)
                    {
                        vm.FilesProcess();
                    }
                },
                param => { return vm.ExecStartCmd?.CanExecute(null) == true || vm.ExecStopCmd?.CanExecute(null) == true; }
            );

            vm.ExitCmd = new RelayCommand(
                param => { Application.Current.Shutdown(); },
                param => { return true; }
            );

            vm.FilesAddCmd = new RelayCommand(
                param => { if (vm.IsReady && vm.IsFileCanBeAdded()) MediaFilesOpenDialog("Select Distorted Files", true); },
                param => { return vm.IsReady && vm.IsFileCanBeAdded(); }
            );

            vm.FilesClearCmd = new RelayCommand(
                param => { if (vm.IsReady && vm.Files.Count > 0) vm.FilesClear(); },
                param => { return vm.IsReady && vm.Files.Count > 0; }
            );

            vm.FilesRemoveCmd = new RelayCommand(
                param =>
                {
                    if (vm.IsReady && listviewFiles != null && listviewFiles.SelectedItems.Count > 0)
                    {
                        var selected = new FileItem[listviewFiles.SelectedItems.Count];
                        listviewFiles.SelectedItems.CopyTo(selected, 0);
                        foreach (var file in selected) vm.FileRemove(file);
                    }
                },
                param => { return vm.IsReady && listviewFiles != null && listviewFiles.SelectedItems.Count > 0; }
            );

            vm.MediaInfoReloadCmd = new RelayCommand(
                param =>
                {
                    if (vm.IsReady && !vm.IsMediaInfoGetRunning && vm.Files.Count > 0) vm.MediaInfoRefresh();
                },
                param => { return vm.IsReady && !vm.IsMediaInfoGetRunning && vm.Files.Count > 0; }
            );

            vm.PlotExportToClipboardCmd = new RelayCommand(
                param =>
                {
                    if (vm.IsReady && !vm.IsPlotEmpty()) vm.PlotExport();
                },
                param => { return vm.IsReady && !vm.IsPlotEmpty(); }
            );

            vm.PlotExportToFileCmd = new RelayCommand(
                param =>
                {
                    if (vm.IsReady && !vm.IsPlotEmpty()) ImageFileSaveDialog("Save Image", vm.PlotExport, vm.PlotFileNameGet("svg"));
                },
                param => { return vm.IsReady && !vm.IsPlotEmpty(); }
            );

            Log.Write(LogLevel.DEBUG, "InitializeCommands finished");
        }


        private static string? DirGet(string? fs = null)
        {
            if (string.IsNullOrEmpty(fs)) fs = Process.GetCurrentProcess().MainModule?.FileName;
            return Path.GetDirectoryName(fs);
        }



        private static string FileSpecBuild(string ext)
        {
            string? fs = Process.GetCurrentProcess().MainModule?.FileName;
            return DirGet(fs) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(fs) + "." + ext;
        }


        private void ImageFileSaveDialog(string title, Action<string> fnSave, string? fs = null)
        {
            string ext = string.IsNullOrEmpty(fs) ? "png" : Path.GetExtension(fs).TrimStart('.');
            var filter = "PNG files|*.png|SVG files|*.svg|All files|*.*";
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = filter,
                RestoreDirectory = true,
                OverwritePrompt = true,
                DefaultExt = ext,
                Title = title
            };
            if (!string.IsNullOrEmpty(fs)) dlg.FileName = Path.GetFileName(fs);

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    fnSave(dlg.FileName);
                }
                catch (Exception ex)
                {
                    Log.Write(LogLevel.ERROR, "Cannot save image file " + dlg.FileName + ": " + ex.Message);
                }
            }
        }


        private void MediaFilesOpenDialog(string title, bool multiple = false)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Video Files|*.264;*.avi;*.avs;*.h264;*.hevc;*.m2ts;*.mkv;*.mov;*.mp4;*.mpeg;*.mpg;*.mts;*.mxf;*.ts;*.webm|All files|*.*",
                RestoreDirectory = true,
                Multiselect = multiple,
                Title = title
            };

            if (dlg.ShowDialog() == true)
            {
                if (multiple) foreach (var filename in dlg.FileNames) if (!string.IsNullOrEmpty(filename)) vm.FileAdd(filename);
                        else vm.FileAdd(dlg.FileName);
            }
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                vm.OverallProgress?.Show("Getting FFProbe version info");
                vm.VersionInfo?.Load();
                vm.OverallProgress?.Hide();

                vm.OptionsSet(programOptions);
            });
        }


        private void Dispatcher_ShutdownStarted(object? sender, EventArgs e)
        {
            try
            {
                vm.OptionsGet().SaveToSettings();
            }
            catch (Exception ex)
            {
                Log.Write(LogLevel.ERROR, "Cannot save program settings: " + ex.Message);
            }

            Log.Write(LogLevel.DEBUG, "Finished");
            Log.Close();
        }


        // Drag & Drop
        // https://docs.microsoft.com/en-us/dotnet/desktop/wpf/advanced/drag-and-drop-overview
        private void Control_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not ListView || vm.Files.Count < 2) return;
            startPoint = e.GetPosition(null);
        }


        private void Control_MouseMove(object? sender, MouseEventArgs e)
        {
            if (sender is not ListView view) return;

            var diff = startPoint - e.GetPosition(null);
            if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance) return;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // Get the dragged ListViewItem
                var item = ((DependencyObject)e.OriginalSource).FindAncestor<ListViewItem>();
                if (item == null) return;

                // Find the data behind the ListViewItem
                var file = (FileItem)(view.ItemContainerGenerator.ItemFromContainer(item));
                if (file == null) return;

                dragFileSrcIndex = listviewFiles.SelectedIndex;
                DragDrop.DoDragDrop(item, new DataObject("FileItem", file), DragDropEffects.Move);
            }
            else
            {
                // Moved over ListView with LMB released
                dragFileSrcIndex = null;
            }
        }


        private void Control_PreviewDragEnter(object sender, DragEventArgs e)
        {
            if (sender is not ListView view || !e.Data.GetDataPresent("FileItem")) return;

            DropPositionSet(view, e, true);
        }


        private void Control_PreviewDragOver(object? sender, DragEventArgs e)
        {
            if (sender is not ListView view) return;

            if (e.Data.GetDataPresent("FileItem"))
            {
                // Dropping FileItem
                if (dragFileSrcIndex == null) return;
                var dest = DropPositionSet(view, e, true);
                e.Handled = true;
                e.Effects = dest == null || dest.Index == null ? DragDropEffects.None : DragDropEffects.Move;
            }
            else if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Dropping file(s) from Explorer
                e.Handled = true;
                e.Effects = DragDropEffects.Copy;
            }
        }


        private void Control_PreviewDragLeave(object sender, DragEventArgs e)
        {
            if (sender is not ListView view || !e.Data.GetDataPresent("FileItem")) return;

            DropPositionSet(view, e, false);
        }


        private void Control_PreviewDrop(object? sender, DragEventArgs e)
        {
            if (sender is not ListView view) return;

            if (e.Data.GetDataPresent("FileItem"))
            {
                // Dropping FileItem
                if (dragFileSrcIndex == null) return;

                var dest = DropPositionSet(view, e, false);
                if (dest != null && dest.Index != null) _ = vm.FileMove((int)dragFileSrcIndex, (int)dest.Index);
                dragFileSrcIndex = null;
            }
            else if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Dropping file(s) from Explorer
                if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
                {
                    for (int i = 0; i < files.Length; ++i) vm.FileAdd(files[i]);
                }
            }
            return;
        }


        private ListViewItemDropTarget? DropPositionSet(ListView view, DragEventArgs e, bool show)
        {
            if (e.OriginalSource is not DependencyObject source) return null;

            var item = source.FindAncestor<ListViewItem>();
            if (item == null) return null;

            var target = new ListViewItemDropTarget(item);
            bool isBelow = (e.GetPosition(item).Y > (item.ActualHeight / 2));
            if (dragFileSrcIndex != null && view.ItemContainerGenerator.ItemFromContainer(target.Item) is FileItem file)
            {
                var srcIndex = (int)dragFileSrcIndex;
                var destIndex = vm.Files.IndexOf(file) + (isBelow ? 1 : 0);
                target.Index = (destIndex >= 0 && destIndex <= vm.Files.Count && (srcIndex != destIndex && srcIndex != (destIndex - 1))) ? destIndex : null;
            }

            target.Item.SetValue(DragDropHighlighter.IsDroppingAboveProperty, show == true && target.Index != null && !isBelow);
            target.Item.SetValue(DragDropHighlighter.IsDroppingBelowProperty, show == true && target.Index != null &&  isBelow);

            return target;
        }

    }
}
