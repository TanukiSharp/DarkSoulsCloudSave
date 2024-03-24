using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Rendering;
using Avalonia.VisualTree;
using SteamCloudSave.Core;

namespace SteamCloudSave.Services;

public class ClipboardService : IClipboardService
{
    private readonly Lazy<IClipboard> clipboard;

    public ClipboardService()
    {
        if (Application.Current is null)
        {
            throw new InvalidOperationException("Could not get application instance.");
        }

        clipboard = new Lazy<IClipboard>(() => GetTopLevel(Application.Current).Clipboard ?? throw new InvalidOperationException("Could not get clipboard instance."));
    }

    private static TopLevel GetTopLevel(Application application)
    {
        if (application.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow ?? throw new InvalidOperationException("Could not get window instance.");
        }

        if (application.ApplicationLifetime is ISingleViewApplicationLifetime viewApp)
        {
            IRenderRoot? visualRoot = viewApp.MainView?.GetVisualRoot();
            return visualRoot as TopLevel ?? throw new InvalidOperationException("Could not get window root instance.");
        }

        throw new InvalidOperationException("Could not get application lifetime instance."); ;
    }

    public Task<string?> GetTextAsync()
    {
        return clipboard.Value.GetTextAsync();
    }

    public Task SetTextAsync(string? text)
    {
        return clipboard.Value.SetTextAsync(text);
    }
}
