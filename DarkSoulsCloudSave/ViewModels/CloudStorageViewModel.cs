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
        public ICloudStorage CloudStorage { get; }

        private bool isChecked;
        public bool IsChecked
        {
            get { return isChecked; }
            set { SetValue(ref isChecked, value); }
        }

        public string Name => CloudStorage.Name;

        public CloudStorageViewModel(ICloudStorage cloudStorage)
        {
            if (cloudStorage == null)
                throw new ArgumentNullException(nameof(cloudStorage));

            CloudStorage = cloudStorage;
        }
    }
}
