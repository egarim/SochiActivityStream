using System.Collections.Concurrent;
using ActivityStream.Abstractions;
using RelationshipService.Abstractions;
using RelationshipService.Core;

namespace RelationshipService.Store.InMemory;

/// <summary>
/// In-memory implementation of IRelationshipStore for testing and development.
/// Thread-safe using ConcurrentDictionary.
/// </summary>
public class InMemoryRelationshipStore : IRelationshipStore
{
    // Primary storage: tenantId -> (edgeId -> edge)
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, RelationshipEdgeDto>> _tenantEdges = new();

    // Secondary index for uniqueness: uniquenessKey -> edgeId
    private readonly ConcurrentDictionary<string, string> _uniquenessIndex = new();

    /// <inheritdoc />
    public Task<RelationshipEdgeDto?> GetByIdAsync(string tenantId, string edgeId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(edgeId))
        {
            return Task.FromResult<RelationshipEdgeDto?>(null);
        }

        var normalizedTenantId = tenantId.Trim().ToLowerInvariant();
        var normalizedEdgeId = edgeId.Trim();

        if (_tenantEdges.TryGetValue(normalizedTenantId, out var edges) &&
            edges.TryGetValue(normalizedEdgeId, out var edge))
        {
            return Task.FromResult<RelationshipEdgeDto?>(CloneEdge(edge));
        }

