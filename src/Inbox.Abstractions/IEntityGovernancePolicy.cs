using ActivityStream.Abstractions;
using RelationshipService.Abstractions;

namespace Inbox.Abstractions;

/// <summary>
/// Policy hook for entity governance decisions.
/// Implemented by the host application to customize behavior.
/// </summary>
public interface IEntityGovernancePolicy
{
    /// <summary>
    /// Determines if an entity can be targeted by activities.
    /// Private objects should return false.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="entity">The entity to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the entity can be targeted; false otherwise.</returns>
    Task<bool> IsTargetableAsync(string tenantId, EntityRefDto entity, CancellationToken ct = default);

    /// <summary>
    /// Determines if following/subscribing to an entity requires approval.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="requester">The entity making the request.</param>
    /// <param name="target">The entity being followed/subscribed.</param>
    /// <param name="requestedKind">The type of relationship being requested.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if approval is required; false otherwise.</returns>
    Task<bool> RequiresApprovalToFollowAsync(
        string tenantId,
        EntityRefDto requester,
        EntityRefDto target,
        RelationshipKind requestedKind,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the approver inbox owners (profiles) for a target entity.
    /// Usually owner profile(s) and/or moderator profile(s).
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="target">The target entity.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of approver entities.</returns>
    Task<IReadOnlyList<EntityRefDto>> GetApproversAsync(
        string tenantId,
        EntityRefDto target,
        CancellationToken ct = default);
}
