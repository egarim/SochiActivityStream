namespace ActivityStream.Abstractions;

/// <summary>
/// Storage interface for posts. Implementations handle persistence.
/// </summary>
public interface IPostStore
{
    Task<PostDto> UpsertAsync(PostDto post, CancellationToken ct = default);
    Task<PostDto?> GetByIdAsync(string tenantId, string postId, CancellationToken ct = default);
    Task<ContentPageResult<PostDto>> QueryAsync(PostQuery query, CancellationToken ct = default);
    Task DeleteAsync(string tenantId, string postId, CancellationToken ct = default);
    Task IncrementCommentCountAsync(string tenantId, string postId, int delta, CancellationToken ct = default);
    Task UpdateReactionCountAsync(string tenantId, string postId, ReactionType type, int delta, CancellationToken ct = default);
}
