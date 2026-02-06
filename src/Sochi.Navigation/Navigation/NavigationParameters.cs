using Microsoft.AspNetCore.WebUtilities;
using System.Collections;

namespace Sochi.Navigation.Navigation;

/// <summary>
/// Implementation of <see cref="INavigationParameters"/> that supports query string parsing.
/// </summary>
public sealed class NavigationParameters : INavigationParameters
{
    private readonly Dictionary<string, object> _parameters = new();

    /// <summary>
    /// Initializes a new empty instance of <see cref="NavigationParameters"/>.
    /// </summary>
    public NavigationParameters() { }

    /// <summary>
    /// Initializes a new instance of <see cref="NavigationParameters"/> by parsing a query string.
    /// </summary>
    /// <param name="queryString">The query string to parse.</param>
    public NavigationParameters(string queryString)
    {
        if (string.IsNullOrWhiteSpace(queryString))
            return;

        // Remove leading '?' if present
        if (queryString.StartsWith('?'))
            queryString = queryString[1..];

        var parsed = QueryHelpers.ParseQuery(queryString);
        foreach (var kvp in parsed)
        {
            _parameters[kvp.Key] = kvp.Value.ToString() ?? string.Empty;
        }
    }

    /// <inheritdoc />
    public void Add(string key, object value)
    {
        ArgumentNullException.ThrowIfNull(key);
        _parameters[key] = value;
    }

    /// <inheritdoc />
    public bool ContainsKey(string key) => _parameters.ContainsKey(key);

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

    /// <inheritdoc />
    public int Count => _parameters.Count;

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _parameters.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Converts the parameters to a query string.
    /// </summary>
    /// <returns>A query string representation of the parameters.</returns>
    public string ToQueryString()
    {
        if (_parameters.Count == 0)
            return string.Empty;

        var queryParams = _parameters.Select(p =>
            $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value?.ToString() ?? "")}");

        return "?" + string.Join("&", queryParams);
    }
}
