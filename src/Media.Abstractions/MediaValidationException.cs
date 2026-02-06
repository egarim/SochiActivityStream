namespace Media.Abstractions;

/// <summary>
/// Exception thrown when media validation fails.
/// </summary>
public sealed class MediaValidationException : Exception
{
    /// <summary>
    /// The validation error that occurred.
    /// </summary>
    public MediaValidationError Error { get; }

    /// <summary>
    /// The field that failed validation (if applicable).
    /// </summary>
    public string? Field { get; }

    /// <summary>
    /// Creates a new MediaValidationException.
    /// </summary>
    public MediaValidationException(MediaValidationError error, string? field = null)
        : base($"Media validation failed: {error}" + (field != null ? $" ({field})" : ""))
    {
        Error = error;
        Field = field;
    }
}
