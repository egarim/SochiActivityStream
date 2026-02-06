namespace Sochi.Navigation.Navigation;

/// <summary>
/// Exception thrown during navigation operations.
/// </summary>
public sealed class NavigationException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="NavigationException"/> with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public NavigationException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of <see cref="NavigationException"/> with a message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public NavigationException(string message, Exception innerException)
        : base(message, innerException) { }
}
