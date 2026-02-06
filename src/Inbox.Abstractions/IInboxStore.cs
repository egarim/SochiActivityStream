using ActivityStream.Abstractions;

namespace Inbox.Abstractions;

/// <summary>
/// Store for inbox items.
/// </summary>
public interface IInboxStore
{
    /// <summary>
    /// Gets an inbox item by ID.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="id">Inbox item identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The inbox item or null if not found.</returns>
    Task<InboxItemDto?> GetByIdAsync(string tenantId, string id, CancellationToken ct = default);

    /// <summary>
    /// Finds an inbox item by its dedup key for a specific recipient.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="recipient">The inbox owner.</param>
    /// <param name="dedupKey">The dedup key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The inbox item or null if not found.</returns>
    Task<InboxItemDto?> FindByDedupKeyAsync(
        string tenantId,
        EntityRefDto recipient,
        string dedupKey,
        CancellationToken ct = default);

    /// <summary>
    /// Finds an inbox item by its thread key for a specific recipient.
    /// Used for grouping multiple events into one thread.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="recipient">The inbox owner.</param>
    /// <param name="threadKey">The thread key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The inbox item or null if not found.</returns>
    Task<InboxItemDto?> FindByThreadKeyAsync(
        string tenantId,
        EntityRefDto recipient,
        string threadKey,
        CancellationToken ct = default);

    /// <summary>
    /// Creates or updates an inbox item.
    /// </summary>
    /// <param name="item">The inbox item to upsert.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpsertAsync(InboxItemDto item, CancellationToken ct = default);

    /// <summary>
    /// Queries inbox items with optional filters and pagination.
    /// </summary>
    /// <param name="query">Query parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated result of inbox items.</returns>
    Task<InboxPageResult> QueryAsync(InboxQuery query, CancellationToken ct = default);

    /// <summary>
    /// Updates the status of an inbox item.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="id">Inbox item identifier.</param>
    /// <param name="status">The new status.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateStatusAsync(string tenantId, string id, InboxItemStatus status, CancellationToken ct = default);
}
