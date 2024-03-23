namespace SteamCloudSave.Core;

/// <summary>
/// The available type of buttons for the message dialog.
/// </summary>
public enum ButtonTypes
{
    /// <summary>
    /// The message box displays an OK button.
    /// </summary>
    OK,
    /// <summary>
    /// The message box displays OK and Cancel buttons.
    /// </summary>
    OKCancel,
    /// <summary>
    /// The message box displays Yes, No, and Cancel buttons.
    /// </summary>
    YesNoCancel,
    /// <summary>
    /// The message box displays Yes and No buttons.
    /// </summary>
    YesNo,
}

/// <summary>
/// The available type of icons for the message dialog.
/// </summary>
public enum IconType
{
    /// <summary>
    /// An information sign.
    /// </summary>
    Information,
    /// <summary>
    /// A warning sign.
    /// </summary>
    Warning,
    /// <summary>
    /// An error sign.
    /// </summary>
    Error,
}

/// <summary>
/// Abstracts a message dialog.
/// </summary>
public interface IMessageBoxService
{
    /// <summary>
    /// Shows a message box on screen.
    /// </summary>
    /// <param name="message">The main message of the dialog box.</param>
    /// <param name="title">The title of the dialog box.</param>
    /// <param name="buttonTypes">The buttons displayed.</param>
    /// <param name="iconType">The icon displayed.</param>
    void Show(string message, string title, ButtonTypes buttonTypes, IconType iconType);
}