        return Task.FromResult<RelationshipEdgeDto?>(null);
    }

    /// <inheritdoc />
    public Task<RelationshipEdgeDto?> FindAsync(
        string tenantId,
        EntityRefDto from,
        EntityRefDto to,
        RelationshipKind kind,
        RelationshipScope scope,
        CancellationToken ct = default)
    {
        var uniquenessKey = BuildUniquenessKey(tenantId, from, to, kind, scope);

        if (_uniquenessIndex.TryGetValue(uniquenessKey, out var edgeId))
        {
            var normalizedTenantId = tenantId.Trim().ToLowerInvariant();
            if (_tenantEdges.TryGetValue(normalizedTenantId, out var edges) &&
                edges.TryGetValue(edgeId, out var edge))
            {
                return Task.FromResult<RelationshipEdgeDto?>(CloneEdge(edge));
            }
        }

        return Task.FromResult<RelationshipEdgeDto?>(null);
    }

    /// <inheritdoc />
    public Task UpsertAsync(RelationshipEdgeDto edge, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(edge);
        ArgumentNullException.ThrowIfNull(edge.Id);

        var normalizedTenantId = edge.TenantId.Trim().ToLowerInvariant();
        var uniquenessKey = BuildUniquenessKey(edge.TenantId, edge.From, edge.To, edge.Kind, edge.Scope);

        // Get or create tenant dictionary
        var edges = _tenantEdges.GetOrAdd(normalizedTenantId, _ => new ConcurrentDictionary<string, RelationshipEdgeDto>());

        // Check if there's an existing edge with different Id but same uniqueness key
        if (_uniquenessIndex.TryGetValue(uniquenessKey, out var existingEdgeId) && existingEdgeId != edge.Id)
        {
            // Remove old edge
            edges.TryRemove(existingEdgeId, out _);
        }

        // Store the edge
        edges[edge.Id] = CloneEdge(edge);

        // Update uniqueness index
        _uniquenessIndex[uniquenessKey] = edge.Id;

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(string tenantId, string edgeId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(edgeId))
        {
            return Task.CompletedTask;
        }

        var normalizedTenantId = tenantId.Trim().ToLowerInvariant();
        var normalizedEdgeId = edgeId.Trim();

        if (_tenantEdges.TryGetValue(normalizedTenantId, out var edges) &&
            edges.TryRemove(normalizedEdgeId, out var removedEdge))
        {
            // Remove from uniqueness index
            var uniquenessKey = BuildUniquenessKey(
                removedEdge.TenantId,
                removedEdge.From,
                removedEdge.To,
                removedEdge.Kind,
                removedEdge.Scope);
            _uniquenessIndex.TryRemove(uniquenessKey, out _);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<RelationshipEdgeDto>> QueryAsync(RelationshipQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var normalizedTenantId = query.TenantId.Trim().ToLowerInvariant();

        if (!_tenantEdges.TryGetValue(normalizedTenantId, out var edges))
        {
            return Task.FromResult<IReadOnlyList<RelationshipEdgeDto>>(Array.Empty<RelationshipEdgeDto>());
        }

        var results = edges.Values.AsEnumerable();

        // Filter by From
        if (query.From is not null)
        {
            results = results.Where(e => EntityRefMatches(e.From, query.From));
        }

        // Filter by To
        if (query.To is not null)
        {
            results = results.Where(e => EntityRefMatches(e.To, query.To));
        }

        // Filter by Kind
        if (query.Kind.HasValue)
        {
            results = results.Where(e => e.Kind == query.Kind.Value);
        }

        // Filter by Scope
        if (query.Scope.HasValue)
        {
            results = results.Where(e => e.Scope == query.Scope.Value);
        }

        // Filter by IsActive
        if (query.IsActive.HasValue)
        {
            results = results.Where(e => e.IsActive == query.IsActive.Value);
        }

        // Apply limit and clone results
        var finalResults = results
            .Take(query.Limit)
            .Select(CloneEdge)
            .ToList();

        return Task.FromResult<IReadOnlyList<RelationshipEdgeDto>>(finalResults);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<EntityRefDto>> GetRelatedEntitiesAsync(
        string tenantId,
        EntityRefDto from,
        RelationshipKind kind,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(from);

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return Task.FromResult<IReadOnlyList<EntityRefDto>>(Array.Empty<EntityRefDto>());
        }

        var normalizedTenantId = tenantId.Trim().ToLowerInvariant();

        if (!_tenantEdges.TryGetValue(normalizedTenantId, out var edges))
        {
            return Task.FromResult<IReadOnlyList<EntityRefDto>>(Array.Empty<EntityRefDto>());
        }

        var results = edges.Values
            .Where(e => e.IsActive &&
                        e.Kind == kind &&
                        EntityRefMatches(e.From, from))
            .Select(e => CloneEntityRef(e.To))
            .ToList();

        return Task.FromResult<IReadOnlyList<EntityRefDto>>(results);
    }

    /// <summary>
    /// Builds a uniqueness key for an edge.
    /// Format: "{tenant}|{fromKey}|{toKey}|{kind}|{scope}"
    /// </summary>
    private static string BuildUniquenessKey(
        string tenantId,
        EntityRefDto from,
        EntityRefDto to,
        RelationshipKind kind,
        RelationshipScope scope)
    {
        var tenant = tenantId.Trim().ToLowerInvariant();
        var fromKey = EntityRefEqualityHelper.ToKey(from);
        var toKey = EntityRefEqualityHelper.ToKey(to);
        return $"{tenant}|{fromKey}|{toKey}|{(int)kind}|{(int)scope}";
    }

    /// <summary>
    /// Checks if two entity refs match using case-insensitive comparison.
    /// </summary>
    private static bool EntityRefMatches(EntityRefDto a, EntityRefDto b)
    {
        return EntityRefEqualityHelper.AreEqual(a, b);
    }

    /// <summary>
    /// Creates a deep clone of an edge to prevent external mutation.
    /// </summary>
    private static RelationshipEdgeDto CloneEdge(RelationshipEdgeDto edge)
    {
        return new RelationshipEdgeDto
        {
            Id = edge.Id,
            TenantId = edge.TenantId,
            From = CloneEntityRef(edge.From),
            To = CloneEntityRef(edge.To),
            Kind = edge.Kind,
            Scope = edge.Scope,
            Filter = edge.Filter is not null ? CloneFilter(edge.Filter) : null,
            IsActive = edge.IsActive,
            CreatedAt = edge.CreatedAt
        };
    }

    private static EntityRefDto CloneEntityRef(EntityRefDto entity)
    {
        return new EntityRefDto
        {
            Kind = entity.Kind,
            Type = entity.Type,
            Id = entity.Id,
            Display = entity.Display,
            Meta = entity.Meta is not null
                ? new Dictionary<string, object?>(entity.Meta)
                : null
        };
    }

    private static RelationshipFilterDto CloneFilter(RelationshipFilterDto filter)
    {
        return new RelationshipFilterDto
        {
            TypeKeys = filter.TypeKeys?.ToList(),
            TypeKeyPrefixes = filter.TypeKeyPrefixes?.ToList(),
            RequiredTagsAny = filter.RequiredTagsAny?.ToList(),
            ExcludedTagsAny = filter.ExcludedTagsAny?.ToList(),
            AllowedVisibilities = filter.AllowedVisibilities?.ToList()
        };
    }
}
