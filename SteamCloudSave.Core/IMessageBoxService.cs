using System.Threading.Tasks;

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
    /// The message box displays OK and Abort buttons.
    /// </summary>
    OKAbort,
    /// <summary>
    /// The message box displays Yes, No, and Cancel buttons.
    /// </summary>
    YesNoCancel,
    /// <summary>
    /// The message box displays Yes, No and Abort buttons.
    /// </summary>
    YesNoAbort,
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
/// The button clicked by the user.
/// </summary>
public enum ButtonResult
{
    /// <summary>
    /// The user clicked the OK button.
    /// </summary>
    OK,
    /// <summary>
    /// The user clicked the Yes button.
    /// </summary>
    Yes,
    /// <summary>
    /// The user clicked the No button.
    /// </summary>
    No,
    /// <summary>
    /// The user clicked the Abort button.
    /// </summary>
    Abort,
    /// <summary>
    /// The user clicked the Cancel button.
    /// </summary>
    Cancel,
    /// <summary>
    /// The user clicked no buttons.
    /// </summary>
    None,
};

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
    Task<ButtonResult> ShowAsync(string message, string title, ButtonTypes buttonTypes, IconType iconType);
}
