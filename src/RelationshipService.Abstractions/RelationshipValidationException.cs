namespace RelationshipService.Abstractions;

/// <summary>
/// Exception thrown when relationship validation fails.
/// </summary>
public sealed class RelationshipValidationException : Exception
{
    /// <summary>
    /// The validation errors that caused this exception.
    /// </summary>
    public IReadOnlyList<RelationshipValidationError> Errors { get; }

    public RelationshipValidationException(IReadOnlyList<RelationshipValidationError> errors)
        : base(FormatMessage(errors))
    {
        Errors = errors;
    }

    public RelationshipValidationException(RelationshipValidationError error)
        : this(new[] { error })
    {
    }

    private static string FormatMessage(IReadOnlyList<RelationshipValidationError> errors)
    {
        if (errors.Count == 1)
        {
            return errors[0].Message;
        }

        return $"Relationship validation failed with {errors.Count} errors: {string.Join("; ", errors.Select(e => e.Message))}";
    }
}
