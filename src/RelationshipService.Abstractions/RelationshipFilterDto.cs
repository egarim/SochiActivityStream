using ActivityStream.Abstractions;

namespace RelationshipService.Abstractions;

/// <summary>
/// Filter criteria that controls when a relationship rule applies.
/// All conditions are evaluated with AND logic (all must match).
/// Within lists, OR logic applies (any one match is sufficient).
/// </summary>
public sealed class RelationshipFilterDto
{
    /// <summary>
    /// Match exact type keys (e.g., "invoice.paid").
    /// Case-insensitive comparison.
    /// If null or empty, does not filter by type key.
    /// </summary>
    public List<string>? TypeKeys { get; set; }

    /// <summary>
    /// Match prefixes (e.g., "invoice.", "build.").
    /// Case-insensitive comparison.
    /// If null or empty, does not filter by prefix.
    /// </summary>
    public List<string>? TypeKeyPrefixes { get; set; }

    /// <summary>
    /// Require at least one of these tags to be present in activity.Tags.
    /// Case-insensitive comparison.
    /// If null or empty, does not require any tags.
    /// </summary>
    public List<string>? RequiredTagsAny { get; set; }

    /// <summary>
    /// If any of these tags are present in activity.Tags, filter does NOT match.
    /// Case-insensitive comparison.
    /// If null or empty, no tags are excluded.
    /// </summary>
    public List<string>? ExcludedTagsAny { get; set; }

    /// <summary>
    /// Optional visibility constraint. If provided, activity.Visibility must be in this list.
    /// If null or empty, does not filter by visibility.
    /// </summary>
    public List<ActivityVisibility>? AllowedVisibilities { get; set; }
}
