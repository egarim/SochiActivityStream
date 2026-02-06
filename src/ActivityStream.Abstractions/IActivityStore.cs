namespace ActivityStream.Abstractions;

/// <summary>
/// Storage interface for activities. Implementations handle persistence.
/// </summary>
public interface IActivityStore
{
    /// <summary>
    /// Gets an activity by its id.
    /// </summary>
    /// <param name="tenantId">The tenant partition.</param>
    /// <param name="id">The activity id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The activity if found, null otherwise.</returns>
    Task<ActivityDto?> GetByIdAsync(string tenantId, string id, CancellationToken ct = default);

    /// <summary>
    /// Finds an activity by idempotency key.
    /// </summary>
    /// <param name="tenantId">The tenant partition.</param>
    /// <param name="sourceSystem">The source system name.</param>
    /// <param name="idempotencyKey">The idempotency key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The activity if found, null otherwise.</returns>
    Task<ActivityDto?> FindByIdempotencyAsync(
        string tenantId,
        string sourceSystem,
        string idempotencyKey,
        CancellationToken ct = default);

    /// <summary>
    /// Appends a new activity to the store.
    /// </summary>
    /// <param name="activity">The activity to append (must have Id set).</param>
    /// <param name="ct">Cancellation token.</param>
    Task AppendAsync(ActivityDto activity, CancellationToken ct = default);

    /// <summary>
    /// Queries activities. Returns items sorted by OccurredAt desc, Id desc.
    /// </summary>
    /// <param name="query">The query parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Matching activities.</returns>
    Task<IReadOnlyList<ActivityDto>> QueryAsync(ActivityQuery query, CancellationToken ct = default);
}
