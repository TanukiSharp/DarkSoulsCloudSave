using System.Threading.Tasks;

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
    Task<string?> GetTextAsync();

    /// <summary>
    /// Sets text to the clipboard.
    /// </summary>
    /// <param name="value">The value to store in the clipboard.</param>
    Task SetTextAsync(string? value);
}
