using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using DarkSoulsCloudSave.Core;

namespace DarkSoulsCloudSave.ViewModels
{
    public class CloudStorageSelectorViewModel : ViewModelBase
    {
        public ICommand AcceptCommand { get; }

        public IEnumerable<CloudStorageViewModel> StoreTo { get; }
        public IEnumerable<CloudStorageViewModel> RestoreFrom { get; }

        public CloudStorageSelectorViewModel(
            IEnumerable<ICloudStorage> allAvailableCloudStorages,
            ICloudStorage restoreCloudStorage,
            IEnumerable<ICloudStorage> storeCloudStorages
        )
        {
            AcceptCommand = new AnonymousCommand(OnAccept);

            StoreTo = allAvailableCloudStorages
                .Where(x => x != null)
                .Select(x => new CloudStorageViewModel(x))
                .ToList();

            if (storeCloudStorages != null)
            {
                foreach (CloudStorageViewModel vm in StoreTo)
                {
                    string storeCloudStorageId = vm.CloudStorage.GetType().FullName;

                    foreach (ICloudStorage cs in storeCloudStorages)
                    {
                        if (storeCloudStorageId == cs.GetType().FullName)
                            vm.IsChecked = true;
                    }
                }
            }

            RestoreFrom = allAvailableCloudStorages.Select(x => new CloudStorageViewModel(x)).ToList();

            string restoreCloudStorageId = restoreCloudStorage.GetType().FullName;

            foreach (CloudStorageViewModel vm in RestoreFrom)
            {
                if (vm.CloudStorage.GetType().FullName == restoreCloudStorageId)
                {
                    vm.IsChecked = true;
                    break;
                }
            }
        }

        private void OnAccept()
        {
        }
    }
}
