using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using SteamCloudSave.Windows.ViewModels;

namespace SteamCloudSave.Windows;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly RootViewModel rootViewModel = new();

    public MainWindow()
    {
        InitializeComponent();

        Version v = Assembly.GetEntryAssembly().GetName().Version;
        Title = $"{Constants.GameDisplayName} - Steam Cloud Save - v{v.Major}.{v.Minor}.{v.Build}";

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

        rootViewModel.Close(e);
    }
}
