namespace Sochi.Navigation.Dialogs;

/// <summary>
/// Implementation of <see cref="IDialogResult"/>.
/// </summary>
public sealed class DialogResult : IDialogResult
{
    /// <inheritdoc />
    public IDialogParameters Parameters { get; init; } = new DialogParameters();

    /// <inheritdoc />
    public bool? Result { get; init; }

    /// <summary>
    /// Creates a successful dialog result.
    /// </summary>
    /// <param name="parameters">Optional parameters to include in the result.</param>
    public static DialogResult Ok(IDialogParameters? parameters = null) =>
        new() { Result = true, Parameters = parameters ?? new DialogParameters() };

    /// <summary>
    /// Creates a cancelled dialog result.
    /// </summary>
    public static DialogResult Cancel() =>
        new() { Result = false };
}
