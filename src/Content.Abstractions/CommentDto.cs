namespace Content.Abstractions;

/// <summary>
/// Represents a comment on a post (with optional threading support).
/// </summary>
public sealed class CommentDto
{
    /// <summary>
    /// Unique identifier for the comment.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Tenant partition.
    /// </summary>
    public required string TenantId { get; set; }

    /// <summary>
    /// The author of the comment.
    /// </summary>
    public required EntityRefDto Author { get; set; }

    /// <summary>
    /// The post this comment belongs to.
    /// </summary>
    public required string PostId { get; set; }

    /// <summary>
    /// Parent comment ID for threading (null = top-level comment).
    /// </summary>
    public string? ParentCommentId { get; set; }

    /// <summary>
    /// The comment body text.
    /// </summary>
    public required string Body { get; set; }

    /// <summary>
    /// When the comment was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When the comment was last updated (null if never updated).
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Whether the comment has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Denormalized count of replies to this comment.
    /// </summary>
    public int ReplyCount { get; set; }

    /// <summary>
    /// Denormalized reaction counts by type.
    /// </summary>
    public Dictionary<ReactionType, int> ReactionCounts { get; set; } = new();

    /// <summary>
    /// The current viewer's reaction type (populated per-viewer on read).
    /// </summary>
    public ReactionType? ViewerReaction { get; set; }
}
