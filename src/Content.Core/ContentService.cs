using ActivityStream.Abstractions;
using ContentEntityRef = ActivityStream.Abstractions.EntityRefDto;

namespace Content.Core;

/// <summary>
/// Implementation of IContentService that orchestrates stores and business logic.
/// </summary>
public sealed class ContentService : IContentService
{
    private readonly IPostStore _postStore;
    private readonly ICommentStore _commentStore;
    private readonly IReactionStore _reactionStore;
    private readonly IIdGenerator _idGenerator;
    private readonly ContentServiceOptions _options;

    public ContentService(
        IPostStore postStore,
        ICommentStore commentStore,
        IReactionStore reactionStore,
        IIdGenerator idGenerator,
        ContentServiceOptions? options = null)
    {
        _postStore = postStore ?? throw new ArgumentNullException(nameof(postStore));
        _commentStore = commentStore ?? throw new ArgumentNullException(nameof(commentStore));
        _reactionStore = reactionStore ?? throw new ArgumentNullException(nameof(reactionStore));
        _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
        _options = options ?? new ContentServiceOptions();
    }

    #region Posts

    /// <inheritdoc />
    public async Task<PostDto> CreatePostAsync(CreatePostRequest request, CancellationToken ct = default)
    {
        ContentValidator.ValidateCreatePost(request, _options);

        var post = new PostDto
        {
            Id = _idGenerator.NewId(),
            TenantId = request.TenantId,
            Author = request.Author,
            Body = ContentNormalizer.NormalizeText(request.Body),
            MediaIds = request.MediaIds,
            Visibility = request.Visibility,
            CreatedAt = DateTimeOffset.UtcNow
        };

        return await _postStore.UpsertAsync(post, ct);
    }

    /// <inheritdoc />
    public async Task<PostDto?> GetPostAsync(string tenantId, string postId, ContentEntityRef? viewer = null, CancellationToken ct = default)
    {
        var post = await _postStore.GetByIdAsync(tenantId, postId, ct);
        if (post == null) return null;

        // Populate viewer's reaction if viewer provided
        if (viewer != null)
        {
            var reaction = await _reactionStore.GetAsync(tenantId, postId, ReactionTargetKind.Post, viewer.Id, ct);
            post.ViewerReaction = reaction?.Type;
        }

        return post;
    }

    /// <inheritdoc />
    public async Task<PostDto> UpdatePostAsync(UpdatePostRequest request, CancellationToken ct = default)
    {
        ContentValidator.ValidateUpdatePost(request, _options);

        var existing = await _postStore.GetByIdAsync(request.TenantId, request.PostId, ct);
        if (existing == null)
            throw new ContentValidationException(ContentValidationError.PostNotFound);

        if (existing.Author.Id != request.Actor.Id)
            throw new ContentValidationException(ContentValidationError.PostUnauthorized);

        if (request.Body != null)
            existing.Body = ContentNormalizer.NormalizeText(request.Body);

        if (request.Visibility.HasValue)
            existing.Visibility = request.Visibility.Value;

        existing.UpdatedAt = DateTimeOffset.UtcNow;

        return await _postStore.UpsertAsync(existing, ct);
    }

    /// <inheritdoc />
    public async Task DeletePostAsync(DeletePostRequest request, CancellationToken ct = default)
    {
        ContentValidator.ValidateDeletePost(request);

        var existing = await _postStore.GetByIdAsync(request.TenantId, request.PostId, ct);
        if (existing == null)
            throw new ContentValidationException(ContentValidationError.PostNotFound);

        if (existing.Author.Id != request.Actor.Id)
            throw new ContentValidationException(ContentValidationError.PostUnauthorized);

        existing.IsDeleted = true;
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        await _postStore.UpsertAsync(existing, ct);
    }

