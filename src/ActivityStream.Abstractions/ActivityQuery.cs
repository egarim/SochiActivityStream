namespace ActivityStream.Abstractions;

/// <summary>
/// Query parameters for retrieving activities.
/// </summary>
public class ActivityQuery
{
    /// <summary>
    /// Required tenant partition.
    /// </summary>
    public required string TenantId { get; set; }

    /// <summary>
    /// Optional: filter by activity type.
    /// </summary>
    public string? TypeKey { get; set; }

    /// <summary>
    /// Optional: filter by actor entity ref (exact match on Kind+Type+Id).
    /// </summary>
    public EntityRefDto? Actor { get; set; }

    /// <summary>
    /// Optional: filter by target entity ref (matches any target in Targets list).
    /// </summary>
    public EntityRefDto? Target { get; set; }

    /// <summary>
    /// Optional: filter activities occurring on or after this time (inclusive).
    /// </summary>
    public DateTimeOffset? From { get; set; }

    /// <summary>
    /// Optional: filter activities occurring before this time (exclusive).
    /// </summary>
    public DateTimeOffset? To { get; set; }

    /// <summary>
    /// Maximum items to return. Default 50. Hard max 200.
    /// </summary>
    public int Limit { get; set; } = 50;

    /// <summary>
    /// Cursor for pagination (opaque string to callers).
    /// </summary>
    public string? Cursor { get; set; }
}
