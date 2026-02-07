using Content.Abstractions;
using Microsoft.EntityFrameworkCore;
using BlazorBook.Web.Data;

namespace BlazorBook.Web.Stores.EFCore;

/// <summary>
/// EF Core implementation of IReactionStore
/// </summary>
public class EFCoreReactionStore : IReactionStore
{
    private readonly ApplicationDbContext _context;

    public EFCoreReactionStore(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<ReactionDto> UpsertAsync(ReactionDto reaction, CancellationToken ct = default)
    {
        var existing = await _context.Reactions
            .FirstOrDefaultAsync(r =>
                r.TenantId == reaction.TenantId &&
                r.TargetId == reaction.TargetId &&
                r.TargetKind == reaction.TargetKind &&
                r.Actor.Id == reaction.Actor.Id, ct);

        if (existing == null)
        {
            _context.Reactions.Add(reaction);
        }
        else
        {
            existing.Type = reaction.Type;
            existing.CreatedAt = reaction.CreatedAt;
        }

        await _context.SaveChangesAsync(ct);
        return reaction;
    }

    public async Task<ReactionDto?> GetAsync(string tenantId, string targetId, ReactionTargetKind targetKind, string actorId, CancellationToken ct = default)
    {
        return await _context.Reactions
            .FirstOrDefaultAsync(r =>
                r.TenantId == tenantId &&
                r.TargetId == targetId &&
                r.TargetKind == targetKind &&
                r.Actor.Id == actorId, ct);
    }

    public async Task<ContentPageResult<ReactionDto>> QueryAsync(ReactionQuery query, CancellationToken ct = default)
    {
        var queryable = _context.Reactions
            .Where(r => r.TenantId == query.TenantId)
            .Where(r => r.TargetId == query.TargetId)
            .Where(r => r.TargetKind == query.TargetKind);

        if (query.Type.HasValue)
        {
            queryable = queryable.Where(r => r.Type == query.Type.Value);
        }

        queryable = queryable
            .OrderByDescending(r => r.CreatedAt)
            .ThenBy(r => r.Id);

        var offset = DecodeCursor(query.Cursor);
        var items = await queryable
            .Skip(offset)
            .Take(query.Limit + 1)
            .ToListAsync(ct);

        var hasMore = items.Count > query.Limit;
        if (hasMore)
        {
            items = items.Take(query.Limit).ToList();
        }

        return new ContentPageResult<ReactionDto>
        {
            Items = items,
            NextCursor = hasMore ? EncodeCursor(offset + query.Limit) : null,
            TotalCount = -1
        };
    }

    public async Task DeleteAsync(string tenantId, string targetId, ReactionTargetKind targetKind, string actorId, CancellationToken ct = default)
    {
        var reaction = await _context.Reactions
            .FirstOrDefaultAsync(r =>
                r.TenantId == tenantId &&
                r.TargetId == targetId &&
                r.TargetKind == targetKind &&
                r.Actor.Id == actorId, ct);

        if (reaction != null)
        {
            _context.Reactions.Remove(reaction);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<Dictionary<ReactionType, int>> GetCountsAsync(string tenantId, string targetId, ReactionTargetKind targetKind, CancellationToken ct = default)
    {
        var counts = await _context.Reactions
            .Where(r => r.TenantId == tenantId)
            .Where(r => r.TargetId == targetId)
            .Where(r => r.TargetKind == targetKind)
            .GroupBy(r => r.Type)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Type, g => g.Count, ct);

        return counts;
    }

    private static int DecodeCursor(string? cursor)
    {
        if (string.IsNullOrEmpty(cursor))
            return 0;

        if (int.TryParse(cursor, out var offset))
            return offset;

        return 0;
    }

    private static string EncodeCursor(int offset)
    {
        return offset.ToString();
    }
}
