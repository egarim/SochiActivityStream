namespace Content.Abstractions;

/// <summary>
/// Main service interface for content operations (posts, comments, reactions).
/// </summary>
public interface IContentService
{
    #region Posts

    /// <summary>
    /// Creates a new post.
    /// </summary>
    Task<PostDto> CreatePostAsync(CreatePostRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gets a post by ID.
    /// </summary>
    /// <param name="tenantId">Tenant partition.</param>
    /// <param name="postId">Post ID.</param>
    /// <param name="viewer">Optional viewer for visibility and ViewerReaction.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<PostDto?> GetPostAsync(string tenantId, string postId, EntityRefDto? viewer = null, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing post.
    /// </summary>
    Task<PostDto> UpdatePostAsync(UpdatePostRequest request, CancellationToken ct = default);

    /// <summary>
    /// Deletes a post (soft delete).
    /// </summary>
    Task DeletePostAsync(DeletePostRequest request, CancellationToken ct = default);

    /// <summary>
    /// Queries posts with filtering and pagination.
    /// </summary>
    Task<ContentPageResult<PostDto>> QueryPostsAsync(PostQuery query, CancellationToken ct = default);

    #endregion

    #region Comments

    /// <summary>
    /// Creates a new comment on a post.
    /// </summary>
    Task<CommentDto> CreateCommentAsync(CreateCommentRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gets a comment by ID.
    /// </summary>
    Task<CommentDto?> GetCommentAsync(string tenantId, string commentId, EntityRefDto? viewer = null, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing comment.
    /// </summary>
    Task<CommentDto> UpdateCommentAsync(UpdateCommentRequest request, CancellationToken ct = default);

    /// <summary>
    /// Deletes a comment (soft delete).
    /// </summary>
    Task DeleteCommentAsync(DeleteCommentRequest request, CancellationToken ct = default);

    /// <summary>
    /// Queries comments with filtering and pagination.
    /// </summary>
    Task<ContentPageResult<CommentDto>> QueryCommentsAsync(CommentQuery query, CancellationToken ct = default);

    #endregion

    #region Reactions

    /// <summary>
    /// Adds or changes a reaction on a post or comment.
    /// </summary>
    Task<ReactionDto> ReactAsync(ReactRequest request, CancellationToken ct = default);

    /// <summary>
    /// Removes a reaction from a post or comment.
    /// </summary>
    Task RemoveReactionAsync(RemoveReactionRequest request, CancellationToken ct = default);

    /// <summary>
    /// Queries reactions on a target.
    /// </summary>
    Task<ContentPageResult<ReactionDto>> QueryReactionsAsync(ReactionQuery query, CancellationToken ct = default);

    #endregion
}
