using System.Collections.Concurrent;
using ActivityStream.Abstractions;

namespace Content.Store.InMemory;

/// <summary>
/// In-memory implementation of IPostStore.
/// </summary>
public sealed class InMemoryPostStore : IPostStore
{
    private readonly ConcurrentDictionary<string, PostDto> _posts = new();
    private readonly ReaderWriterLockSlim _lock = new();

    private static string GetKey(string tenantId, string postId) => $"{tenantId}|{postId}";

    /// <inheritdoc />
    public Task<PostDto> UpsertAsync(PostDto post, CancellationToken ct = default)
    {
        var key = GetKey(post.TenantId, post.Id!);
        var cloned = Clone(post);
        _posts[key] = cloned;
        return Task.FromResult(Clone(cloned));
    }

    /// <inheritdoc />
    public Task<PostDto?> GetByIdAsync(string tenantId, string postId, CancellationToken ct = default)
    {
        var key = GetKey(tenantId, postId);
        return Task.FromResult(_posts.TryGetValue(key, out var post) ? Clone(post) : null);
    }

    /// <inheritdoc />
    public Task<ContentPageResult<PostDto>> QueryAsync(PostQuery query, CancellationToken ct = default)
    {
        _lock.EnterReadLock();
        try
        {
            var candidates = _posts.Values
                .Where(p => p.TenantId == query.TenantId)
                .Where(p => query.IncludeDeleted || !p.IsDeleted);

            if (query.Author != null)
                candidates = candidates.Where(p => p.Author.Id == query.Author.Id);

            if (query.MinVisibility.HasValue)
                candidates = candidates.Where(p => p.Visibility <= query.MinVisibility.Value);

            var sorted = candidates.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id);

            var offset = DecodeCursor(query.Cursor);
            var page = sorted.Skip(offset).Take(query.Limit + 1).ToList();
            var hasMore = page.Count > query.Limit;
            if (hasMore) page = page.Take(query.Limit).ToList();

            return Task.FromResult(new ContentPageResult<PostDto>
            {
                Items = page.Select(Clone).ToList(),
                NextCursor = hasMore ? EncodeCursor(offset + query.Limit) : null,
                TotalCount = -1
            });
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <inheritdoc />
    public Task DeleteAsync(string tenantId, string postId, CancellationToken ct = default)
    {
        var key = GetKey(tenantId, postId);
        _posts.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task IncrementCommentCountAsync(string tenantId, string postId, int delta, CancellationToken ct = default)
    {
        var key = GetKey(tenantId, postId);
        if (_posts.TryGetValue(key, out var post))
        {
            post.CommentCount = Math.Max(0, post.CommentCount + delta);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateReactionCountAsync(string tenantId, string postId, ReactionType type, int delta, CancellationToken ct = default)
    {
        var key = GetKey(tenantId, postId);
        if (_posts.TryGetValue(key, out var post))
        {
            if (!post.ReactionCounts.ContainsKey(type))
                post.ReactionCounts[type] = 0;
            post.ReactionCounts[type] = Math.Max(0, post.ReactionCounts[type] + delta);
            if (post.ReactionCounts[type] <= 0)
                post.ReactionCounts.Remove(type);
        }
        return Task.CompletedTask;
    }

    private static string EncodeCursor(int offset)
        => Convert.ToBase64String(BitConverter.GetBytes(offset));

    private static int DecodeCursor(string? cursor)
    {
        if (string.IsNullOrEmpty(cursor)) return 0;
        try
        {
            return BitConverter.ToInt32(Convert.FromBase64String(cursor), 0);
        }
        catch
        {
            return 0;
        }
    }

    private static PostDto Clone(PostDto p) => new()
    {
        Id = p.Id,
        TenantId = p.TenantId,
        Author = new EntityRefDto
        {
            Type = p.Author.Type,
            Id = p.Author.Id,
            DisplayName = p.Author.DisplayName,
            ImageUrl = p.Author.ImageUrl
        },
        Body = p.Body,
        MediaIds = p.MediaIds?.ToList(),
        Visibility = p.Visibility,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt,
        IsDeleted = p.IsDeleted,
        CommentCount = p.CommentCount,
        ReactionCounts = new Dictionary<ReactionType, int>(p.ReactionCounts),
        ViewerReaction = p.ViewerReaction
    };
}
