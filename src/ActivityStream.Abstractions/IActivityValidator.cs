namespace ActivityStream.Abstractions;

/// <summary>
/// Validates activities before publishing.
/// </summary>
public interface IActivityValidator
{
    /// <summary>
    /// Validates an activity and returns any errors.
    /// </summary>
    /// <param name="activity">The activity to validate.</param>
    /// <returns>List of validation errors (empty if valid).</returns>
    IReadOnlyList<ActivityValidationError> Validate(ActivityDto activity);
}
