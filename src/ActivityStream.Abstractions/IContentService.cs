namespace ActivityStream.Abstractions;

public interface IContentService
{
    Task<PostDto> CreatePostAsync(CreatePostRequest request, CancellationToken ct = default);
    Task<PostDto?> GetPostAsync(string tenantId, string postId, EntityRefDto? viewer = null, CancellationToken ct = default);
    Task<PostDto> UpdatePostAsync(UpdatePostRequest request, CancellationToken ct = default);
    Task<ContentPageResult<PostDto>> QueryPostsAsync(PostQuery query, CancellationToken ct = default);

    Task DeletePostAsync(DeletePostRequest request, CancellationToken ct = default);

    Task<CommentDto> CreateCommentAsync(CreateCommentRequest request, CancellationToken ct = default);
    Task<CommentDto?> GetCommentAsync(string tenantId, string commentId, EntityRefDto? viewer = null, CancellationToken ct = default);
    Task<CommentDto> UpdateCommentAsync(UpdateCommentRequest request, CancellationToken ct = default);
    Task<ContentPageResult<CommentDto>> QueryCommentsAsync(CommentQuery query, CancellationToken ct = default);

    Task DeleteCommentAsync(DeleteCommentRequest request, CancellationToken ct = default);

    Task<ReactionDto> ReactAsync(ReactRequest request, CancellationToken ct = default);
    Task<ContentPageResult<ReactionDto>> QueryReactionsAsync(ReactionQuery query, CancellationToken ct = default);
    Task RemoveReactionAsync(RemoveReactionRequest request, CancellationToken ct = default);
}
