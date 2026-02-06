using ActivityStream.Abstractions;

namespace RelationshipService.Abstractions;

/// <summary>
/// Persistence layer for relationship edges.
/// </summary>
public interface IRelationshipStore
{
    /// <summary>
    /// Gets an edge by its ID.
    /// </summary>
    Task<RelationshipEdgeDto?> GetByIdAsync(string tenantId, string edgeId, CancellationToken ct = default);

    /// <summary>
    /// Finds an existing edge for upsert uniqueness check.
    /// Uniqueness key: (TenantId, From, To, Kind, Scope).
    /// </summary>
    Task<RelationshipEdgeDto?> FindAsync(
        string tenantId,
        EntityRefDto from,
        EntityRefDto to,
        RelationshipKind kind,
        RelationshipScope scope,
        CancellationToken ct = default);

    /// <summary>
    /// Creates or updates an edge.
    /// </summary>
    Task UpsertAsync(RelationshipEdgeDto edge, CancellationToken ct = default);

    /// <summary>
    /// Removes an edge by ID (hard delete).
    /// </summary>
    Task RemoveAsync(string tenantId, string edgeId, CancellationToken ct = default);

    /// <summary>
    /// Queries edges based on the provided criteria.
    /// </summary>
    Task<IReadOnlyList<RelationshipEdgeDto>> QueryAsync(RelationshipQuery query, CancellationToken ct = default);

    /// <summary>
    /// Gets all "To" entities for edges from a specific entity with specified kind.
    /// Used for mutual relationship calculations.
    /// </summary>
    Task<IReadOnlyList<EntityRefDto>> GetRelatedEntitiesAsync(
        string tenantId,
        EntityRefDto from,
        RelationshipKind kind,
        CancellationToken ct = default);
}
