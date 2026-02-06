using ActivityStream.Abstractions;

namespace Inbox.Abstractions;

/// <summary>
/// Store for follow/subscribe requests.
/// </summary>
public interface IFollowRequestStore
{
    /// <summary>
    /// Gets a follow request by ID.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="requestId">Request identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The request or null if not found.</returns>
    Task<FollowRequestDto?> GetByIdAsync(string tenantId, string requestId, CancellationToken ct = default);

    /// <summary>
    /// Finds a follow request by its idempotency key.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="idempotencyKey">The idempotency key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The request or null if not found.</returns>
    Task<FollowRequestDto?> FindByIdempotencyAsync(
        string tenantId,
        string idempotencyKey,
        CancellationToken ct = default);

    /// <summary>
    /// Creates or updates a follow request.
    /// </summary>
    /// <param name="request">The request to upsert.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpsertAsync(FollowRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// Queries pending requests for a specific target.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="target">The target entity being followed/subscribed.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of pending requests.</returns>
    Task<IReadOnlyList<FollowRequestDto>> QueryPendingForTargetAsync(
        string tenantId,
        EntityRefDto target,
        CancellationToken ct = default);
}
