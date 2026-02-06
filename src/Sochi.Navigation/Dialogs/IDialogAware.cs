namespace Sochi.Navigation.Dialogs;

/// <summary>
/// Interface for dialog ViewModels.
/// </summary>
public interface IDialogAware
{
    /// <summary>
    /// Called when the dialog is opened.
    /// </summary>
    /// <param name="parameters">The parameters passed to the dialog.</param>
    void OnDialogOpened(IDialogParameters parameters);

    /// <summary>
    /// Determines if the dialog can be closed.
    /// </summary>
    /// <returns>true if the dialog can close; otherwise, false.</returns>
    bool CanCloseDialog();

    /// <summary>
    /// Called when the dialog is closing.
    /// </summary>
    void OnDialogClosed();

    /// <summary>
    /// Event raised to request dialog closure.
    /// </summary>
    event Action<IDialogResult>? RequestClose;
}
