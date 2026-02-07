using RelationshipService.Abstractions;
using ActivityStream.Abstractions;
using Microsoft.EntityFrameworkCore;
using BlazorBook.Web.Data;

namespace BlazorBook.Web.Stores.EFCore;

/// <summary>
/// EF Core implementation of IRelationshipStore
/// </summary>
public class EFCoreRelationshipStore : IRelationshipStore
{
    private readonly ApplicationDbContext _context;

    public EFCoreRelationshipStore(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<RelationshipEdgeDto?> GetByIdAsync(string tenantId, string edgeId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(edgeId))
            return null;

        return await _context.RelationshipEdges
            .FirstOrDefaultAsync(e => e.TenantId == tenantId && e.Id == edgeId, ct);
    }

    public async Task<RelationshipEdgeDto?> FindAsync(
        string tenantId,
        EntityRefDto from,
        EntityRefDto to,
        RelationshipKind kind,
        RelationshipScope scope,
        CancellationToken ct = default)
    {
        return await _context.RelationshipEdges
            .FirstOrDefaultAsync(e =>
                e.TenantId == tenantId &&
                e.From.Id == from.Id &&
                e.From.Type == from.Type &&
                e.To.Id == to.Id &&
                e.To.Type == to.Type &&
                e.Kind == kind &&
                e.Scope == scope, ct);
    }

    public async Task UpsertAsync(RelationshipEdgeDto edge, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(edge);
        ArgumentNullException.ThrowIfNull(edge.Id);

        var existing = await FindAsync(
            edge.TenantId,
            edge.From,
            edge.To,
            edge.Kind,
            edge.Scope,
            ct);

        if (existing != null && existing.Id != edge.Id)
        {
            // Remove the old edge with the same uniqueness key
            _context.RelationshipEdges.Remove(existing);
        }

        var currentEdge = await _context.RelationshipEdges
            .FirstOrDefaultAsync(e => e.TenantId == edge.TenantId && e.Id == edge.Id, ct);

        if (currentEdge == null)
        {
            _context.RelationshipEdges.Add(edge);
        }
        else
        {
            currentEdge.From = edge.From;
            currentEdge.To = edge.To;
            currentEdge.Kind = edge.Kind;
            currentEdge.Scope = edge.Scope;
            currentEdge.IsActive = edge.IsActive;
            currentEdge.CreatedAt = edge.CreatedAt;
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(string tenantId, string edgeId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(edgeId))
            return;

        var edge = await _context.RelationshipEdges
            .FirstOrDefaultAsync(e => e.TenantId == tenantId && e.Id == edgeId, ct);

        if (edge != null)
        {
            _context.RelationshipEdges.Remove(edge);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<IReadOnlyList<RelationshipEdgeDto>> QueryAsync(RelationshipQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var queryable = _context.RelationshipEdges
            .Where(e => e.TenantId == query.TenantId);

        if (query.From != null)
        {
            queryable = queryable.Where(e => e.From.Id == query.From.Id && e.From.Type == query.From.Type);
        }

        if (query.To != null)
        {
            queryable = queryable.Where(e => e.To.Id == query.To.Id && e.To.Type == query.To.Type);
        }

        if (query.Kind.HasValue)
        {
            queryable = queryable.Where(e => e.Kind == query.Kind.Value);
        }

        if (query.Scope.HasValue)
        {
            queryable = queryable.Where(e => e.Scope == query.Scope.Value);
        }

        if (query.IsActive.HasValue)
        {
            queryable = queryable.Where(e => e.IsActive == query.IsActive.Value);
        }

        return await queryable
            .Take(query.Limit)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<EntityRefDto>> GetRelatedEntitiesAsync(
        string tenantId,
        EntityRefDto from,
        RelationshipKind kind,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(from);

        if (string.IsNullOrWhiteSpace(tenantId))
            return Array.Empty<EntityRefDto>();

        var results = await _context.RelationshipEdges
            .Where(e =>
                e.TenantId == tenantId &&
                e.IsActive &&
                e.Kind == kind &&
                e.From.Id == from.Id &&
                e.From.Type == from.Type)
            .Select(e => e.To)
            .ToListAsync(ct);

        return results;
    }
}
