using System.Collections.Concurrent;
using ActivityStream.Abstractions;

namespace ActivityStream.Store.InMemory;

/// <summary>
/// In-memory implementation of IReactionStore (migrated from Content.Store.InMemory).
/// </summary>
public sealed class InMemoryReactionStore : IReactionStore
{
    private readonly ConcurrentDictionary<string, ReactionDto> _reactions = new();

    private static string GetKey(string tenantId, string targetId, ReactionTargetKind kind, string actorId)
        => $"{tenantId}|{targetId}|{(int)kind}|{actorId}";

    public Task<ReactionDto> UpsertAsync(ReactionDto reaction, CancellationToken ct = default)
    {
        var key = GetKey(reaction.TenantId, reaction.TargetId, reaction.TargetKind, reaction.ActorId);
        _reactions[key] = Clone(reaction);
        return Task.FromResult(Clone(_reactions[key]));
    }

    public Task<ReactionDto?> GetAsync(string tenantId, string targetId, ReactionTargetKind kind, string actorId, CancellationToken ct = default)
    {
        var key = GetKey(tenantId, targetId, kind, actorId);
        return Task.FromResult(_reactions.TryGetValue(key, out var r) ? Clone(r) : null);
    }

    public Task DeleteAsync(string tenantId, string targetId, ReactionTargetKind kind, string actorId, CancellationToken ct = default)
    {
        var key = GetKey(tenantId, targetId, kind, actorId);
        _reactions.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task<ContentPageResult<ReactionDto>> QueryAsync(ReactionQuery query, CancellationToken ct = default)
    {
        var candidates = _reactions.Values
            .Where(r => r.TenantId == query.TenantId)
            .Where(r => r.TargetId == query.TargetId)
            .Where(r => r.TargetKind == query.TargetKind)
            .Where(r => !query.Type.HasValue || r.Type == query.Type.Value)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

        var offset = DecodeCursor(query.Cursor);
        var page = candidates.Skip(offset).Take(query.Limit + 1).ToList();
        var hasMore = page.Count > query.Limit;
        if (hasMore) page = page.Take(query.Limit).ToList();

        return Task.FromResult(new ContentPageResult<ReactionDto>
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

    public Task<Dictionary<ReactionType, int>> GetCountsAsync(string tenantId, string targetId, ReactionTargetKind targetKind, CancellationToken ct = default)
    {
        var counts = _reactions.Values
            .Where(r => r.TenantId == tenantId && r.TargetId == targetId && r.TargetKind == targetKind)
            .GroupBy(r => r.Type)
            .ToDictionary(g => g.Key, g => g.Count());

        return Task.FromResult(counts);
    }

    private static ReactionDto Clone(ReactionDto r) => new()
    {
        Id = r.Id,
        TenantId = r.TenantId,
        Actor = new EntityRefDto { Type = r.Actor.Type, Id = r.Actor.Id, DisplayName = r.Actor.DisplayName, ImageUrl = r.Actor.ImageUrl },
        ActorId = r.ActorId,
        TargetId = r.TargetId,
        TargetKind = r.TargetKind,
        Type = r.Type,
        CreatedAt = r.CreatedAt
    };
}
