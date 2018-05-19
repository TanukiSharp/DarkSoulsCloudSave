using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DarkSoulsCloudSave.DataModels.Configuration.V1;
using DarkSoulsCloudSave.Core;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;
using System.ComponentModel;
using System.Globalization;

namespace DarkSoulsCloudSave.ViewModels
{
    public class RootViewModel : ViewModelBase
    {
        private ICloudStorage cloudStorage;

        public string Version { get; private set; }

        public bool IsStoreLocked
        {
            get { return IsAutoStore || IsStoring; }
        }

        public bool IsRestoreLocked
        {
            get { return IsAutoRestore || IsRestoring; }
        }

        public bool isRunGameLocked;
        public bool IsRunGameLocked
        {
            get { return isRunGameLocked; }
            private set { SetValue(ref isRunGameLocked, value); }
        }

        private bool isAutoStore;
        public bool IsAutoStore
        {
            get { return isAutoStore; }
            set
            {
                if (SetValue(ref isAutoStore, value))
                {
                    configuration.AutoStore = value;
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsStoreLocked)));
                    SaveConfiguration();
                }
            }
        }

        private bool isAutoRestore;
        public bool IsAutoRestore
        {
            get { return isAutoRestore; }
            set
            {
                if (SetValue(ref isAutoRestore, value))
                {
                    configuration.AutoRestore = value;
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsRestoreLocked)));
                    SaveConfiguration();
                }
            }
        }

        private string status;
        public string Status
        {
            get { return status; }
            set { SetValue(ref status, value); }
        }

        public ICommand RestoreCommand { get; private set; }
        public ICommand StartGameCommand { get; private set; }
        public ICommand StoreCommand { get; private set; }

        public ICommand CopyVersionCommand { get; private set; }

        private Configuration configuration;

        public RootViewModel()
        {
            RestoreCommand = new AnonymousCommand(OnRestore);
            StartGameCommand = new AnonymousCommand(OnStartGame);
            StoreCommand = new AnonymousCommand(OnStore);

            CopyVersionCommand = new AnonymousCommand(OnCopyVersion);

            Version v = Assembly.GetEntryAssembly().GetName().Version;
            Version = $"{v.Major}.{v.Minor}.{v.Build}";

            configuration = LoadConfiguration();

            IsAutoStore = configuration.AutoStore;
            IsAutoRestore = configuration.AutoRestore;

            InitializeCloudStorage();

            StartGameProcessMonitoring();
        }

        public void Close(CancelEventArgs e)
        {
            string action = null;

            if (IsStoring)
                action = "Store";
            else if (IsRestoring)
                action = "Restore";

            if (action != null)
            {
                MessageBox.Show(
                    $"{action} operation is underway, closing application is interrupted to avoid possible data corruption",
                    $"{action} underway",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                e.Cancel = true;
                return;
            }

            gameProcessMonitoring.Set();
        }

        private class NullCloudStorage : ICloudStorage
        {
            public Task Initialize()
            {
                return Task.CompletedTask;
            }

            public void Dispose()
            {
            }

            public Task<IEnumerable<CloudStorageFileInfo>> ListFiles()
            {
                return Task.FromResult(Enumerable.Empty<CloudStorageFileInfo>());
            }

            public Task<Stream> Download(string remoteFileIdentifier)
            {
                return Task.FromResult(Stream.Null);
            }

            public Task<bool> Upload(string localFilename, Stream stream)
            {
                return Task.FromResult(true);
            }

            public Task<bool> Delete(string fileIdentifier)
            {
                return Task.FromResult(true);
            }
        }

        private async void InitializeCloudStorage()
        {
            Status = "Initializing cloud storage...";

            try
            {
                cloudStorage = new NullCloudStorage();
                //cloudStorage = new DropboxExtension.DropboxCloudStorage();
                //cloudStorage = new GoogleDriveExtension.GoogleDriveCloudStorage();

                await cloudStorage.Initialize();

                Status = "Cloud storage initialization done";
            }
            catch (Exception ex)
            {
                Status = string.Format(
                    "Failed to initialize {0} ({1})",
                    cloudStorage != null ? cloudStorage.GetType().Name : "cloud storage",
                    ex.Message
                );
            }
        }

        private Configuration LoadConfiguration()
        {
            Configuration configuration = null;

            string configurationFilePath = Path.ChangeExtension(Assembly.GetEntryAssembly().Location, ".config");

            if (File.Exists(configurationFilePath))
            {
                try
                {
                    configuration = Configuration.Load(configurationFilePath);
                }
                catch
                {
                }
            }

            if (configuration == null)
            {
                configuration = new Configuration();
                configuration.Save(configurationFilePath);
            }

            return configuration;
        }

        private void SaveConfiguration()
        {
            if (configuration != null)
            {
                try
                {
                    configuration.Save(configuration.SettingsFilePath);
                }
                catch (Exception ex)
                {
                    Status = "Error: " + ex.Message;
                }
            }
        }

        private async void OnRestore()
        {
            await RunRestoreProcess();
        }

        private async void OnStartGame()
        {
            if (IsAutoRestore)
                await RunRestoreProcess();

            await RunGameProcess();

            if (IsAutoStore)
                await RunStoreProcess();
        }

        private async void OnStore()
        {
            await RunStoreProcess();
        }

        private void OnCopyVersion()
        {
            try
            {
                Status = "Trying to copy version to clipboard...";
                Clipboard.SetText(Version);
                Status = "Version copied to clipboard";
            }
            catch (Exception ex)
            {
                Status = "Failed to copy version to clipboard: " + ex.Message;
            }
        }

        private bool isRestoring;
        public bool IsRestoring
        {
            get { return isRestoring; }
            private set
            {
                if (SetValue(ref isRestoring, value))
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsRestoreLocked)));
            }
        }

        private bool isStoring;
        public bool IsStoring
        {
            get { return isStoring; }
            private set
            {
                if (SetValue(ref isStoring, value))
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsStoreLocked)));
            }
        }

        private bool isGameStarting;
        private ManualResetEventSlim gameProcessMonitoring = new ManualResetEventSlim();

        private event EventHandler GameStarted;
        private event EventHandler GameStopped;

        private void StartGameProcessMonitoring()
        {
            Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                while (gameProcessMonitoring.Wait(1000) == false)
                {
                    bool localIsRunGameLocked = dispatcher.Invoke(() => IsRunGameLocked);

                    Process[] processes = Process.GetProcessesByName(Constants.ProcessName);

                    if (processes.Length > 0 && IsRunGameLocked == false)
                    {
                        dispatcher.Invoke(() =>
                        {
                            IsRunGameLocked = true;
                            Status = "Dark Souls 3 is running...";
                            GameStarted?.Invoke(this, EventArgs.Empty);
                        });
                    }
                    else if (processes.Length == 0 && IsRunGameLocked)
                    {
                        dispatcher.Invoke(() =>
                        {
                            IsRunGameLocked = false;
                            Status = "Dark Souls 3 has stopped...";
                            GameStopped?.Invoke(this, EventArgs.Empty);
                        });
                    }
                }

                gameProcessMonitoring.Reset();
            });
        }

        private async Task RunGameProcess()
        {
            if (isGameStarting || IsRunGameLocked)
                return;

            isGameStarting = true;

            var tcs = new TaskCompletionSource<bool>();

            void onGameStopped(object ss, EventArgs ee) => tcs.TrySetResult(true);

            GameStopped += onGameStopped;

            try
            {
                Process.Start(Constants.SteamUrl);
                await tcs.Task;
            }
            catch (Exception ex)
            {
                Status = "Error: " + ex.Message;
            }
            finally
            {
                GameStopped -= onGameStopped;
                isGameStarting = false;
            }
        }

        private async Task RunRestoreProcess()
        {
            if (IsRestoring)
                return;

            IsRestoring = true;

            try
            {
                Status = "Backing up local save data...";

                await SaveDataUtility.BackupLocalSaveData();

                Status = "Retrieving save data list...";

                IList<IGrouping<DateTime, CloudStorageFileInfo>> fileGroups = GroupArchives(await cloudStorage.ListFiles());

                if (fileGroups.Count == 0)
                {
                    Status = "No save data";
                    return;
                }

                foreach (CloudStorageFileInfo fileInfo in fileGroups[0])
                {
                    Status = string.Format("Restoring {0}...", Path.GetFileNameWithoutExtension(fileInfo.LocalFilename));

                    Stream archiveStream = await cloudStorage.Download(fileInfo.RemoteFileIdentifier);
                    await SaveDataUtility.ExtractSaveDataArchive(archiveStream);
                }

                if (fileGroups.Count > configuration.RevisionsToKeep)
                {
                    Status = "Cleaning up...";

                    IEnumerable<Task<bool>> deleteTasks = fileGroups
                        .Skip(configuration.RevisionsToKeep)
                        .SelectMany(x => x)
                        .Select(x => x.RemoteFileIdentifier)
                        .Select(cloudStorage.Delete)
                        .ToList();

                    await Task.WhenAll(deleteTasks);

                    if (deleteTasks.Any(x => x.Result) == false)
                    {
                        Status = "Error: At least one deletion task failed";
                        return;
                    }
                }

                Status = "Restore done";
            }
            catch (Exception ex)
            {
                Status = "Error: " + ex.Message;
            }
            finally
            {
                IsRestoring = false;
            }
        }

        private async Task RunStoreProcess()
        {
            if (IsStoring)
                return;

            IsStoring = true;

            try
            {
                string timestamp = DateTime.UtcNow.ToString(Constants.TimestampFormat);

                foreach (string directory in Directory.GetDirectories(SaveDataUtility.SaveDataPath, "*", SearchOption.TopDirectoryOnly))
                {
                    string filename = Path.GetFileName(directory) + ".zip";

                    Status = string.Format("Storing {0}...", Path.GetFileNameWithoutExtension(filename));

                    Stream archiveStream = await SaveDataUtility.GetSaveDataArchive(directory);
                    await cloudStorage.Upload($"/{timestamp}_{filename}", archiveStream);
                }

                Status = "Store done";
            }
            catch (Exception ex)
            {
                Status = "Error: " + ex.Message;
            }
            finally
            {
                IsStoring = false;
            }
        }

        private IList<IGrouping<DateTime, CloudStorageFileInfo>> GroupArchives(IEnumerable<CloudStorageFileInfo> files)
        {
            return files
                .Select(ExtractTimestamp)
                .OrderBy(x => x.timestamp)
                .GroupBy(x => x.timestamp, x => x.fileInfo)
                .ToList();
        }

        private (DateTime timestamp, CloudStorageFileInfo fileInfo) ExtractTimestamp(CloudStorageFileInfo fileInfo)
        {
            if (fileInfo.LocalFilename.Length > Constants.TimestampFormat.Length + 1)
            {
                if (DateTime.TryParseExact(fileInfo.LocalFilename.Substring(0, Constants.TimestampFormat.Length), Constants.TimestampFormat, null, DateTimeStyles.None, out DateTime dt))
                    return (dt, fileInfo);
            }
            return (DateTime.MinValue, fileInfo);
        }
    }
}
