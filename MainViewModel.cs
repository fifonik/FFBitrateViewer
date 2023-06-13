using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;
using System.Threading.Channels;
using System.Diagnostics;
using System.Windows;

namespace FFBitrateViewer
{

    public class FilesMediaInfoQueueItem
    {
        public FileItem File { get; set; }
        public FilesMediaInfoQueueItem(FileItem file)
        {
            File = file;
        }
    }



    public class MainViewModel : Bindable
    {
        public CancellationTokenSource?                               cancellationTokenSource;
        private readonly Channel<bool>                                filesMediaInfoNotificationChannel = Channel.CreateUnbounded<bool>();
        private readonly ConcurrentQueue<FilesMediaInfoQueueItem>     filesMediaInfoQueue = new();
        private Task?                                                 filesBitRateInfoGetTask;

        public VersionInfo? VersionInfo { get { return Get<VersionInfo>();     }    private set { Set(value); } }
        public ObservableCollection<FileItem> Files                   { get;                                      private set; }
        public bool                           IsAdjustStartTimeOnPlot { get { return Get<bool>();            }    set { Set(value); IsAdjustOffsetOnPlotUpdated(); } }
        public bool                           IsAutoRun               { get;                                      set; }
        public bool                           IsMediaInfoGetRunning   { get { return Get<bool>();            }    private set { Set(value); } }
        public bool                           IsLoggingFFCommands     { get { return IsLogCommands || Log.LogLevelIs(LogLevel.DEBUG); } set { if(!Log.LogLevelIs(LogLevel.DEBUG)) IsLogCommands = value; } }
        public bool                           IsLogCommandsEnabled    { get { return !Log.LogLevelIs(LogLevel.DEBUG); } }
        public bool                           IsReady                 { get { return Get<bool>();            }    private set { Set(value); } } // true when program initialisation is finished
        public bool                           IsRunning               { get { return Get<bool>();            }    private set { Set(value); } }

        public bool                           IsLogCommands           { get { return Get<bool>();            }    set { Set(value); Log.IsLogCommands = value; OnPropertyChanged(nameof(IsLoggingFFCommands)); } }
        public Action?                        AfterGettingMediaInfo   { get;                                      private set; }
        public OverallProgress?               OverallProgress         { get { return Get<OverallProgress>(); }    set { Set(value); } }
        public MyPlotModel?                   PlotModel               { get { return Get<MyPlotModel?>();    }    private set { Set(value); } }
        public string?                        PlotViewType            { get { return Get<string>();          }    set { Set(value); PlotViewTypeUpdated(); } }
        public string                         WindowTitle             { get { return WindowTitleGet();       } }

        // Commands
        public RelayCommand?    AboutShowCmd                        { get; set; }
        public RelayCommand?    ExecStartCmd                        { get; set; }
        public RelayCommand?    ExecStopCmd                         { get; set; }
        public RelayCommand?    ExecToggleCmd                       { get; set; }
        public RelayCommand?    ExitCmd                             { get; set; }
        public RelayCommand?    FilesAddCmd                         { get; set; }
        public RelayCommand?    FilesClearCmd                       { get; set; }
        public RelayCommand?    FilesRemoveCmd                      { get; set; }
        public RelayCommand?    MediaInfoReloadCmd                  { get; set; }
        public RelayCommand?    PlotExportToClipboardCmd            { get; set; }
        public RelayCommand?    PlotExportToFileCmd                 { get; set; }


        public MainViewModel()
        {
            VersionInfo             = new();
            OverallProgress         = new();

            Files                   = new();
            Files.CollectionChanged += OnFilesCollectionChanged;

            PlotModel = new MyPlotModel("");

            PlotViewType = "frame";

            AfterGettingMediaInfo = new(() => {
                if (IsAutoRun)
                {
                    IsAutoRun = false;
                    if (ExecStartCmd?.CanExecute(null) == true) ExecStartCmd?.Execute(null);
                }
            });

            Task.Run(FilesMediaInfoChannelReaderAsync);
        }


