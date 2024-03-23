using System;
using System.Windows;
using SteamCloudSave.Core;

namespace SteamCloudSave.Services;

internal class MessageBoxService : IMessageBoxService
{
    public void Show(string message, string title, ButtonTypes buttonTypes, IconType iconType)
    {
        MessageBoxButton messageBoxButtons = buttonTypes switch
        {
            ButtonTypes.OK => MessageBoxButton.OK,
            ButtonTypes.OKCancel => MessageBoxButton.OKCancel,
            ButtonTypes.YesNoCancel => MessageBoxButton.YesNoCancel,
            ButtonTypes.YesNo => MessageBoxButton.YesNo,
            _ => throw new ArgumentException(null, nameof(buttonTypes)),
        };

        MessageBoxImage messageBoxImage = iconType switch
        {
            IconType.Information => MessageBoxImage.Information,
            IconType.Warning => MessageBoxImage.Warning,
            IconType.Error => MessageBoxImage.Error,
            _ => throw new ArgumentException(null, nameof(iconType)),
        };

        MessageBox.Show(message, title, messageBoxButtons, messageBoxImage);
    }
}