    /// <inheritdoc />
    public async Task<ContentPageResult<PostDto>> QueryPostsAsync(PostQuery query, CancellationToken ct = default)
    {
        query.Limit = Math.Clamp(query.Limit, 1, _options.MaxPageSize);
        var result = await _postStore.QueryAsync(query, ct);

        // Populate viewer reactions
        if (query.Viewer != null)
        {
            foreach (var post in result.Items)
            {
                var reaction = await _reactionStore.GetAsync(query.TenantId, post.Id!, ReactionTargetKind.Post, query.Viewer.Id, ct);
                post.ViewerReaction = reaction?.Type;
            }
        }

        return result;
    }

    #endregion

    #region Comments

    /// <inheritdoc />
    public async Task<CommentDto> CreateCommentAsync(CreateCommentRequest request, CancellationToken ct = default)
    {
        ContentValidator.ValidateCreateComment(request, _options);

        // Verify post exists
        var post = await _postStore.GetByIdAsync(request.TenantId, request.PostId, ct);
        if (post == null)
            throw new ContentValidationException(ContentValidationError.PostNotFound);

        // Verify parent comment if specified
        if (!string.IsNullOrEmpty(request.ParentCommentId))
        {
            var parent = await _commentStore.GetByIdAsync(request.TenantId, request.ParentCommentId, ct);
            if (parent == null)
                throw new ContentValidationException(ContentValidationError.ParentCommentNotFound);
            if (parent.PostId != request.PostId)
                throw new ContentValidationException(ContentValidationError.ParentCommentWrongPost);
        }

        var comment = new CommentDto
        {
            Id = _idGenerator.NewId(),
            TenantId = request.TenantId,
            Author = request.Author,
            PostId = request.PostId,
            ParentCommentId = request.ParentCommentId,
            Body = ContentNormalizer.NormalizeText(request.Body),
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = await _commentStore.UpsertAsync(comment, ct);

        // Update denormalized counts
        await _postStore.IncrementCommentCountAsync(request.TenantId, request.PostId, 1, ct);
        if (!string.IsNullOrEmpty(request.ParentCommentId))
        {
            await _commentStore.IncrementReplyCountAsync(request.TenantId, request.ParentCommentId, 1, ct);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<CommentDto?> GetCommentAsync(string tenantId, string commentId, ContentEntityRef? viewer = null, CancellationToken ct = default)
    {
        var comment = await _commentStore.GetByIdAsync(tenantId, commentId, ct);
        if (comment == null) return null;

        // Populate viewer's reaction
        if (viewer != null)
        {
            var reaction = await _reactionStore.GetAsync(tenantId, commentId, ReactionTargetKind.Comment, viewer.Id, ct);
            comment.ViewerReaction = reaction?.Type;
        }

        return comment;
    }

    /// <inheritdoc />
    public async Task<CommentDto> UpdateCommentAsync(UpdateCommentRequest request, CancellationToken ct = default)
    {
        ContentValidator.ValidateUpdateComment(request, _options);

        var existing = await _commentStore.GetByIdAsync(request.TenantId, request.CommentId, ct);
        if (existing == null)
            throw new ContentValidationException(ContentValidationError.CommentNotFound);

        if (existing.Author.Id != request.Actor.Id)
            throw new ContentValidationException(ContentValidationError.CommentUnauthorized);

        existing.Body = ContentNormalizer.NormalizeText(request.Body);
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        return await _commentStore.UpsertAsync(existing, ct);
    }

    /// <inheritdoc />
    public async Task DeleteCommentAsync(DeleteCommentRequest request, CancellationToken ct = default)
    {
        ContentValidator.ValidateDeleteComment(request);

        var existing = await _commentStore.GetByIdAsync(request.TenantId, request.CommentId, ct);
        if (existing == null)
            throw new ContentValidationException(ContentValidationError.CommentNotFound);

        if (existing.Author.Id != request.Actor.Id)
            throw new ContentValidationException(ContentValidationError.CommentUnauthorized);

        existing.IsDeleted = true;
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        await _commentStore.UpsertAsync(existing, ct);

        // Update denormalized counts
        await _postStore.IncrementCommentCountAsync(request.TenantId, existing.PostId, -1, ct);
        if (!string.IsNullOrEmpty(existing.ParentCommentId))
        {
            await _commentStore.IncrementReplyCountAsync(request.TenantId, existing.ParentCommentId, -1, ct);
        }
    }

    /// <inheritdoc />
    public async Task<ContentPageResult<CommentDto>> QueryCommentsAsync(CommentQuery query, CancellationToken ct = default)
    {
        query.Limit = Math.Clamp(query.Limit, 1, _options.MaxPageSize);
        var result = await _commentStore.QueryAsync(query, ct);

        // Populate viewer reactions
        if (query.Viewer != null)
        {
            foreach (var comment in result.Items)
            {
                var reaction = await _reactionStore.GetAsync(query.TenantId, comment.Id!, ReactionTargetKind.Comment, query.Viewer.Id, ct);
                comment.ViewerReaction = reaction?.Type;
            }
        }

        return result;
    }

    #endregion

    #region Reactions

    /// <inheritdoc />
    public async Task<ReactionDto> ReactAsync(ReactRequest request, CancellationToken ct = default)
    {
        ContentValidator.ValidateReact(request);

        // Verify target exists
        if (request.TargetKind == ReactionTargetKind.Post)
        {
            var post = await _postStore.GetByIdAsync(request.TenantId, request.TargetId, ct);
            if (post == null)
                throw new ContentValidationException(ContentValidationError.TargetNotFound);
        }
        else
        {
            var comment = await _commentStore.GetByIdAsync(request.TenantId, request.TargetId, ct);
            if (comment == null)
                throw new ContentValidationException(ContentValidationError.TargetNotFound);
        }

        // Check for existing reaction
        var existing = await _reactionStore.GetAsync(request.TenantId, request.TargetId, request.TargetKind, request.Actor.Id, ct);

        var reaction = new ReactionDto
        {
            Id = existing?.Id ?? _idGenerator.NewId(),
            TenantId = request.TenantId,
            Actor = request.Actor,
            ActorId = request.Actor.Id,
            TargetId = request.TargetId,
            TargetKind = request.TargetKind,
            Type = request.Type,
            CreatedAt = existing?.CreatedAt ?? DateTimeOffset.UtcNow
        };

        await _reactionStore.UpsertAsync(reaction, ct);

        // Update counts
        if (existing == null)
        {
            // New reaction
            await UpdateReactionCount(request.TenantId, request.TargetId, request.TargetKind, request.Type, 1, ct);
        }
        else if (existing.Type != request.Type)
        {
            // Changed reaction
            await UpdateReactionCount(request.TenantId, request.TargetId, request.TargetKind, existing.Type, -1, ct);
            await UpdateReactionCount(request.TenantId, request.TargetId, request.TargetKind, request.Type, 1, ct);
        }

        return reaction;
    }

    /// <inheritdoc />
    public async Task RemoveReactionAsync(RemoveReactionRequest request, CancellationToken ct = default)
    {
        ContentValidator.ValidateRemoveReaction(request);

        var existing = await _reactionStore.GetAsync(request.TenantId, request.TargetId, request.TargetKind, request.Actor.Id, ct);
        if (existing == null)
            return; // Already removed, no-op

        await _reactionStore.DeleteAsync(request.TenantId, request.TargetId, request.TargetKind, request.Actor.Id, ct);

        // Update counts
        await UpdateReactionCount(request.TenantId, request.TargetId, request.TargetKind, existing.Type, -1, ct);
    }

    /// <inheritdoc />
    public Task<ContentPageResult<ReactionDto>> QueryReactionsAsync(ReactionQuery query, CancellationToken ct = default)
    {
        query.Limit = Math.Clamp(query.Limit, 1, _options.MaxPageSize);
        return _reactionStore.QueryAsync(query, ct);
    }

    private async Task UpdateReactionCount(string tenantId, string targetId, ReactionTargetKind targetKind, ReactionType type, int delta, CancellationToken ct)
    {
        if (targetKind == ReactionTargetKind.Post)
        {
            await _postStore.UpdateReactionCountAsync(tenantId, targetId, type, delta, ct);
        }
        else
        {
            await _commentStore.UpdateReactionCountAsync(tenantId, targetId, type, delta, ct);
        }
    }

    #endregion
}
