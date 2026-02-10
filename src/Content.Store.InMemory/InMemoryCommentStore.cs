using System.Collections.Concurrent;
using ActivityStream.Abstractions;

namespace Content.Store.InMemory;

/// <summary>
/// In-memory implementation of ICommentStore.
/// </summary>
public sealed class InMemoryCommentStore : ICommentStore
{
    private readonly ConcurrentDictionary<string, CommentDto> _comments = new();
    private readonly ReaderWriterLockSlim _lock = new();

    private static string GetKey(string tenantId, string commentId) => $"{tenantId}|{commentId}";

    /// <inheritdoc />
    public Task<CommentDto> UpsertAsync(CommentDto comment, CancellationToken ct = default)
    {
        var key = GetKey(comment.TenantId, comment.Id!);
        var cloned = Clone(comment);
        _comments[key] = cloned;
        return Task.FromResult(Clone(cloned));
    }

    /// <inheritdoc />
    public Task<CommentDto?> GetByIdAsync(string tenantId, string commentId, CancellationToken ct = default)
    {
        var key = GetKey(tenantId, commentId);
        return Task.FromResult(_comments.TryGetValue(key, out var comment) ? Clone(comment) : null);
    }

    /// <inheritdoc />
    public Task<ContentPageResult<CommentDto>> QueryAsync(CommentQuery query, CancellationToken ct = default)
    {
        _lock.EnterReadLock();
        try
        {
            var candidates = _comments.Values
                .Where(c => c.TenantId == query.TenantId)
                .Where(c => c.PostId == query.PostId)
                .Where(c => query.IncludeDeleted || !c.IsDeleted);

            // Filter by parent (null = top-level only)
            if (query.ParentCommentId == null)
            {
                candidates = candidates.Where(c => c.ParentCommentId == null);
            }
            else
            {
                candidates = candidates.Where(c => c.ParentCommentId == query.ParentCommentId);
            }

            var sorted = candidates.OrderBy(c => c.CreatedAt).ThenBy(c => c.Id);

            var offset = DecodeCursor(query.Cursor);
            var page = sorted.Skip(offset).Take(query.Limit + 1).ToList();
            var hasMore = page.Count > query.Limit;
            if (hasMore) page = page.Take(query.Limit).ToList();

            return Task.FromResult(new ContentPageResult<CommentDto>
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
    public Task DeleteAsync(string tenantId, string commentId, CancellationToken ct = default)
    {
        var key = GetKey(tenantId, commentId);
        _comments.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task IncrementReplyCountAsync(string tenantId, string commentId, int delta, CancellationToken ct = default)
    {
        var key = GetKey(tenantId, commentId);
        if (_comments.TryGetValue(key, out var comment))
        {
            comment.ReplyCount = Math.Max(0, comment.ReplyCount + delta);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateReactionCountAsync(string tenantId, string commentId, ReactionType type, int delta, CancellationToken ct = default)
    {
        var key = GetKey(tenantId, commentId);
        if (_comments.TryGetValue(key, out var comment))
        {
            if (!comment.ReactionCounts.ContainsKey(type))
                comment.ReactionCounts[type] = 0;
            comment.ReactionCounts[type] = Math.Max(0, comment.ReactionCounts[type] + delta);
            if (comment.ReactionCounts[type] <= 0)
                comment.ReactionCounts.Remove(type);
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

    private static CommentDto Clone(CommentDto c) => new()
    {
        Id = c.Id,
        TenantId = c.TenantId,
        Author = new EntityRefDto
        {
            Type = c.Author.Type,
            Id = c.Author.Id,
            DisplayName = c.Author.DisplayName,
            ImageUrl = c.Author.ImageUrl
        },
        PostId = c.PostId,
        ParentCommentId = c.ParentCommentId,
        Body = c.Body,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt,
        IsDeleted = c.IsDeleted,
        ReplyCount = c.ReplyCount,
        ReactionCounts = new Dictionary<ReactionType, int>(c.ReactionCounts),
        ViewerReaction = c.ViewerReaction
    };
}
