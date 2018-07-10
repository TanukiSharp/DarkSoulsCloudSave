using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DarkSoulsCloudSave.Core;

namespace DarkSoulsCloudSave.ViewModels
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

        public static string MakeUniqueId(ICloudStorage cloudStorage)
        {
            if (cloudStorage == null)
                throw new ArgumentNullException(nameof(cloudStorage));

            return cloudStorage.GetType().FullName;
        }
    }
}
