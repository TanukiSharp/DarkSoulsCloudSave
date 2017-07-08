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

            var v = Assembly.GetEntryAssembly().GetName().Version;
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

        private async void InitializeCloudStorage()
        {
            Status = "Initializing cloud storage...";

            try
            {
                cloudStorage = new DropboxExtension.DropboxCloudStorage();
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

            var configurationFilePath = Path.ChangeExtension(Assembly.GetEntryAssembly().Location, ".config");

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
            var dispatcher = Dispatcher.CurrentDispatcher;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                while (gameProcessMonitoring.Wait(1000) == false)
                {
                    bool localIsRunGameLocked = dispatcher.Invoke(() => IsRunGameLocked);

                    var processes = Process.GetProcessesByName(Constants.ProcessName);

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

            EventHandler onGameStopped = (ss, ee) => tcs.TrySetResult(true);

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

                string[] files = await cloudStorage.ListFiles();

                if (files.Length == 0)
                {
                    Status = "No save data";
                    return;
                }

                foreach (string filename in files)
                {
                    Status = string.Format("Restoring {0}...", Path.GetFileNameWithoutExtension(filename));

                    Stream archiveStream = await cloudStorage.Download(filename);
                    await SaveDataUtility.ExtractSaveDataArchive(archiveStream);
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
                foreach (string directory in Directory.GetDirectories(SaveDataUtility.SaveDataPath, "*", SearchOption.TopDirectoryOnly))
                {
                    var filename = Path.GetFileName(directory) + ".zip";

                    Status = string.Format("Storing {0}...", Path.GetFileNameWithoutExtension(filename));

                    Stream archiveStream = await SaveDataUtility.GetSaveDataArchive(directory);
                    await cloudStorage.Upload("/" + filename, archiveStream);
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
    }
}
