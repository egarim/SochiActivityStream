namespace Sochi.Navigation.Dialogs;

/// <summary>
/// Implementation of <see cref="IDialogParameters"/>.
/// </summary>
public sealed class DialogParameters : IDialogParameters
{
    private readonly Dictionary<string, object> _parameters = new();

    /// <inheritdoc />
    public void Add(string key, object value)
    {
        ArgumentNullException.ThrowIfNull(key);
        _parameters[key] = value;
    }

    /// <inheritdoc />
    public T? GetValue<T>(string key)
    {
        if (!_parameters.TryGetValue(key, out var value))
            return default;

        if (value is T typedValue)
            return typedValue;

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return default;
        }
    }

    /// <inheritdoc />
    public bool TryGetValue<T>(string key, out T? value)
    {
        if (!_parameters.TryGetValue(key, out var objValue))
        {
            value = default;
            return false;
        }

        if (objValue is T typedValue)
        {
            value = typedValue;
            return true;
        }

        try
        {
            value = (T)Convert.ChangeType(objValue, typeof(T));
            return true;
        }
        catch
        {
            value = default;
            return false;
        }
    }
}
