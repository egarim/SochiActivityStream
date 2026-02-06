namespace Content.Abstractions;

/// <summary>
/// Storage interface for comments. Implementations handle persistence.
/// </summary>
public interface ICommentStore
{
    /// <summary>
    /// Upserts a comment (insert or update).
    /// </summary>
    Task<CommentDto> UpsertAsync(CommentDto comment, CancellationToken ct = default);

    /// <summary>
    /// Gets a comment by ID.
    /// </summary>
    Task<CommentDto?> GetByIdAsync(string tenantId, string commentId, CancellationToken ct = default);

    /// <summary>
    /// Queries comments with filtering and pagination.
    /// </summary>
    Task<ContentPageResult<CommentDto>> QueryAsync(CommentQuery query, CancellationToken ct = default);

    /// <summary>
    /// Deletes a comment by ID.
    /// </summary>
    Task DeleteAsync(string tenantId, string commentId, CancellationToken ct = default);

    /// <summary>
    /// Atomically increments the reply count on a comment.
    /// </summary>
    Task IncrementReplyCountAsync(string tenantId, string commentId, int delta, CancellationToken ct = default);

    /// <summary>
    /// Atomically updates a reaction count on a comment.
    /// </summary>
    Task UpdateReactionCountAsync(string tenantId, string commentId, ReactionType type, int delta, CancellationToken ct = default);
}
