namespace Content.Abstractions;

/// <summary>
/// Query parameters for listing posts.
/// </summary>
public sealed class PostQuery
{
    /// <summary>
    /// Tenant partition (required).
    /// </summary>
    public required string TenantId { get; set; }

    /// <summary>
    /// Filter by author (optional).
    /// </summary>
    public EntityRefDto? Author { get; set; }

    /// <summary>
    /// The viewer for visibility filtering and ViewerReaction population.
    /// </summary>
    public EntityRefDto? Viewer { get; set; }

    /// <summary>
    /// Minimum visibility level to include.
    /// </summary>
    public ContentVisibility? MinVisibility { get; set; }

    /// <summary>
    /// Whether to include soft-deleted posts.
    /// </summary>
    public bool IncludeDeleted { get; set; } = false;

    /// <summary>
    /// Pagination cursor.
    /// </summary>
    public string? Cursor { get; set; }

    /// <summary>
    /// Maximum items to return.
    /// </summary>
    public int Limit { get; set; } = 20;
}
