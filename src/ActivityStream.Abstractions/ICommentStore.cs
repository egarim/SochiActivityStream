namespace ActivityStream.Abstractions;

public interface ICommentStore
{
    Task<CommentDto> UpsertAsync(CommentDto comment, CancellationToken ct = default);
    Task<CommentDto?> GetByIdAsync(string tenantId, string commentId, CancellationToken ct = default);
    Task<ContentPageResult<CommentDto>> QueryAsync(CommentQuery query, CancellationToken ct = default);
    Task DeleteAsync(string tenantId, string commentId, CancellationToken ct = default);
    Task IncrementReplyCountAsync(string tenantId, string commentId, int delta, CancellationToken ct = default);
    Task UpdateReactionCountAsync(string tenantId, string commentId, ReactionType type, int delta, CancellationToken ct = default);
}
