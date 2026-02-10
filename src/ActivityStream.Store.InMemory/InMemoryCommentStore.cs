using System.Collections.Concurrent;
using ActivityStream.Abstractions;

namespace ActivityStream.Store.InMemory;

/// <summary>
/// In-memory implementation of ICommentStore (migrated from Content.Store.InMemory).
/// </summary>
public sealed class InMemoryCommentStore : ICommentStore
{
    private readonly ConcurrentDictionary<string, CommentDto> _comments = new();

    private static string GetKey(string tenantId, string commentId) => $"{tenantId}|{commentId}";

    public Task<CommentDto> UpsertAsync(CommentDto comment, CancellationToken ct = default)
    {
        var key = GetKey(comment.TenantId, comment.Id!);
        _comments[key] = Clone(comment);
        return Task.FromResult(Clone(_comments[key]));
    }

    public Task<CommentDto?> GetByIdAsync(string tenantId, string commentId, CancellationToken ct = default)
    {
        var key = GetKey(tenantId, commentId);
        return Task.FromResult(_comments.TryGetValue(key, out var c) ? Clone(c) : null);
    }

    public Task<ContentPageResult<CommentDto>> QueryAsync(CommentQuery query, CancellationToken ct = default)
    {
        var candidates = _comments.Values
            .Where(c => c.TenantId == query.TenantId)
            .Where(c => query.IncludeDeleted || !c.IsDeleted)
            .Where(c => string.IsNullOrEmpty(query.PostId) || c.PostId == query.PostId)
            .OrderByDescending(c => c.CreatedAt)
            .ToList();

        var offset = DecodeCursor(query.Cursor);
        var page = candidates.Skip(offset).Take(query.Limit + 1).ToList();
        var hasMore = page.Count > query.Limit;
        if (hasMore) page = page.Take(query.Limit).ToList();

        return Task.FromResult(new ContentPageResult<CommentDto>
        {
            Items = page.Select(Clone).ToList(),
            NextCursor = hasMore ? EncodeCursor(offset + query.Limit) : null,
            TotalCount = -1
        });
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

    public Task DeleteAsync(string tenantId, string commentId, CancellationToken ct = default)
    {
        var key = GetKey(tenantId, commentId);
        _comments.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task IncrementReplyCountAsync(string tenantId, string commentId, int delta, CancellationToken ct = default)
    {
        var key = GetKey(tenantId, commentId);
        if (_comments.TryGetValue(key, out var c))
        {
            c.ReplyCount = Math.Max(0, c.ReplyCount + delta);
        }
        return Task.CompletedTask;
    }

    public Task UpdateReactionCountAsync(string tenantId, string commentId, ReactionType type, int delta, CancellationToken ct = default)
    {
        var key = GetKey(tenantId, commentId);
        if (_comments.TryGetValue(key, out var c))
        {
            if (!c.ReactionCounts.ContainsKey(type)) c.ReactionCounts[type] = 0;
            c.ReactionCounts[type] = Math.Max(0, c.ReactionCounts[type] + delta);
            if (c.ReactionCounts[type] <= 0) c.ReactionCounts.Remove(type);
        }
        return Task.CompletedTask;
    }

    private static CommentDto Clone(CommentDto c) => new()
    {
        Id = c.Id,
        TenantId = c.TenantId,
        PostId = c.PostId,
        ParentCommentId = c.ParentCommentId,
        Author = new EntityRefDto { Type = c.Author.Type, Id = c.Author.Id, DisplayName = c.Author.DisplayName, ImageUrl = c.Author.ImageUrl },
        Body = c.Body,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt,
        IsDeleted = c.IsDeleted,
        ReplyCount = c.ReplyCount,
        ReactionCounts = new Dictionary<ReactionType, int>(c.ReactionCounts),
        ViewerReaction = c.ViewerReaction
    };
}
