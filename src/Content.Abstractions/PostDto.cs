namespace Content.Abstractions;

/// <summary>
/// Represents a post in the content system.
/// </summary>
public sealed class PostDto
{
    /// <summary>
    /// Unique identifier for the post.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Tenant partition.
    /// </summary>
    public required string TenantId { get; set; }

    /// <summary>
    /// The author of the post.
    /// </summary>
    public required EntityRefDto Author { get; set; }

    /// <summary>
    /// The main content body of the post.
    /// </summary>
    public required string Body { get; set; }

    /// <summary>
    /// Optional list of media IDs attached to the post.
    /// </summary>
    public List<string>? MediaIds { get; set; }

    /// <summary>
    /// Visibility level of the post.
    /// </summary>
    public ContentVisibility Visibility { get; set; } = ContentVisibility.Public;

    /// <summary>
    /// When the post was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When the post was last updated (null if never updated).
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Whether the post has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Denormalized count of comments on this post.
    /// </summary>
    public int CommentCount { get; set; }

    /// <summary>
    /// Denormalized reaction counts by type.
    /// </summary>
    public Dictionary<ReactionType, int> ReactionCounts { get; set; } = new();

    /// <summary>
    /// The current viewer's reaction type (populated per-viewer on read).
    /// </summary>
    public ReactionType? ViewerReaction { get; set; }
}
