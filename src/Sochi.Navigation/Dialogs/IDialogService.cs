namespace Sochi.Navigation.Dialogs;

/// <summary>
/// Service for displaying dialogs.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows a dialog with the specified name.
    /// </summary>
    /// <param name="dialogName">The name of the dialog to show.</param>
    /// <param name="parameters">Optional parameters to pass to the dialog.</param>
    /// <returns>The dialog result.</returns>
    Task<IDialogResult> ShowDialogAsync(string dialogName, IDialogParameters? parameters = null);

    /// <summary>
    /// Shows a confirmation dialog with the specified title and message.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The confirmation message.</param>
    /// <returns>true if confirmed; otherwise, false.</returns>
    Task<bool> ShowConfirmationAsync(string title, string message);
}
