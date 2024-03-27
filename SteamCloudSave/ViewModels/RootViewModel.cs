using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using ReactiveUI;
using SteamCloudSave.Core;
using SteamCloudSave.DataModels.Configuration.Configuration.V1;

namespace SteamCloudSave.ViewModels;

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

    private bool isGameStarting;
    public bool IsGameStarting
    {
        get { return isGameStarting; }
        private set { SetValue(ref isGameStarting, value); }
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
                OnPropertyChanged(nameof(IsStoreLocked));
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
                OnPropertyChanged(nameof(IsRestoreLocked));
                SaveConfiguration();
            }
        }
    }

    private string? status = null!;
    public string? Status
    {
        get { return status; }
        set { SetValue(ref status, value); }
    }

    public ICommand RestoreCommand { get; }
    public ICommand StartGameCommand { get; }
    public ICommand StoreCommand { get; }

    public ICommand CopyVersionCommand { get; }

    private Configuration configuration = null!;

    public SaveDataUtility SaveDataUtility { get; private set; } = null!;

    public RootViewModel(IClipboardService clipboardService, IMessageBoxService messageBoxService)
    {
        RestoreCommand = ReactiveCommand.Create(OnRestore);
        StartGameCommand = ReactiveCommand.Create(OnStartGame);
        StoreCommand = ReactiveCommand.Create(OnStore);

        CopyVersionCommand = ReactiveCommand.Create(OnCopyVersion);

        CloudStorageViewModels = availableCloudStorages
            .Where(x => x is not null)
            .Select(x => new CloudStorageViewModel(x, this))
            .ToList();

        if (Assembly.GetEntryAssembly()?.GetName()?.Version is Version v)
        {
            Version = $"{v.Major}.{v.Minor}.{v.Build}";
        }
        else
        {
            Version = "<unknown version>";
        }

        this.clipboardService = clipboardService;
        this.messageBoxService = messageBoxService;
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
        {
            return;
        }

        SaveCloudStorageSelection();
        InitializeCloudStorages();

        if (CheckCloudStorageSelectionValid() == false)
        {
            Status = "Warning: incorrect cloud storage selection";
        }
        else
        {
            Status = null;
        }
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
        CloudStorageViewModel? restoreCloudStorage = CloudStorageViewModels.FirstOrDefault(x => x.IsRestoreSource);
        configuration.RestoreCloudStorage = restoreCloudStorage?.UniqueId;

        string[] storeCloudStorageIds = CloudStorageViewModels
            .Where(x => x.IsStoreTarget)
            .Select(x => x.UniqueId)
            .ToArray();

        if (storeCloudStorageIds.Length > 0)
        {
            configuration.StoreCloudStorages = storeCloudStorageIds;
        }
        else
        {
            configuration.StoreCloudStorages = null;
        }

        configuration.Save(configuration.SettingsFilePath);
    }

    private bool isCloudStorageSelectionValid;
    public bool IsCloudStorageSelectionValid
    {
        get { return isCloudStorageSelectionValid; }
        private set { SetValue(ref isCloudStorageSelectionValid, value); }
    }

    public bool CheckCloudStorageSelectionValid()
    {
        return CloudStorageViewModels.Any(x => x.IsStoreTarget) &&
            CloudStorageViewModels.Any(x => x.IsRestoreSource);
    }

    public async Task Close(CancelEventArgs e)
    {
        string? action = null;

        if (IsStoring)
        {
            action = "Store";
        }
        else if (IsRestoring)
        {
            action = "Restore";
        }

        if (action is not null)
        {
            e.Cancel = true;
            await messageBoxService.ShowAsync(
                $"{action} operation is underway, closing application is interrupted to avoid possible data corruption",
                $"{action} underway",
                ButtonTypes.OK,
                IconType.Warning
            );
            return;
        }

        gameProcessMonitoring.Set();
    }

    private class NullCloudStorage : ICloudStorage
    {
        public string Name => "Null cloud storage";

        public Task InitializeAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public async Task<IReadOnlyList<CloudStorageFileInfo>> ListFilesAsync(string path, CancellationToken cancellationToken)
        {
            return await Task.FromResult(new List<CloudStorageFileInfo>());
        }

        public Task<Stream> DownloadAsync(CloudStorageFileInfo fileInfo, CancellationToken cancellationToken)
        {
            return Task.FromResult(Stream.Null);
        }

        public Task<bool> UploadAsync(string localFilename, Stream stream, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(CloudStorageFileInfo fileInfo, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task<bool> DeleteManyAsync(IEnumerable<CloudStorageFileInfo> fileInfo, TimeSpan perFileTimeout, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }

    private static Configuration LoadConfiguration()
    {
        Configuration? configuration = null;

        string? configurationFilePath = Path.ChangeExtension(AppContext.BaseDirectory, ".config");

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
        if (configuration is null)
        {
            return;
        }

        try
        {
            configuration.Save(configuration.SettingsFilePath);
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
        }
    }

    private async void OnRestore()
    {
        await RunRestoreProcess();
    }

    private async void OnStartGame()
    {
        if (IsAutoRestore)
        {
            await RunRestoreProcess();
        }

        await RunGameProcess();

        if (IsAutoStore)
        {
            await RunStoreProcess();
        }
    }

    private async void OnStore()
    {
        await RunStoreProcess();
    }

    private async Task OnCopyVersion()
    {
        try
        {
            Status = "Trying to copy version to clipboard...";
            await clipboardService.SetTextAsync(Version);
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
            {
                OnPropertyChanged(nameof(IsRestoreLocked));
            }
        }
    }

    private bool isStoring;
    public bool IsStoring
    {
        get { return isStoring; }
        private set
        {
            if (SetValue(ref isStoring, value))
            {
                OnPropertyChanged(nameof(IsStoreLocked));
            }
        }
    }

    private readonly IClipboardService clipboardService;
    private readonly IMessageBoxService messageBoxService;

    private readonly ManualResetEventSlim gameProcessMonitoring = new();

    private event EventHandler GameStarted = null!;
    private event EventHandler GameStopped = null!;

    private void StartGameProcessMonitoring()
    {
        Dispatcher dispatcher = Dispatcher.UIThread;

        ThreadPool.QueueUserWorkItem(_ =>
        {
            while (gameProcessMonitoring.Wait(1000) == false)
            {
                bool localIsRunGameLocked = dispatcher.Invoke(() => IsRunGameLocked);

                Process[] processes = Process.GetProcessesByName(Constants.ProcessName);

                if (processes.Length > 0 && localIsRunGameLocked == false)
                {
                    dispatcher.Invoke(() =>
                    {
                        IsRunGameLocked = true;
                        Status = $"{Constants.GameDisplayName} is running...";
                        GameStarted?.Invoke(this, EventArgs.Empty);
                    });
                }
                else if (processes.Length == 0 && localIsRunGameLocked)
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
        if (IsGameStarting || IsRunGameLocked)
        {
            return;
        }

        IsGameStarting = true;

        var started = new TaskCompletionSource<bool>();
        var stopped = new TaskCompletionSource<bool>();

        var timeout = TimeSpan.FromSeconds(10.0);

        using var startedTimeout = new CancellationTokenSource(timeout);
        startedTimeout.Token.Register(() => started.TrySetCanceled());

        void onGameStarted(object? ss, EventArgs ee) => started.TrySetResult(true);
        void onGameStopped(object? ss, EventArgs ee) => stopped.TrySetResult(true);

        GameStarted += onGameStarted;
        GameStopped += onGameStopped;

        try
        {
            Process.Start(new ProcessStartInfo(Constants.GameSteamUrl) { UseShellExecute = true });

            Status = "Waiting for game to start...";

            await started.Task;
            await stopped.Task;
        }
        catch (TaskCanceledException)
        {
            Status = $"Error: The game didn't start within {timeout.TotalSeconds} seconds.";
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
        }
        finally
        {
            GameStarted -= onGameStarted;
            GameStopped -= onGameStopped;

            IsGameStarting = false;
        }
    }

    private async Task RunRestoreProcess()
    {
        if (IsRestoring)
        {
            return;
        }

        IsRestoring = true;

        try
        {
            Status = "Backing up local save data...";

            await SaveDataUtility.BackupLocalSaveData();

            CloudStorageViewModel? restoreSource = CloudStorageViewModels.FirstOrDefault(x => x.IsRestoreSource);

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
        {
            return;
        }

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
                    {
                        vm.IsStoreTarget = true;
                    }
                }
            }
        }

        foreach (CloudStorageViewModel vm in CloudStorageViewModels)
        {
            vm.IsRestoreSource = vm.UniqueId == configuration.RestoreCloudStorage;
        }
    }

    public static IList<CloudStorageFileInfo> SortFiles(IEnumerable<CloudStorageFileInfo> files)
    {
        return files
            .OrderByDescending(x => x.StoreTimestamp)
            .ToList();
    }
}
