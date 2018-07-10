using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using DarkSoulsCloudSave.Core;
using DarkSoulsCloudSave.ViewModels;

namespace DarkSoulsCloudSave
{
    /// <summary>
    /// Interaction logic for CloudStorageSelector.xaml
    /// </summary>
    public partial class CloudStorageSelectorWindow : Window
    {
        private readonly CloudStorageSelectorViewModel cloudStorageSelector;

        public CloudStorageSelectorWindow(
            IEnumerable<ICloudStorage> allAvailableCloudStorages,
            ICloudStorage restoreCloudStorage,
            IEnumerable<ICloudStorage> storeCloudStorages
        )
        {
            InitializeComponent();

            cloudStorageSelector = new CloudStorageSelectorViewModel(
                allAvailableCloudStorages,
                restoreCloudStorage,
                storeCloudStorages
            );

            DataContext = cloudStorageSelector;
        }
    }
}
