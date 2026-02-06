using ActivityStream.Abstractions;

namespace ActivityStream.Core;

/// <summary>
/// Default activity validator implementing the v1 validation rules.
/// </summary>
public sealed class DefaultActivityValidator : IActivityValidator
{
    /// <summary>
    /// Maximum allowed length for Summary field.
    /// </summary>
    public const int MaxSummaryLength = 500;

    /// <summary>
    /// Maximum allowed length for TypeKey field.
    /// </summary>
    public const int MaxTypeKeyLength = 200;

    /// <summary>
    /// Maximum allowed number of tags.
    /// </summary>
    public const int MaxTagCount = 50;

    public IReadOnlyList<ActivityValidationError> Validate(ActivityDto activity)
    {
        var errors = new List<ActivityValidationError>();

        // TenantId required
        if (string.IsNullOrWhiteSpace(activity.TenantId))
        {
            errors.Add(new ActivityValidationError("REQUIRED", "TenantId is required.", "TenantId"));
        }

        // TypeKey required
        if (string.IsNullOrWhiteSpace(activity.TypeKey))
        {
            errors.Add(new ActivityValidationError("REQUIRED", "TypeKey is required.", "TypeKey"));
        }
        else if (activity.TypeKey.Length > MaxTypeKeyLength)
        {
            errors.Add(new ActivityValidationError("MAX_LENGTH", $"TypeKey exceeds maximum length of {MaxTypeKeyLength}.", "TypeKey"));
        }

        // OccurredAt must not be default
        if (activity.OccurredAt == default)
        {
            errors.Add(new ActivityValidationError("REQUIRED", "OccurredAt must not be default.", "OccurredAt"));
        }

        // Actor required and valid
        if (activity.Actor is null)
        {
            errors.Add(new ActivityValidationError("REQUIRED", "Actor is required.", "Actor"));
        }
        else
        {
            ValidateEntityRef(activity.Actor, "Actor", errors);
        }

        // Payload required
        if (activity.Payload is null)
        {
            errors.Add(new ActivityValidationError("REQUIRED", "Payload is required.", "Payload"));
        }

        // Targets validation
        for (int i = 0; i < activity.Targets.Count; i++)
        {
            var target = activity.Targets[i];
            ValidateEntityRef(target, $"Targets[{i}]", errors);
        }

        // Owner validation (if present)
        if (activity.Owner is not null)
        {
            ValidateEntityRef(activity.Owner, "Owner", errors);
        }

        // Summary length
        if (activity.Summary is not null && activity.Summary.Length > MaxSummaryLength)
        {
            errors.Add(new ActivityValidationError("MAX_LENGTH", $"Summary exceeds maximum length of {MaxSummaryLength}.", "Summary"));
        }

        // Tags count
        if (activity.Tags.Count > MaxTagCount)
        {
            errors.Add(new ActivityValidationError("MAX_COUNT", $"Tags exceed maximum count of {MaxTagCount}.", "Tags"));
        }

        return errors;
    }

    private static void ValidateEntityRef(EntityRefDto entityRef, string path, List<ActivityValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(entityRef.Kind))
        {
            errors.Add(new ActivityValidationError("REQUIRED", $"{path}.Kind is required.", $"{path}.Kind"));
        }

        if (string.IsNullOrWhiteSpace(entityRef.Type))
        {
            errors.Add(new ActivityValidationError("REQUIRED", $"{path}.Type is required.", $"{path}.Type"));
        }

        if (string.IsNullOrWhiteSpace(entityRef.Id))
        {
            errors.Add(new ActivityValidationError("REQUIRED", $"{path}.Id is required.", $"{path}.Id"));
        }
    }
}
