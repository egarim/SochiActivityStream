namespace Sochi.Navigation.Dialogs;

/// <summary>
/// Implementation of <see cref="IDialogService"/>.
/// </summary>
public sealed class DialogService : IDialogService
{
    private TaskCompletionSource<IDialogResult>? _dialogTaskCompletionSource;

    /// <summary>
    /// Event raised when a dialog should be shown.
    /// </summary>
    public event Action<string, IDialogParameters?>? ShowDialogRequested;

    /// <summary>
    /// Event raised when the current dialog should be hidden.
    /// </summary>
    public event Action? HideDialogRequested;

    /// <inheritdoc />
    public async Task<IDialogResult> ShowDialogAsync(string dialogName, IDialogParameters? parameters = null)
    {
        _dialogTaskCompletionSource = new TaskCompletionSource<IDialogResult>();
        ShowDialogRequested?.Invoke(dialogName, parameters);
        return await _dialogTaskCompletionSource.Task;
    }

    /// <inheritdoc />
    public async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        var parameters = new DialogParameters();
        parameters.Add("Title", title);
        parameters.Add("Message", message);

        var result = await ShowDialogAsync("ConfirmDialog", parameters);
        return result.Result ?? false;
    }

    /// <summary>
    /// Closes the current dialog with the specified result.
    /// Called by dialog host components.
    /// </summary>
    /// <param name="result">The dialog result.</param>
    public void CloseDialog(IDialogResult result)
    {
        _dialogTaskCompletionSource?.TrySetResult(result);
        HideDialogRequested?.Invoke();
    }
}
