namespace SteamCloudSave.Core;

/// <summary>
/// Abstracts a way to access the clipboard.
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// Gets text from the clipboard.
    /// </summary>
    /// <returns>Returns the string content in the clipboard.</returns>
    string GetText();
}
