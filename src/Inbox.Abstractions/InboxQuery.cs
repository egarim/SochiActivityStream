using ActivityStream.Abstractions;

namespace Inbox.Abstractions;

/// <summary>
/// Query parameters for retrieving inbox items.
/// </summary>
public sealed class InboxQuery
{
    /// <summary>
    /// Required partition key.
    /// </summary>
    public required string TenantId { get; set; }

    /// <summary>
    /// Query a single inbox owner (Profile) OR multiple owners (user has many profiles).
    /// </summary>
    public List<EntityRefDto> Recipients { get; set; } = new();

    /// <summary>
    /// Filter by status (e.g., Unread only).
    /// </summary>
    public InboxItemStatus? Status { get; set; }

    /// <summary>
    /// Filter by kind (e.g., Request only).
    /// </summary>
    public InboxItemKind? Kind { get; set; }

    /// <summary>
    /// Filter items created at or after this time.
    /// </summary>
    public DateTimeOffset? From { get; set; }

    /// <summary>
    /// Filter items created before this time.
    /// </summary>
    public DateTimeOffset? To { get; set; }

    /// <summary>
    /// Maximum number of items to return. Default 50.
    /// </summary>
    public int Limit { get; set; } = 50;

    /// <summary>
    /// Cursor for pagination (opaque string to callers).
    /// </summary>
    public string? Cursor { get; set; }
}
