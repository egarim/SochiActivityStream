namespace Sochi.Navigation.Dialogs;

/// <summary>
/// Represents the result returned from a dialog.
/// </summary>
public interface IDialogResult
{
    /// <summary>
    /// Gets the parameters returned by the dialog.
    /// </summary>
    IDialogParameters Parameters { get; }

    /// <summary>
    /// Gets the result of the dialog. true for confirmed, false for cancelled, null for closed.
    /// </summary>
    bool? Result { get; }
}
