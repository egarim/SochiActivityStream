using System.Collections.Concurrent;
using Content.Abstractions;

namespace Content.Store.InMemory;

/// <summary>
/// In-memory implementation of IReactionStore.
/// </summary>
public sealed class InMemoryReactionStore : IReactionStore
{
    private readonly ConcurrentDictionary<string, ReactionDto> _reactions = new();
    private readonly ReaderWriterLockSlim _lock = new();

    // Key format: tenantId|targetKind|targetId|actorId
    private static string GetKey(string tenantId, string targetId, ReactionTargetKind targetKind, string actorId)
        => $"{tenantId}|{(int)targetKind}|{targetId}|{actorId}";

    /// <inheritdoc />
    public Task<ReactionDto> UpsertAsync(ReactionDto reaction, CancellationToken ct = default)
    {
        var key = GetKey(reaction.TenantId, reaction.TargetId, reaction.TargetKind, reaction.Actor.Id);
        var cloned = Clone(reaction);
        _reactions[key] = cloned;
        return Task.FromResult(Clone(cloned));
    }

    /// <inheritdoc />
    public Task<ReactionDto?> GetAsync(string tenantId, string targetId, ReactionTargetKind targetKind, string actorId, CancellationToken ct = default)
    {
        var key = GetKey(tenantId, targetId, targetKind, actorId);
        return Task.FromResult(_reactions.TryGetValue(key, out var reaction) ? Clone(reaction) : null);
    }

    /// <inheritdoc />
    public Task<ContentPageResult<ReactionDto>> QueryAsync(ReactionQuery query, CancellationToken ct = default)
    {
        _lock.EnterReadLock();
        try
        {
            var candidates = _reactions.Values
                .Where(r => r.TenantId == query.TenantId)
                .Where(r => r.TargetId == query.TargetId)
                .Where(r => r.TargetKind == query.TargetKind);

            if (query.Type.HasValue)
                candidates = candidates.Where(r => r.Type == query.Type.Value);

            var sorted = candidates.OrderByDescending(r => r.CreatedAt).ThenBy(r => r.Id);

            var offset = DecodeCursor(query.Cursor);
            var page = sorted.Skip(offset).Take(query.Limit + 1).ToList();
            var hasMore = page.Count > query.Limit;
            if (hasMore) page = page.Take(query.Limit).ToList();

            return Task.FromResult(new ContentPageResult<ReactionDto>
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
    public Task DeleteAsync(string tenantId, string targetId, ReactionTargetKind targetKind, string actorId, CancellationToken ct = default)
    {
        var key = GetKey(tenantId, targetId, targetKind, actorId);
        _reactions.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<Dictionary<ReactionType, int>> GetCountsAsync(string tenantId, string targetId, ReactionTargetKind targetKind, CancellationToken ct = default)
    {
        _lock.EnterReadLock();
        try
        {
            var counts = _reactions.Values
                .Where(r => r.TenantId == tenantId)
                .Where(r => r.TargetId == targetId)
                .Where(r => r.TargetKind == targetKind)
                .GroupBy(r => r.Type)
                .ToDictionary(g => g.Key, g => g.Count());

            return Task.FromResult(counts);
        }
        finally
        {
            _lock.ExitReadLock();
        }
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

    private static ReactionDto Clone(ReactionDto r) => new()
    {
        Id = r.Id,
        TenantId = r.TenantId,
        Actor = new EntityRefDto
        {
            Type = r.Actor.Type,
            Id = r.Actor.Id,
            DisplayName = r.Actor.DisplayName,
            ImageUrl = r.Actor.ImageUrl
        },
        TargetId = r.TargetId,
        TargetKind = r.TargetKind,
        Type = r.Type,
        CreatedAt = r.CreatedAt
    };
}
