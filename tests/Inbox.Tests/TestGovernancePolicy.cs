using ActivityStream.Abstractions;
using Inbox.Abstractions;
using RelationshipService.Abstractions;

namespace Inbox.Tests;

/// <summary>
/// Test implementation of IEntityGovernancePolicy that allows configuration.
/// </summary>
public class TestGovernancePolicy : IEntityGovernancePolicy
{
    private readonly HashSet<string> _nonTargetableEntities = new();
    private readonly HashSet<string> _entitiesRequiringApproval = new();
    private readonly Dictionary<string, List<EntityRefDto>> _approversByTarget = new();

    public void SetNonTargetable(EntityRefDto entity)
    {
        _nonTargetableEntities.Add(ToKey(entity));
    }

    public void SetRequiresApproval(EntityRefDto target)
    {
        _entitiesRequiringApproval.Add(ToKey(target));
    }

    public void SetApprovers(EntityRefDto target, params EntityRefDto[] approvers)
    {
        _approversByTarget[ToKey(target)] = approvers.ToList();
    }

    public Task<bool> IsTargetableAsync(string tenantId, EntityRefDto entity, CancellationToken ct = default)
    {
        return Task.FromResult(!_nonTargetableEntities.Contains(ToKey(entity)));
    }

    public Task<bool> RequiresApprovalToFollowAsync(
        string tenantId,
        EntityRefDto requester,
        EntityRefDto target,
        RelationshipKind requestedKind,
        CancellationToken ct = default)
    {
        return Task.FromResult(_entitiesRequiringApproval.Contains(ToKey(target)));
    }

    public Task<IReadOnlyList<EntityRefDto>> GetApproversAsync(
        string tenantId,
        EntityRefDto target,
        CancellationToken ct = default)
    {
        if (_approversByTarget.TryGetValue(ToKey(target), out var approvers))
            return Task.FromResult<IReadOnlyList<EntityRefDto>>(approvers);
        return Task.FromResult<IReadOnlyList<EntityRefDto>>(Array.Empty<EntityRefDto>());
    }

    private static string ToKey(EntityRefDto entity)
    {
        var kind = entity.Kind?.Trim().ToLowerInvariant() ?? "";
        var type = entity.Type?.Trim().ToLowerInvariant() ?? "";
        var id = entity.Id?.Trim().ToLowerInvariant() ?? "";
        return $"{kind}|{type}|{id}";
    }
}
