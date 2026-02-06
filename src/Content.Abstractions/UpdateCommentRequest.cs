namespace Content.Abstractions;

/// <summary>
/// Request to update a comment.
/// </summary>
public sealed class UpdateCommentRequest
{
    /// <summary>
    /// Tenant partition.
    /// </summary>
    public required string TenantId { get; set; }

    /// <summary>
    /// The comment ID to update.
    /// </summary>
    public required string CommentId { get; set; }

    /// <summary>
    /// The actor performing the update (must be the author).
    /// </summary>
    public required EntityRefDto Actor { get; set; }

    /// <summary>
    /// The new comment body text.
    /// </summary>
    public required string Body { get; set; }
}