        private async void FilesMediaInfoChannelReaderAsync()
        {
            await foreach (var x in filesMediaInfoNotificationChannel.Reader.ReadAllAsync())
            {
                Debug.WriteLine("FilesMediaInfoChannelReaderAsync: NOTIFICATION RECEIVED");
                await Task.Run(FilesMediaInfoGet);
            }
            Debug.WriteLine("FilesMediaInfoChannelReaderAsync: finished");
        }


        private void IsAdjustOffsetOnPlotUpdated()
        {
            foreach (var file in Files) file.FramesIsAdjustStartTimeSet(IsAdjustStartTimeOnPlot);
            PlotUpdate();
        }


        public bool IsFileCanBeAdded(string? fs = null)
        {
            return (Files.Count < MyPlotModel.PlotStyles.Count && (string.IsNullOrEmpty(fs) || !IsFileOnList(fs)));
        }


        public bool IsFileOnList(string fs)
        {
            foreach(var file in Files) if (fs.Equals(file.FS)) return true;
            return false;
        }


        public bool IsFileOnList(FileItem file)
        {
            return Files.IndexOf(file) >= 0;
        }


        public bool IsFilesReady()
        {
            int enabledCount = FilesCountExistAndEnable();
            return enabledCount > 0 && enabledCount == FilesCountExistAndEnableAndReady();
        }


        public bool IsOptionsReady()
        {
            return IsFilesReady();
        }


        public bool IsPlotEmpty()
        {
            return PlotModel?.IsEmpty() ?? true;
        }


        public bool FileAdd(string fs, bool enabled = true)
        {
            if (!IsFileCanBeAdded(fs)) return false;

            var file = new FileItem(fs, enabled);
            file.FramesIsAdjustStartTimeSet(IsAdjustStartTimeOnPlot);

            Files.Add(file);

            int idx = Files.Count - 1;
            PlotModel?.SerieSet(null, PlotModel.SerieCreate(file, idx));
            PlotModel?.Redraw();

            return true;
        }


        public bool FileMove(int src, int dest)
        {
            if (src < 0 || src >= Files.Count || dest < 0 || dest > Files.Count || src == dest || src == dest - 1) return false;

            Files.Move(src, src < dest ? dest - 1 : dest);

            var temp = PlotModel?.SerieGet(src);

            if (temp != null)
            {
                if (src < dest)
                {
                    for (var idx = src; idx < dest - 1; ++idx) PlotModel?.SerieSet(idx, PlotModel.SerieGet(idx + 1));
                    PlotModel?.SerieSet(dest - 1, temp);
                }
                else
                {
                    for (var idx = src; idx > dest; --idx) PlotModel?.SerieSet(idx, PlotModel.SerieGet(idx - 1));
                    PlotModel?.SerieSet(dest, temp);
                }
                PlotModel?.Redraw();
            }

            return true;
        }


        public bool FileRemove(FileItem file)
        {
            return FileRemove(Files.IndexOf(file));
        }


        public bool FileRemove(int fileIndex)
        {
            if (fileIndex < 0 || fileIndex >= Files.Count) return false;

            PlotModel?.Series.RemoveAt(fileIndex);
            for (int serieIndex = fileIndex; serieIndex < PlotModel?.Series.Count; ++serieIndex) PlotModel?.SerieStyleApply(serieIndex);

            Files.RemoveAt(fileIndex);

            PlotAxesUpdate(true/*force*/);
            PlotModel?.Redraw();
            return true;
        }


        public void FilesClear()
        {
            for (int idx = Files.Count - 1; idx >= 0; --idx) FileRemove(idx);
        }


        public int FilesCountExistAndEnable()
        {
            int n = 0;
            foreach (var file in Files) if (file.IsExistsAndEnabled) ++n;
            return n;
        }


        public int FilesCountExistAndEnableAndReady()
        {
            int n = 0;
            foreach (var file in Files) if (file.IsReady) ++n;
            return n;
        }


