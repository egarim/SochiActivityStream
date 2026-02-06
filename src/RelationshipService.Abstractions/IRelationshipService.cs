using ActivityStream.Abstractions;

namespace RelationshipService.Abstractions;

/// <summary>
/// Core service for managing relationship edges and evaluating visibility decisions.
/// </summary>
public interface IRelationshipService
{
    /// <summary>
    /// Creates or updates a relationship edge.
    /// Uniqueness is determined by (TenantId, From, To, Kind, Scope).
    /// If an edge with the same uniqueness key exists, it is updated.
    /// </summary>
    Task<RelationshipEdgeDto> UpsertAsync(RelationshipEdgeDto edge, CancellationToken ct = default);

    /// <summary>
    /// Removes a relationship edge by ID.
    /// </summary>
    Task RemoveAsync(string tenantId, string edgeId, CancellationToken ct = default);

    /// <summary>
    /// Queries relationship edges.
    /// </summary>
    Task<IReadOnlyList<RelationshipEdgeDto>> QueryAsync(RelationshipQuery query, CancellationToken ct = default);

    /// <summary>
    /// Determines whether a viewer can see an activity based on visibility rules and relationship edges.
    /// 
    /// Priority order:
    /// 0. Self-authored: if viewer == Actor, always Allowed
    /// 1. Block: hard deny
    /// 2. Deny: rule-based deny
    /// 3. Visibility: Private requires viewer == Actor OR Owner OR any Target
    /// 4. Mute: soft hide (Hidden decision)
    /// 5. Allow: explicit allow (does not override Block/Deny)
    /// 6. Default: Allowed
    /// </summary>
    Task<RelationshipDecision> CanSeeAsync(
        string tenantId,
        EntityRefDto viewer,
        ActivityDto activity,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a single edge by From, To, Kind, and optional Scope.
    /// </summary>
    Task<RelationshipEdgeDto?> GetEdgeAsync(
        string tenantId,
        EntityRefDto from,
        EntityRefDto to,
        RelationshipKind kind,
        RelationshipScope scope = RelationshipScope.Any,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if two entities have mutual relationships of the specified kind.
    /// (A→B and B→A both exist and are active)
    /// </summary>
    Task<bool> AreMutualAsync(
        string tenantId,
        EntityRefDto entity1,
        EntityRefDto entity2,
        RelationshipKind kind,
        CancellationToken ct = default);

    /// <summary>
    /// Gets entities that both users have relationships with (e.g., mutual friends/follows).
    /// Returns entities where both entity1→X and entity2→X edges exist.
    /// </summary>
    Task<IReadOnlyList<EntityRefDto>> GetMutualRelationshipsAsync(
        string tenantId,
        EntityRefDto entity1,
        EntityRefDto entity2,
        RelationshipKind kind,
        int limit = 50,
        CancellationToken ct = default);

    /// <summary>
    /// Counts mutual relationships between two entities.
    /// </summary>
    Task<int> CountMutualRelationshipsAsync(
        string tenantId,
        EntityRefDto entity1,
        EntityRefDto entity2,
        RelationshipKind kind,
        CancellationToken ct = default);
}
