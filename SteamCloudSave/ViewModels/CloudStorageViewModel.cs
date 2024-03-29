using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteamCloudSave.Core;

namespace SteamCloudSave.ViewModels;

public class CloudStorageViewModel : ViewModelBase
{
    private readonly RootViewModel parent;

    public ICloudStorage CloudStorage { get; }

    public string UniqueId { get; }

    private bool isStoreTarget;
    public bool IsStoreTarget
    {
        get { return isStoreTarget; }
        set
        {
            if (SetValue(ref isStoreTarget, value))
            {
                parent.CloudStorageSelectionChanged();
            }
        }
    }

    private bool isRestoreSource;
    public bool IsRestoreSource
    {
        get { return isRestoreSource; }
        set
        {
            if (SetValue(ref isRestoreSource, value))
            {
                parent.CloudStorageSelectionChanged();
            }
        }
    }

    public string Name => CloudStorage.Name;

    public CloudStorageViewModel(ICloudStorage cloudStorage, RootViewModel parent)
    {
        ArgumentNullException.ThrowIfNull(cloudStorage);

        this.parent = parent;

        CloudStorage = cloudStorage;

        UniqueId = MakeUniqueId(cloudStorage);
    }

    private string? status;
    public string? Status
    {
        get { return status; }
        private set { SetValue(ref status, value); }
    }

    private string? detailedStatus;
    public string? DetailedStatus
    {
        get { return detailedStatus; }
        private set { SetValue(ref detailedStatus, value); }
    }

    private bool isInitializing;
    private bool isInitialized;

    public async Task Initialize()
    {
        if (isInitialized || isInitializing)
        {
            return;
        }

        isInitializing = true;

        Status = "Initializing...";
        DetailedStatus = null;

        using var cts = new CancellationTokenSource(Timeouts.InitializeTimeout);

        try
        {
            await CloudStorage.InitializeAsync(cts.Token);

            Status = "Initialized";
            DetailedStatus = null;

            isInitialized = true;
        }
        catch (Exception ex)
        {
            Status = "Initialization failed";
            DetailedStatus = ex.Message;
        }
        finally
        {
            isInitializing = false;
        }
    }

    private bool isRestoring;

    public async Task Restore()
    {
        if (IsRestoreSource == false || isRestoring)
        {
            return;
        }

        isRestoring = true;

        try
        {
            await RestoreInternal();
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
        }
        finally
        {
            isRestoring = false;
        }
    }

    private async Task RestoreInternal()
    {
        Status = "Retrieving save data list...";

        using var listCts = new CancellationTokenSource(Timeouts.ListFilesTimeout);

        var files = await CloudStorage.ListFilesAsync($"/{parent.SaveDataUtility.GameRootDirectoryName}", listCts.Token);

        if (files.Count == 0)
        {
            Status = "No save data";
            return;
        }

        CloudStorageFileInfo fileInfo = RootViewModel.SortFiles(files)[0];

        Status = string.Format("Restoring {0}...", Path.GetFileNameWithoutExtension(fileInfo.LocalFilename));

        using var downloadCts = new CancellationTokenSource(Timeouts.DownloadTimeout);

        using Stream archiveStream = await CloudStorage.DownloadAsync(fileInfo, downloadCts.Token);

        await parent.SaveDataUtility.ExtractSaveDataArchive(archiveStream);

        Status = "Restore done";
    }

    private bool isStoring;

    public async Task Store(string timestamp, int revisionsToKeep)
    {
        if (IsStoreTarget == false || isStoring)
        {
            return;
        }

        isStoring = true;

        try
        {
            await StoreInternal(timestamp, revisionsToKeep);
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
        }
        finally
        {
            isStoring = false;
        }
    }

    private async Task StoreInternal(string timestamp, int revisionsToKeep)
    {
        Status = string.Format("Storing...");

        Stream archiveStream = await parent.SaveDataUtility.GetSaveDataArchive();

        using var cts = new CancellationTokenSource(Timeouts.UploadTimeout);

        await CloudStorage.UploadAsync($"/{parent.SaveDataUtility.GameRootDirectoryName}/{timestamp}.zip", archiveStream, cts.Token);

        Status = "Cleaning up...";

        if (await CleanupOldRemoteSaves(revisionsToKeep) == false)
        {
            Status = "Failed to cleanup";
        }
        else
        {
            Status = "Store done";
        }
    }

    private async Task<bool> CleanupOldRemoteSaves(int revisionsToKeep)
    {
        var cts = new CancellationTokenSource(Timeouts.ListFilesTimeout);

        IList<CloudStorageFileInfo> files = RootViewModel.SortFiles(await CloudStorage.ListFilesAsync($"/{parent.SaveDataUtility.GameRootDirectoryName}", cts.Token));

        if (files.Count > revisionsToKeep)
        {
            IEnumerable<CloudStorageFileInfo> toDeleteFiles = files.Skip(revisionsToKeep);

            return await CloudStorage.DeleteManyAsync(toDeleteFiles, Timeouts.DeleteTimeout, CancellationToken.None);
        }

        return true;
    }

    public static string MakeUniqueId(ICloudStorage cloudStorage)
    {
        ArgumentNullException.ThrowIfNull(cloudStorage);

        Type type = cloudStorage.GetType();

        return type.FullName ?? type.Name;
    }
}
