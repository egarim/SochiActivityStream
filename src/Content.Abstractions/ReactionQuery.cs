namespace Content.Abstractions;

/// <summary>
/// Query parameters for listing reactions.
/// </summary>
public sealed class ReactionQuery
{
    /// <summary>
    /// Tenant partition (required).
    /// </summary>
    public required string TenantId { get; set; }

    /// <summary>
    /// The target ID (required).
    /// </summary>
    public required string TargetId { get; set; }

    /// <summary>
    /// The kind of target (required).
    /// </summary>
    public required ReactionTargetKind TargetKind { get; set; }

    /// <summary>
    /// Filter by reaction type (optional).
    /// </summary>
    public ReactionType? Type { get; set; }

    /// <summary>
    /// Pagination cursor.
    /// </summary>
    public string? Cursor { get; set; }

    /// <summary>
    /// Maximum items to return.
    /// </summary>
    public int Limit { get; set; } = 20;
}
