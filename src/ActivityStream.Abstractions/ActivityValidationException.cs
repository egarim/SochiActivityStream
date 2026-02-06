namespace ActivityStream.Abstractions;

/// <summary>
/// Exception thrown when activity validation fails.
/// </summary>
public class ActivityValidationException : Exception
{
    /// <summary>
    /// The validation errors.
    /// </summary>
    public IReadOnlyList<ActivityValidationError> Errors { get; }

    public ActivityValidationException(IReadOnlyList<ActivityValidationError> errors)
        : base(FormatMessage(errors))
    {
        Errors = errors;
    }

    public ActivityValidationException(IReadOnlyList<ActivityValidationError> errors, Exception innerException)
        : base(FormatMessage(errors), innerException)
    {
        Errors = errors;
    }

    private static string FormatMessage(IReadOnlyList<ActivityValidationError> errors)
    {
        if (errors.Count == 0)
            return "Activity validation failed.";

        if (errors.Count == 1)
            return $"Activity validation failed: {errors[0].Message}";

        return $"Activity validation failed with {errors.Count} errors: {errors[0].Message}";
    }
}
