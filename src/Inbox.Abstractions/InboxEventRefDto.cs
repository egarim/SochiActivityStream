namespace Inbox.Abstractions;

/// <summary>
/// Reference to the event that triggered an inbox item.
/// </summary>
public sealed class InboxEventRefDto
{
    /// <summary>
    /// Type of event: "activity" | "follow-request" | etc.
    /// </summary>
    public required string Kind { get; set; }

    /// <summary>
    /// Id of the referenced event (activityId, requestId).
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Convenience: activity.TypeKey when Kind == "activity".
    /// </summary>
    public string? TypeKey { get; set; }

    /// <summary>
    /// When the event occurred.
    /// </summary>
    public DateTimeOffset? OccurredAt { get; set; }
}
