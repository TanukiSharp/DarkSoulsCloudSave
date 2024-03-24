using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using SteamCloudSave.Core;
using SteamCloudSave.Windows.DataModels.Configuration.V1;

namespace SteamCloudSave.Windows.ViewModels;

public class RootViewModel : ViewModelBase
{
    private readonly IList<ICloudStorage> availableCloudStorages = new List<ICloudStorage>
    {
        //new NullCloudStorage(),

        new DropboxExtension.DropboxCloudStorage(
            DropboxConstants.AppKey,
            DropboxConstants.AppSecret
        ),

        new GoogleDriveExtension.GoogleDriveCloudStorage(
            GoogleDriveConstants.ApplicationName,
            GoogleDriveConstants.ClientId,
            GoogleDriveConstants.ClientSecret
        ),
    };

    public IList<CloudStorageViewModel> CloudStorageViewModels { get; }

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

    public ICommand RestoreCommand { get; }
    public ICommand StartGameCommand { get; }
    public ICommand StoreCommand { get; }

    public ICommand CopyVersionCommand { get; }

    private Configuration configuration;

    public SaveDataUtility SaveDataUtility { get; private set; }

    public RootViewModel()
    {
        RestoreCommand = new AnonymousCommand(OnRestore);
        StartGameCommand = new AnonymousCommand(OnStartGame);
        StoreCommand = new AnonymousCommand(OnStore);

        CopyVersionCommand = new AnonymousCommand(OnCopyVersion);

        CloudStorageViewModels = availableCloudStorages
            .Where(x => x is not null)
            .Select(x => new CloudStorageViewModel(x, this))
            .ToList();

        Version v = Assembly.GetEntryAssembly().GetName().Version;
        Version = $"{v.Major}.{v.Minor}.{v.Build}";
    }

    private bool isInitializing;

    public void Initialize()
    {
        isInitializing = true;

        configuration = LoadConfiguration();

        SaveDataUtility = new SaveDataUtility(Constants.SaveDataPath, Constants.GameArchiveMode);

        ConfigureStorageViewModels(configuration);

        IsAutoStore = configuration.AutoStore;
        IsAutoRestore = configuration.AutoRestore;

        StartGameProcessMonitoring();

        isInitializing = false;

        CloudStorageSelectionChanged();
    }

    internal void CloudStorageSelectionChanged()
    {
        if (isInitializing)
            return;

        SaveCloudStorageSelection();
        InitializeCloudStorages();

        if (CheckCloudStorageSelectionValid() == false)
            Status = "Warning: incorrect cloud storage selection";
        else
            Status = null;
    }

    private void InitializeCloudStorages()
    {
        Task.WhenAll(
            CloudStorageViewModels
                .Where(x => x.IsRestoreSource || x.IsStoreTarget)
                .Select(x => x.Initialize())
        );
    }

    private void SaveCloudStorageSelection()
    {
        CloudStorageViewModel restoreCloudStorage = CloudStorageViewModels.FirstOrDefault(x => x.IsRestoreSource);
        configuration.RestoreCloudStorage = restoreCloudStorage?.UniqueId;

        string[] storeCloudStorageIds = CloudStorageViewModels
            .Where(x => x.IsStoreTarget)
            .Select(x => x.UniqueId)
            .ToArray();

        if (storeCloudStorageIds.Length > 0)
            configuration.StoreCloudStorages = storeCloudStorageIds;
        else
            configuration.StoreCloudStorages = null;

        configuration.Save(configuration.SettingsFilePath);
    }

    private bool isCloudStorageSelectionValid;
    public bool IsCloudStorageSelectionValid
    {
        get { return isCloudStorageSelectionValid; }
        private  set { SetValue(ref isCloudStorageSelectionValid, value); }
    }

    public bool CheckCloudStorageSelectionValid()
    {
        return CloudStorageViewModels.Any(x => x.IsStoreTarget) &&
            CloudStorageViewModels.Any(x => x.IsRestoreSource);
    }

    public void Close(CancelEventArgs e)
    {
        string action = null;

        if (IsStoring)
            action = "Store";
        else if (IsRestoring)
            action = "Restore";

        if (action is not null)
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
        public string Name => "Null cloud storage";

        public Task Initialize()
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }

        public async Task<IReadOnlyList<CloudStorageFileInfo>> ListFiles()
        {
            return await Task.FromResult(new List<CloudStorageFileInfo>());
        }

        public Task<Stream> Download(CloudStorageFileInfo fileInfo)
        {
            return Task.FromResult(Stream.Null);
        }

        public Task<bool> Upload(string localFilename, Stream stream)
        {
            return Task.FromResult(true);
        }

        public Task<bool> Delete(CloudStorageFileInfo fileInfo)
        {
            return Task.FromResult(true);
        }

        public Task<bool> DeleteMany(IEnumerable<CloudStorageFileInfo> fileInfo)
        {
            return Task.FromResult(true);
        }
    }

    private static Configuration LoadConfiguration()
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

        if (configuration is null)
        {
            configuration = new Configuration();
            configuration.Save(configurationFilePath);
        }

        return configuration;
    }

    private void SaveConfiguration()
    {
        if (configuration is not null)
        {
            try
            {
                configuration.Save(configuration.SettingsFilePath);
            }
            catch (Exception ex)
            {
                Status = $"Error: {ex.Message}";
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
            Status = $"Failed to copy version to clipboard: {ex.Message}";
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
    private readonly ManualResetEventSlim gameProcessMonitoring = new();

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
                        Status = $"{Constants.GameDisplayName} is running...";
                        GameStarted?.Invoke(this, EventArgs.Empty);
                    });
                }
                else if (processes.Length == 0 && IsRunGameLocked)
                {
                    dispatcher.Invoke(() =>
                    {
                        IsRunGameLocked = false;
                        Status = $"{Constants.GameDisplayName} has stopped...";
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
            Process.Start(Constants.GameSteamUrl);
            await tcs.Task;
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
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

            await SaveDataUtility.BackupLocalSaveData("ds3");

            CloudStorageViewModel restoreSource = CloudStorageViewModels.FirstOrDefault(x => x.IsRestoreSource);

            if (restoreSource is null)
            {
                // Should not be possible.
                Status = "Error: Restore source cloud storage unavailable";
                return;
            }

            await restoreSource.Restore();

            Status = "Restore done";
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
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
            string timestamp = DateTime.UtcNow.ToString(CloudStorageFileInfo.TimestampFormat);

            Task storeFunc(CloudStorageViewModel cloudStorage)
            {
                return cloudStorage.Store(timestamp, configuration.RevisionsToKeep);
            };

            await Task.WhenAll(
                CloudStorageViewModels
                    .Where(x => x.IsStoreTarget)
                    .Select(x => storeFunc(x))
            );

            Status = "Store done";
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
        }
        finally
        {
            IsStoring = false;
        }
    }

    private void ConfigureStorageViewModels(Configuration configuration)
    {
        if (configuration.StoreCloudStorages is not null)
        {
            foreach (CloudStorageViewModel vm in CloudStorageViewModels)
            {
                foreach (string uniqueId in configuration.StoreCloudStorages)
                {
                    if (vm.UniqueId == uniqueId)
                        vm.IsStoreTarget = true;
                }
            }
        }

        foreach (CloudStorageViewModel vm in CloudStorageViewModels)
            vm.IsRestoreSource = vm.UniqueId == configuration.RestoreCloudStorage;
    }

    public static IList<CloudStorageFileInfo> SortFiles(IEnumerable<CloudStorageFileInfo> files)
    {
        return files
            .OrderByDescending(x => x.StoreTimestamp)
            .ToList();
    }
}
