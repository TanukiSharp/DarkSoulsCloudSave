using System;
using Avalonia.Controls;
using SteamCloudSave.ViewModels;

namespace SteamCloudSave.Windows;

public partial class MainWindow : Window
{
    private readonly RootViewModel rootViewModel = new();

    public MainWindow()
    {
        InitializeComponent();

        DataContext = rootViewModel;
    }
}
