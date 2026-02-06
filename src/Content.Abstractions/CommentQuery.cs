namespace Content.Abstractions;

/// <summary>
/// Query parameters for listing comments.
/// </summary>
public sealed class CommentQuery
{
    /// <summary>
    /// Tenant partition (required).
    /// </summary>
    public required string TenantId { get; set; }

    /// <summary>
    /// The post to get comments for (required).
    /// </summary>
    public required string PostId { get; set; }

    /// <summary>
    /// Parent comment ID for getting nested replies (null = top-level only).
    /// </summary>
    public string? ParentCommentId { get; set; }

    /// <summary>
    /// The viewer for ViewerReaction population.
    /// </summary>
    public EntityRefDto? Viewer { get; set; }

    /// <summary>
    /// Whether to include soft-deleted comments.
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
