using System;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SteamCloudSave.Services;
using SteamCloudSave.ViewModels;

namespace SteamCloudSave.Windows;

public partial class MainWindow : Window
{
    private readonly RootViewModel rootViewModel;

    public MainWindow()
    {
        InitializeComponent();

        SetupTitle();

        rootViewModel = new RootViewModel(
            new ClipboardService(),
            new MessageBoxService()
        );

        DataContext = rootViewModel;

        if (IsLoaded)
            OnLoaded();
        else
            Loaded += MainWindow_Loaded;
    }

    private void SetupTitle()
    {
        string version;

        if (Assembly.GetEntryAssembly()?.GetName()?.Version is Version v)
        {
            version = $"v{v.Major}.{v.Minor}.{v.Build}";
        }
        else
        {
            version = "<unknown version>";
        }

        Title = $"{Constants.GameDisplayName} - Steam Cloud Save - {version}";
    }

    private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        OnLoaded();
    }

    private void OnLoaded()
    {
        rootViewModel.Initialize();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

        _ = rootViewModel.Close(e);
    }
}
