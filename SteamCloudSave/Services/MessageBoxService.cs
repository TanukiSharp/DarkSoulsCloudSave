using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Rendering;
using Avalonia.VisualTree;
using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using SteamCloudSave.Core;

namespace SteamCloudSave.Services;

internal class MessageBoxService : IMessageBoxService
{
    public async Task<ButtonResult> ShowAsync(string message, string title, ButtonTypes buttonTypes, IconType iconType)
    {
        MsBox.Avalonia.Enums.ButtonEnum messageBoxButtons = buttonTypes switch
        {
            ButtonTypes.OK => MsBox.Avalonia.Enums.ButtonEnum.Ok,
            ButtonTypes.OKCancel => MsBox.Avalonia.Enums.ButtonEnum.OkCancel,
            ButtonTypes.YesNoCancel => MsBox.Avalonia.Enums.ButtonEnum.YesNoCancel,
            ButtonTypes.YesNo => MsBox.Avalonia.Enums.ButtonEnum.YesNo,
            ButtonTypes.OKAbort => MsBox.Avalonia.Enums.ButtonEnum.OkAbort,
            ButtonTypes.YesNoAbort => MsBox.Avalonia.Enums.ButtonEnum.YesNoAbort,
            _ => throw new ArgumentException(null, nameof(buttonTypes)),
        };

        MsBox.Avalonia.Enums.Icon messageBoxImage = iconType switch
        {
            IconType.Information => MsBox.Avalonia.Enums.Icon.Info,
            IconType.Warning => MsBox.Avalonia.Enums.Icon.Warning,
            IconType.Error => MsBox.Avalonia.Enums.Icon.Error,
            _ => throw new ArgumentException(null, nameof(iconType)),
        };

        IMsBox<MsBox.Avalonia.Enums.ButtonResult> dialogBox = MessageBoxManager.GetMessageBoxStandard(title, message, messageBoxButtons, messageBoxImage);

        MsBox.Avalonia.Enums.ButtonResult result;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow is not null)
        {
            result = await dialogBox.ShowWindowDialogAsync(desktop.MainWindow);
        }
        else if (Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime applicationLifetime)
        {
            IRenderRoot? renderRoot = applicationLifetime.MainView?.GetVisualRoot();
            ContentControl contentControl = renderRoot as ContentControl ?? throw new InvalidOperationException("Could not get window root instance.");
            result = await dialogBox.ShowAsPopupAsync(contentControl);
        }
        else
        {
            result = await dialogBox.ShowWindowAsync();
        }

        return result switch
        {
            MsBox.Avalonia.Enums.ButtonResult.Ok => ButtonResult.OK,
            MsBox.Avalonia.Enums.ButtonResult.Yes => ButtonResult.Yes,
            MsBox.Avalonia.Enums.ButtonResult.No => ButtonResult.No,
            MsBox.Avalonia.Enums.ButtonResult.Abort => ButtonResult.Abort,
            MsBox.Avalonia.Enums.ButtonResult.Cancel => ButtonResult.Cancel,
            MsBox.Avalonia.Enums.ButtonResult.None => ButtonResult.None,
            _ => throw new InvalidOperationException(),
        };
    }
}
