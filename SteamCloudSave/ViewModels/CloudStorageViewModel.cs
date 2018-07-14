using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamCloudSave.Core;

namespace SteamCloudSave.ViewModels
{
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
                    parent.CloudStorageSelectionChanged();
            }
        }

        private bool isRestoreSource;
        public bool IsRestoreSource
        {
            get { return isRestoreSource; }
            set
            {
                if (SetValue(ref isRestoreSource, value))
                    parent.CloudStorageSelectionChanged();
            }
        }

        public string Name => CloudStorage.Name;

        public CloudStorageViewModel(ICloudStorage cloudStorage, RootViewModel parent)
        {
            if (cloudStorage == null)
                throw new ArgumentNullException(nameof(cloudStorage));

            this.parent = parent;

            CloudStorage = cloudStorage;

            UniqueId = MakeUniqueId(cloudStorage);
        }

        private string status;
        public string Status
        {
            get { return status; }
            private set { SetValue(ref status, value); }
        }

        private string detailedStatus;
        public string DetailedStatus
        {
            get { return detailedStatus; }
            private set { SetValue(ref detailedStatus, value); }
        }

        private bool isInitializing;
        private bool isInitialized;

        public async Task Initialize()
        {
            if (isInitialized || isInitializing)
                return;

            isInitializing = true;

            Status = "Initializing...";
            DetailedStatus = null;

            try
            {
                await CloudStorage.Initialize();

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
                return;

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

            IList<CloudStorageFileInfo> files = RootViewModel.SortFiles(await CloudStorage.ListFiles());

            if (files.Count == 0)
            {
                Status = "No save data";
                return;
            }

            CloudStorageFileInfo fileInfo = files[0];

            Status = string.Format("Restoring {0}...", Path.GetFileNameWithoutExtension(fileInfo.LocalFilename));

            using (Stream archiveStream = await CloudStorage.Download(fileInfo))
                await parent.SaveDataUtility.ExtractSaveDataArchive(archiveStream);

            Status = "Restore done";
        }

        private bool isStoring;

        public async Task Store(string timestamp, string[] directories, int revisionsToKeep)
        {
            if (IsStoreTarget == false || isStoring)
                return;

            isStoring = true;

            try
            {
                await StoreInternal(timestamp, directories, revisionsToKeep);
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

        private async Task StoreInternal(string timestamp, string[] directories, int revisionsToKeep)
        {
            Status = string.Format("Storing...");

            Stream archiveStream = await parent.SaveDataUtility.GetSaveDataArchive();
            await CloudStorage.Upload($"/{timestamp}.zip", archiveStream);

            Status = "Cleaning up...";

            if (await CleanupOldRemoteSaves(revisionsToKeep) == false)
                Status = "Failed to cleanup";
            else
                Status = "Store done";
        }

        private async Task<bool> CleanupOldRemoteSaves(int revisionsToKeep)
        {
            IList<CloudStorageFileInfo> files = RootViewModel.SortFiles(await CloudStorage.ListFiles());

            if (files.Count > revisionsToKeep)
            {
                IEnumerable<CloudStorageFileInfo> toDeleteFiles = files.Skip(revisionsToKeep);

                return await CloudStorage.DeleteMany(toDeleteFiles);
            }

            return true;
        }

        public static string MakeUniqueId(ICloudStorage cloudStorage)
        {
            if (cloudStorage == null)
                throw new ArgumentNullException(nameof(cloudStorage));

            return cloudStorage.GetType().FullName;
        }
    }
}
