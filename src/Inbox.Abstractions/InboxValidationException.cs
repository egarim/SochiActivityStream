namespace Inbox.Abstractions;

/// <summary>
/// Exception thrown when inbox validation fails.
/// </summary>
public class InboxValidationException : Exception
{
    /// <summary>
    /// The validation errors that caused this exception.
    /// </summary>
    public IReadOnlyList<InboxValidationError> Errors { get; }

    public InboxValidationException(IReadOnlyList<InboxValidationError> errors)
        : base(FormatMessage(errors))
    {
        Errors = errors;
    }

    public InboxValidationException(string code, string message, string? path = null)
        : this(new[] { new InboxValidationError(code, message, path) })
    {
    }

    private static string FormatMessage(IReadOnlyList<InboxValidationError> errors)
    {
        if (errors.Count == 0)
            return "Validation failed.";
        if (errors.Count == 1)
            return $"Validation failed: {errors[0].Message}";
        return $"Validation failed with {errors.Count} errors: {errors[0].Message}";
    }
}