        public void FilesMediaInfoGet()
        {
            if (VersionInfo?.IsOK != true) return;
            IsMediaInfoGetRunning = true;
            OverallProgress?.Show("Getting files media info");
            int updated = 0;
            while (filesMediaInfoQueue.TryDequeue(out FilesMediaInfoQueueItem? item))
            {
                if (item != null && IsFileOnList(item.File))
                {
                    item.File.MediaInfoGet();
                    ++updated;
                }
            }
            OverallProgress?.Hide();
            
            AfterGettingMediaInfo?.Invoke();
            if (updated > 0) PlotAxesUpdate();

            RefreshUI();

            IsMediaInfoGetRunning = false;
        }


        public async void FilesProcess()
        {
            if (filesBitRateInfoGetTask != null && filesBitRateInfoGetTask.Status == TaskStatus.Running) return;
            IsReady   = false;
            IsRunning = true;
            OverallProgress?.Show("Processing files");
            cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            try
            {
                await Task.Run(() =>
                {
                    //var processor = new SynchronousProcessor();
                    for (int idx = 0; idx < Files.Count; ++idx)
                    {
                        var file = Files[idx];
                        if (!file.IsExistsAndEnabled) continue;
                        file.FramesClear();
                        PlotSerieClear(idx);
                        file.FramesGet(cancellationToken, (pos, frame) => {});

                        if (PlotModel != null)
                        {
                            PlotModel.SeriePointsAdd(idx, file.FramesDataPointsGet(PlotViewType));
                            PlotModel.AxisMaximumSet(0, file.FramesDurationGet());
                            PlotModel.AxisMaximumSet(1, file.FramesMaxYGet(PlotViewType));
                            PlotModel.Redraw();
                        }

                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }, cancellationToken);
            }
            catch(Exception ex)
            {
                // todo@
                Debug.WriteLine("exception", ex.ToString());
            }
            finally
            {
            }
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;
            OverallProgress?.Hide();
            OverallProgress?.Reset();
            OverallProgress?.Stop();
            IsRunning = false;
            IsReady   = true;
        }


        public bool FilesProcessCancel()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                return true;
            }
            return false;
        }


        public void MediaInfoGetStart()
        {
            Debug.WriteLine("NOTIFICATION SENT");
            filesMediaInfoNotificationChannel.Writer.WriteAsync(true);
        }


        private void MediaInfoQueueAddFile(FileItem file)
        {
            if (file.MediaInfo?.IsFilled == true) return;
            Debug.WriteLine("FILE ADDED TO QUEUE - " + file.FS);
            filesMediaInfoQueue.Enqueue(new FilesMediaInfoQueueItem(file));
            MediaInfoGetStart();
        }


        private void MediaInfoQueueClear()
        {
            filesMediaInfoQueue.Clear();
        }


        public void MediaInfoRefresh()
        {
            MediaInfoQueueClear();

            foreach (var file in Files)
            {
                file.MediaInfoClear();
                MediaInfoQueueAddFile(file);
            }
        }


