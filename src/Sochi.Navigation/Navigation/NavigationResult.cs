namespace Sochi.Navigation.Navigation;

/// <summary>
/// Represents the result of a navigation operation.
/// </summary>
public sealed class NavigationResult
{
    /// <summary>
    /// Gets a value indicating whether the navigation was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the exception that occurred during navigation, if any.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Creates a successful navigation result.
    /// </summary>
    public static NavigationResult CreateSuccess() => new() { Success = true };

    /// <summary>
    /// Creates a failed navigation result with the specified exception.
    /// </summary>
    /// <param name="exception">The exception that caused the failure.</param>
    public static NavigationResult CreateFailure(Exception exception) =>
        new() { Success = false, Exception = exception };
}
