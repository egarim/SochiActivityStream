namespace Content.Abstractions;

/// <summary>
/// Storage interface for posts. Implementations handle persistence.
/// </summary>
public interface IPostStore
{
    /// <summary>
    /// Upserts a post (insert or update).
    /// </summary>
    Task<PostDto> UpsertAsync(PostDto post, CancellationToken ct = default);

    /// <summary>
    /// Gets a post by ID.
    /// </summary>
    Task<PostDto?> GetByIdAsync(string tenantId, string postId, CancellationToken ct = default);

    /// <summary>
    /// Queries posts with filtering and pagination.
    /// </summary>
    Task<ContentPageResult<PostDto>> QueryAsync(PostQuery query, CancellationToken ct = default);

    /// <summary>
    /// Deletes a post by ID.
    /// </summary>
    Task DeleteAsync(string tenantId, string postId, CancellationToken ct = default);

    /// <summary>
    /// Atomically increments the comment count on a post.
    /// </summary>
    Task IncrementCommentCountAsync(string tenantId, string postId, int delta, CancellationToken ct = default);

    /// <summary>
    /// Atomically updates a reaction count on a post.
    /// </summary>
    Task UpdateReactionCountAsync(string tenantId, string postId, ReactionType type, int delta, CancellationToken ct = default);
}