        private void OnFilesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (FileItem newItem in e.NewItems)
                {
                    MediaInfoQueueAddFile(newItem);
                    newItem.PropertyChanged += OnFileItemPropertyChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (FileItem oldItem in e.OldItems) oldItem.PropertyChanged -= OnFileItemPropertyChanged;
            }
        }


        private void OnFileItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is FileItem file)
            {
                switch (e.PropertyName)
                {
                    case "IsEnabled":
                    case "FS": // FS should never updated as we are not modifying filespec for existing file (deleting/adding new file instead)

                        int idx = Files.IndexOf(file);
                        if (idx >= 0)
                        {
                            PlotAxesUpdate(true/*force*/);
                            PlotModel?.SerieRedraw(idx, file.IsEnabled);
                        }

                        break;
                }
            }
        }


        public ProgramOptions OptionsGet()
        {
            var result = new ProgramOptions
            {
                AdjustStartTimeOnPlot = IsAdjustStartTimeOnPlot,
                LogCommands           = IsLogCommands,
            };
            foreach (var file in Files) result.Files.Add(new FileItemPO(file));
            return result;
        }


        public void OptionsSet(ProgramOptions? options = null)
        {
            OverallProgress?.Show("Assigning program options");

            options ??= new ProgramOptions();

            IsAdjustStartTimeOnPlot = options.AdjustStartTimeOnPlot == true;
            IsLogCommands           = options.LogCommands == true;

            FilesClear();
            foreach (var file in options.Files) if (!string.IsNullOrEmpty(file.FS)) FileAdd(file.FS, file.IsEnabled);

            OverallProgress?.Hide();
            IsReady = true;
        }


        public void PlotAxesUpdate(bool force = false) {
            if (PlotModel == null) return;
            if (force)
            {
                PlotModel.AxisMaximumSet(0);
                PlotModel.AxisMaximumSet(1);
                PlotModel.AxesRedraw();
            }
            double maxX = -1;
            int    maxY = -1;
            foreach (var file in Files)
            {
                if (!file.IsExistsAndEnabled) continue;
                var x = file.GetDuration();
                if (x != null && x > maxX) maxX = (double)x;
                int? y = file.FramesMaxYGet(PlotViewType);
                if (y != null && y > maxY) maxY = (int)y;
            }
            if (maxX > 0 && PlotModel.AxisMaximumSet(0, maxX)) PlotModel.AxisRedraw(0);
            if (maxY > 0 && PlotModel.AxisMaximumSet(1, maxY)) PlotModel.AxisRedraw(1);
        }


        public void PlotExport(string? fs = null)
        {
            if(PlotModel == null) return;
            if (string.IsNullOrEmpty(fs))
            {
                // To clipboard
                Clipboard.SetImage(((OxyPlot.Wpf.PngExporter)MyPlotModel.GetExporter())?.ExportToBitmap(PlotModel));
            }
            else
            {
                // To file
                using var stream = File.Create(fs);
                MyPlotModel.GetExporter(Path.GetExtension(fs).TrimStart('.')).Export(PlotModel, stream);
            }
        }


        public string PlotFileNameGet(string ext)
        {
            return (PlotViewType ?? "") + '.' + ext.ToLower();
        }


        public bool PlotSerieAddPoint(int fileIndex, double x, int y, int? pos = null)
        {
            return PlotModel?.SeriePointAdd(fileIndex, x, y, pos) == true;
        }


        public void PlotSerieClear(int fileIndex)
        {
            PlotModel?.SeriePointsClear(fileIndex);
        }


        public void PlotUpdate()
        {
            if(PlotModel == null) return;

            PlotModel.AxisMaximumSet(0);
            PlotModel.AxisMaximumSet(1);

            double maxX = -1;
            int    maxY = -1;
            for (int idx = 0; idx < Files.Count; ++idx){
                var file = Files[idx];
                if (!file.IsExistsAndEnabled) continue;

                PlotSerieClear(idx);

                var x = file.GetDuration();
                if (x != null && x > maxX) maxX = (double)x;
                int? y = file.FramesMaxYGet(PlotViewType);
                if (y != null && y > maxY) maxY = (int)y;

                PlotModel.SeriePointsAdd(idx, file.FramesDataPointsGet(PlotViewType));
            }

            if (maxX > 0 && PlotModel.AxisMaximumSet(0, maxX)) PlotModel.AxisRedraw(0);
            if (maxY > 0 && PlotModel.AxisMaximumSet(1, maxY)) PlotModel.AxisRedraw(1);

            PlotModel.Redraw();
        }


        private void PlotViewTypeUpdated()
        {
            if (PlotModel == null || PlotViewType == null) return;
            PlotModel.PlotViewTypeSet(PlotViewType);
            PlotModel.AxisYTitleAndUnitSet(PlotViewType);
            PlotUpdate();
        }


        public static void RefreshUI()
        {
            // This makes the UI refresh against the RelayCommand.CanExecute() function.
            // Essentially, this makes the button enable when calculating is done.
            CommandManager.InvalidateRequerySuggested();

            Application.Current.Dispatcher.Invoke(CommandManager.InvalidateRequerySuggested);
        }


        public string WindowTitleGet()
        {
            return ProgramInfo.Name + " v" + ProgramInfo.Version;
        }


    }
}
