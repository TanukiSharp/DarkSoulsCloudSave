using System;
using System.Windows;
using System.ComponentModel;
using DarkSoulsCloudSave.ViewModels;

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

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (rootViewModel != null)
                rootViewModel.Close(e);
        }
    }
}
