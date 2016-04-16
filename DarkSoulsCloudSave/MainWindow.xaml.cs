using DarkSoulsCloudSave.ViewModels;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DarkSoulsCloudSave
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private RootViewModel rootViewModel = new RootViewModel();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = rootViewModel;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            if (rootViewModel != null)
                rootViewModel.Close();
        }
    }
}
