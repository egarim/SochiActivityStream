namespace Content.Abstractions;

/// <summary>
/// Request to delete a comment.
/// </summary>
public sealed class DeleteCommentRequest
{
    /// <summary>
    /// Tenant partition.
    /// </summary>
    public required string TenantId { get; set; }

    /// <summary>
    /// The comment ID to delete.
    /// </summary>
    public required string CommentId { get; set; }

    /// <summary>
    /// The actor performing the delete (must be the author).
    /// </summary>
    public required EntityRefDto Actor { get; set; }
}
