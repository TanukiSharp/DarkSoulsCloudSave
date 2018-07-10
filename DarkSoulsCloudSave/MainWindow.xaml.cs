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

            if (IsLoaded)
                OnLoaded();
            else
                Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            OnLoaded();
        }

        private void OnLoaded()
        {
            rootViewModel.Initialize();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (rootViewModel != null)
                rootViewModel.Close(e);
        }
    }
}
