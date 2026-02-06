namespace Identity.Abstractions;

/// <summary>
/// Exception thrown when identity validation fails.
/// </summary>
public class IdentityValidationException : Exception
{
    /// <summary>
    /// The validation errors that caused this exception.
    /// </summary>
    public IReadOnlyList<IdentityValidationError> Errors { get; }

    /// <summary>
    /// Creates a new validation exception with the specified errors.
    /// </summary>
    public IdentityValidationException(IReadOnlyList<IdentityValidationError> errors)
        : base(FormatMessage(errors))
    {
        Errors = errors;
    }

    /// <summary>
    /// Creates a validation exception with a single error.
    /// </summary>
    public IdentityValidationException(string code, string message, string? path = null)
        : this(new[] { new IdentityValidationError(code, message, path) })
    {
    }

    private static string FormatMessage(IReadOnlyList<IdentityValidationError> errors)
    {
        if (errors.Count == 0)
            return "Identity validation failed.";
        if (errors.Count == 1)
            return $"Identity validation failed: {errors[0].Message}";
        return $"Identity validation failed with {errors.Count} errors: {errors[0].Message}";
    }
}
