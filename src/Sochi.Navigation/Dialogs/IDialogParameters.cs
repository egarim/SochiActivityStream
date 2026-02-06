namespace Sochi.Navigation.Dialogs;

/// <summary>
/// Represents parameters passed to a dialog.
/// </summary>
public interface IDialogParameters
{
    /// <summary>
    /// Adds a parameter with the specified key and value.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <param name="value">The parameter value.</param>
    void Add(string key, object value);

    /// <summary>
    /// Gets the value associated with the specified key.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The parameter key.</param>
    /// <returns>The value if found and convertible; otherwise, default.</returns>
    T? GetValue<T>(string key);

    /// <summary>
    /// Attempts to get the value associated with the specified key.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The parameter key.</param>
    /// <param name="value">When this method returns, the value if found; otherwise, default.</param>
    /// <returns>true if the key was found and the value was convertible; otherwise, false.</returns>
    bool TryGetValue<T>(string key, out T? value);
}
