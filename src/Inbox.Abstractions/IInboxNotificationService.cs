using ActivityStream.Abstractions;

namespace Inbox.Abstractions;

/// <summary>
/// Core service for managing inbox notifications and follow/subscribe requests.
/// </summary>
public interface IInboxNotificationService
{
    /// <summary>
    /// Processes a published activity and creates inbox items for relevant recipients.
    /// </summary>
    /// <param name="activity">The activity to process.</param>
    /// <param name="ct">Cancellation token.</param>
    Task OnActivityPublishedAsync(ActivityDto activity, CancellationToken ct = default);

    /// <summary>
    /// Adds an inbox item directly (for custom notifications).
    /// </summary>
    /// <param name="item">The inbox item to add.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The added inbox item with generated Id.</returns>
    Task<InboxItemDto> AddAsync(InboxItemDto item, CancellationToken ct = default);

    /// <summary>
    /// Queries inbox items for one or more recipients.
    /// </summary>
    /// <param name="query">Query parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated result of inbox items.</returns>
    Task<InboxPageResult> QueryInboxAsync(InboxQuery query, CancellationToken ct = default);

    /// <summary>
    /// Marks an inbox item as read.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="inboxItemId">Inbox item identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task MarkReadAsync(string tenantId, string inboxItemId, CancellationToken ct = default);

    /// <summary>
    /// Archives an inbox item.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="inboxItemId">Inbox item identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ArchiveAsync(string tenantId, string inboxItemId, CancellationToken ct = default);

    /// <summary>
    /// Creates a follow/subscribe request. If approval is not required, the relationship
    /// is created immediately. Otherwise, a pending request is stored and approvers are notified.
    /// </summary>
    /// <param name="request">The follow request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created or found request.</returns>
    Task<FollowRequestDto> CreateFollowRequestAsync(FollowRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// Approves a pending follow/subscribe request.
    /// Creates the relationship edge and notifies the requester.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="requestId">Request identifier.</param>
    /// <param name="decidedBy">The entity approving the request.</param>
    /// <param name="reason">Optional reason for approval.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated request.</returns>
    Task<FollowRequestDto> ApproveRequestAsync(
        string tenantId,
        string requestId,
        EntityRefDto decidedBy,
        string? reason,
        CancellationToken ct = default);

    /// <summary>
    /// Denies a pending follow/subscribe request.
    /// Notifies the requester.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="requestId">Request identifier.</param>
    /// <param name="decidedBy">The entity denying the request.</param>
    /// <param name="reason">Optional reason for denial.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated request.</returns>
    Task<FollowRequestDto> DenyRequestAsync(
        string tenantId,
        string requestId,
        EntityRefDto decidedBy,
        string? reason,
        CancellationToken ct = default);
}
