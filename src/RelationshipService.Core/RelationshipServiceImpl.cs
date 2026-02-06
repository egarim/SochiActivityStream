using ActivityStream.Abstractions;
using RelationshipService.Abstractions;

namespace RelationshipService.Core;

/// <summary>
/// Core implementation of IRelationshipService.
/// Manages relationship edges and evaluates visibility decisions.
/// </summary>
public class RelationshipServiceImpl : IRelationshipService
{
    private readonly IRelationshipStore _store;
    private readonly IIdGenerator _idGenerator;

    public RelationshipServiceImpl(IRelationshipStore store, IIdGenerator idGenerator)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
    }

    /// <inheritdoc />
    public async Task<RelationshipEdgeDto> UpsertAsync(RelationshipEdgeDto edge, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(edge);

        // Normalize
        RelationshipEdgeNormalizer.Normalize(edge, _idGenerator);

        // Validate
        var errors = RelationshipEdgeValidator.Validate(edge);
        if (errors.Count > 0)
        {
            throw new RelationshipValidationException(errors);
        }

        // Check for existing edge with same uniqueness key
        var existing = await _store.FindAsync(
            edge.TenantId,
            edge.From,
            edge.To,
            edge.Kind,
            edge.Scope,
            ct);

        if (existing is not null)
        {
            // Update existing edge - keep same Id and CreatedAt
            edge.Id = existing.Id;
            edge.CreatedAt = existing.CreatedAt;
        }

        await _store.UpsertAsync(edge, ct);
        return edge;
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string tenantId, string edgeId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(edgeId))
        {
            throw new ArgumentException("EdgeId is required.", nameof(edgeId));
        }

        await _store.RemoveAsync(tenantId.Trim(), edgeId.Trim(), ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RelationshipEdgeDto>> QueryAsync(RelationshipQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (string.IsNullOrWhiteSpace(query.TenantId))
        {
            throw new ArgumentException("TenantId is required in query.", nameof(query));
        }

        return await _store.QueryAsync(query, ct);
    }

    /// <inheritdoc />
    public async Task<RelationshipDecision> CanSeeAsync(
        string tenantId,
        EntityRefDto viewer,
        ActivityDto activity,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        ArgumentNullException.ThrowIfNull(activity);

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        // Priority 0: Self-authored - viewer is the activity actor
        if (EntityRefEqualityHelper.AreEqual(viewer, activity.Actor))
        {
            return new RelationshipDecision(
                RelationshipDecisionKind.Allowed,
                Allowed: true,
                Reason: "SelfAuthored");
        }

        // Fetch viewer's edges for this tenant
        var viewerEdges = await _store.QueryAsync(new RelationshipQuery
        {
            TenantId = tenantId,
            From = viewer,
            IsActive = true,
            Limit = 1000 // Get all relevant edges
        }, ct);

        // Partition edges by kind for efficient lookup
        var blocks = viewerEdges.Where(e => e.Kind == RelationshipKind.Block).ToList();
        var denies = viewerEdges.Where(e => e.Kind == RelationshipKind.Deny).ToList();
        var mutes = viewerEdges.Where(e => e.Kind == RelationshipKind.Mute).ToList();
        var allows = viewerEdges.Where(e => e.Kind == RelationshipKind.Allow).ToList();

        // Priority 1: Block (hard deny)
        var blockMatch = FindMatchingEdge(blocks, activity);
        if (blockMatch is not null)
        {
            return new RelationshipDecision(
                RelationshipDecisionKind.Denied,
                Allowed: false,
                Reason: "Block",
                MatchedEdgeId: blockMatch.Id);
        }

        // Priority 2: Deny (rule-based deny)
        var denyMatch = FindMatchingEdge(denies, activity);
        if (denyMatch is not null)
        {
            return new RelationshipDecision(
                RelationshipDecisionKind.Denied,
                Allowed: false,
                Reason: "DenyRule",
                MatchedEdgeId: denyMatch.Id);
        }

        // Priority 3: Visibility base policy
        if (activity.Visibility == ActivityVisibility.Private)
        {
            // Private: allowed only if viewer equals Actor, Owner, or any Target
            var isActorOrOwnerOrTarget =
                EntityRefEqualityHelper.AreEqual(viewer, activity.Actor) ||
                EntityRefEqualityHelper.AreEqual(viewer, activity.Owner) ||
                (activity.Targets?.Any(t => EntityRefEqualityHelper.AreEqual(viewer, t)) ?? false);

            if (!isActorOrOwnerOrTarget)
            {
                return new RelationshipDecision(
                    RelationshipDecisionKind.Denied,
                    Allowed: false,
                    Reason: "PrivateVisibility");
            }
        }
        // Public and Internal: continue to next checks

        // Priority 4: Mute (soft hide)
        var muteMatch = FindMatchingEdge(mutes, activity);
        if (muteMatch is not null)
        {
            return new RelationshipDecision(
                RelationshipDecisionKind.Hidden,
                Allowed: false,
                Reason: "Mute",
                MatchedEdgeId: muteMatch.Id);
        }

        // Priority 5: Allow (explicit allow) - note: does not override Block/Deny
        var allowMatch = FindMatchingEdge(allows, activity);
        if (allowMatch is not null)
        {
            return new RelationshipDecision(
                RelationshipDecisionKind.Allowed,
                Allowed: true,
                Reason: "AllowRule",
                MatchedEdgeId: allowMatch.Id);
        }

        // Priority 6: Default - allowed
        return new RelationshipDecision(
            RelationshipDecisionKind.Allowed,
            Allowed: true,
            Reason: "Default");
    }

    /// <summary>
    /// Finds the first edge that matches the activity based on scope and filter.
    /// </summary>
    private static RelationshipEdgeDto? FindMatchingEdge(
        IEnumerable<RelationshipEdgeDto> edges,
        ActivityDto activity)
    {
        foreach (var edge in edges)
        {
            if (EdgeMatchesActivity(edge, activity))
            {
                return edge;
            }
        }

        return null;
    }

    /// <summary>
    /// Determines if an edge matches an activity based on scope and filter.
    /// </summary>
    private static bool EdgeMatchesActivity(RelationshipEdgeDto edge, ActivityDto activity)
    {
        // Check scope match first
        if (!ScopeMatchesToEntity(edge.Scope, edge.To, activity))
        {
            return false;
        }

        // Check filter match
        return FilterMatcher.Matches(edge.Filter, activity);
    }

    /// <summary>
    /// Checks if the edge's To entity matches the activity based on the scope.
    /// </summary>
    private static bool ScopeMatchesToEntity(RelationshipScope scope, EntityRefDto to, ActivityDto activity)
    {
        return scope switch
        {
            RelationshipScope.ActorOnly => EntityRefEqualityHelper.AreEqual(to, activity.Actor),
            RelationshipScope.TargetOnly => activity.Targets?.Any(t => EntityRefEqualityHelper.AreEqual(to, t)) ?? false,
            RelationshipScope.OwnerOnly => activity.Owner is not null && EntityRefEqualityHelper.AreEqual(to, activity.Owner),
            RelationshipScope.Any => EntityRefEqualityHelper.AreEqual(to, activity.Actor) ||
                                     (activity.Targets?.Any(t => EntityRefEqualityHelper.AreEqual(to, t)) ?? false) ||
                                     (activity.Owner is not null && EntityRefEqualityHelper.AreEqual(to, activity.Owner)),
            _ => false
        };
    }

    /// <inheritdoc />
    public async Task<RelationshipEdgeDto?> GetEdgeAsync(
        string tenantId,
        EntityRefDto from,
        EntityRefDto to,
        RelationshipKind kind,
        RelationshipScope scope = RelationshipScope.Any,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);

        return await _store.FindAsync(tenantId.Trim(), from, to, kind, scope, ct);
    }

    /// <inheritdoc />
    public async Task<bool> AreMutualAsync(
        string tenantId,
        EntityRefDto entity1,
        EntityRefDto entity2,
        RelationshipKind kind,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        ArgumentNullException.ThrowIfNull(entity1);
        ArgumentNullException.ThrowIfNull(entity2);

        // Check A→B edge
        var aToB = await _store.FindAsync(tenantId.Trim(), entity1, entity2, kind, RelationshipScope.Any, ct);
        if (aToB is null || !aToB.IsActive)
        {
            return false;
        }

        // Check B→A edge
        var bToA = await _store.FindAsync(tenantId.Trim(), entity2, entity1, kind, RelationshipScope.Any, ct);
        return bToA is not null && bToA.IsActive;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EntityRefDto>> GetMutualRelationshipsAsync(
        string tenantId,
        EntityRefDto entity1,
        EntityRefDto entity2,
        RelationshipKind kind,
        int limit = 50,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        ArgumentNullException.ThrowIfNull(entity1);
        ArgumentNullException.ThrowIfNull(entity2);

        var normalizedTenantId = tenantId.Trim();

        // Get entities that entity1 has relationships with
        var entity1Relationships = await _store.GetRelatedEntitiesAsync(normalizedTenantId, entity1, kind, ct);

        // Get entities that entity2 has relationships with
        var entity2Relationships = await _store.GetRelatedEntitiesAsync(normalizedTenantId, entity2, kind, ct);

        // Find intersection (mutual)
        var entity2Set = new HashSet<string>(
            entity2Relationships.Select(e => $"{e.Kind}:{e.Id}"),
            StringComparer.OrdinalIgnoreCase);

        var mutuals = entity1Relationships
            .Where(e => entity2Set.Contains($"{e.Kind}:{e.Id}"))
            .Take(limit)
            .ToList();

        return mutuals;
    }

    /// <inheritdoc />
    public async Task<int> CountMutualRelationshipsAsync(
        string tenantId,
        EntityRefDto entity1,
        EntityRefDto entity2,
        RelationshipKind kind,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        ArgumentNullException.ThrowIfNull(entity1);
        ArgumentNullException.ThrowIfNull(entity2);

        var normalizedTenantId = tenantId.Trim();

        // Get entities that entity1 has relationships with
        var entity1Relationships = await _store.GetRelatedEntitiesAsync(normalizedTenantId, entity1, kind, ct);

        // Get entities that entity2 has relationships with
        var entity2Relationships = await _store.GetRelatedEntitiesAsync(normalizedTenantId, entity2, kind, ct);

        // Count intersection
        var entity2Set = new HashSet<string>(
            entity2Relationships.Select(e => $"{e.Kind}:{e.Id}"),
            StringComparer.OrdinalIgnoreCase);

        return entity1Relationships.Count(e => entity2Set.Contains($"{e.Kind}:{e.Id}"));
    }
}
