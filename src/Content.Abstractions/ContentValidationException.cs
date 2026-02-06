namespace Content.Abstractions;

/// <summary>
/// Exception thrown when content validation fails.
/// </summary>
public sealed class ContentValidationException : Exception
{
    /// <summary>
    /// The validation error code.
    /// </summary>
    public ContentValidationError Error { get; }

    /// <summary>
    /// The field that failed validation (optional).
    /// </summary>
    public string? Field { get; }

    public ContentValidationException(ContentValidationError error, string? field = null)
        : base($"Content validation failed: {error}" + (field != null ? $" ({field})" : ""))
    {
        Error = error;
        Field = field;
    }
}
