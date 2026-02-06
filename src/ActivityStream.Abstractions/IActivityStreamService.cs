namespace ActivityStream.Abstractions;

/// <summary>
/// Main service interface for publishing and querying activities.
/// </summary>
public interface IActivityStreamService
{
    /// <summary>
    /// Publishes a single activity. Handles normalization, validation, id generation, and idempotency.
    /// </summary>
    /// <param name="activity">The activity to publish.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The stored activity with assigned Id and CreatedAt.</returns>
    /// <exception cref="ActivityValidationException">Thrown if validation fails.</exception>
    Task<ActivityDto> PublishAsync(ActivityDto activity, CancellationToken ct = default);

    /// <summary>
    /// Publishes multiple activities.
    /// </summary>
    /// <param name="activities">The activities to publish.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The stored activities with assigned Ids and CreatedAt.</returns>
    /// <exception cref="ActivityValidationException">Thrown if any activity fails validation.</exception>
    Task<IReadOnlyList<ActivityDto>> PublishBatchAsync(
        IReadOnlyList<ActivityDto> activities,
        CancellationToken ct = default);

    /// <summary>
    /// Queries activities with filtering and pagination.
    /// </summary>
    /// <param name="query">The query parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A page of activities with optional next cursor.</returns>
    Task<ActivityPageResult> QueryAsync(ActivityQuery query, CancellationToken ct = default);
}
