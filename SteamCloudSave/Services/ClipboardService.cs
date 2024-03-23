using System.Windows;
using SteamCloudSave.Core;

namespace SteamCloudSave.Services;

public class ClipboardService : IClipboardService
{
    public string GetText()
    {
        return Clipboard.GetText();
    }
}
