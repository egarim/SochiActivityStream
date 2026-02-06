namespace Content.Abstractions;

/// <summary>
/// Request to create a comment on a post.
/// </summary>
public sealed class CreateCommentRequest
{
    /// <summary>
    /// Tenant partition.
    /// </summary>
    public required string TenantId { get; set; }

    /// <summary>
    /// The author of the comment.
    /// </summary>
    public required EntityRefDto Author { get; set; }

    /// <summary>
    /// The post to comment on.
    /// </summary>
    public required string PostId { get; set; }

    /// <summary>
    /// Parent comment ID for threading (null for top-level).
    /// </summary>
    public string? ParentCommentId { get; set; }

    /// <summary>
    /// The comment body text.
    /// </summary>
    public required string Body { get; set; }
}
