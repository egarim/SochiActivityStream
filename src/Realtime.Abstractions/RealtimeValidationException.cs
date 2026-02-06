namespace Realtime.Abstractions;

/// <summary>
/// Exception thrown when realtime validation fails.
/// </summary>
public sealed class RealtimeValidationException : Exception
{
    /// <summary>The validation error code.</summary>
    public RealtimeValidationError Error { get; }

    public RealtimeValidationException(RealtimeValidationError error)
        : base($"Realtime validation failed: {error}")
    {
        Error = error;
    }

    public RealtimeValidationException(RealtimeValidationError error, string message)
        : base(message)
    {
        Error = error;
    }
}
