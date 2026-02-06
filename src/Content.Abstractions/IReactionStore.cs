namespace Content.Abstractions;

/// <summary>
/// Storage interface for reactions. Implementations handle persistence.
/// </summary>
public interface IReactionStore
{
    /// <summary>
    /// Upserts a reaction (insert or update).
    /// </summary>
    Task<ReactionDto> UpsertAsync(ReactionDto reaction, CancellationToken ct = default);

    /// <summary>
    /// Gets a reaction by actor and target.
    /// </summary>
    Task<ReactionDto?> GetAsync(string tenantId, string targetId, ReactionTargetKind targetKind, string actorId, CancellationToken ct = default);

    /// <summary>
    /// Queries reactions with filtering and pagination.
    /// </summary>
    Task<ContentPageResult<ReactionDto>> QueryAsync(ReactionQuery query, CancellationToken ct = default);

    /// <summary>
    /// Deletes a reaction by actor and target.
    /// </summary>
    Task DeleteAsync(string tenantId, string targetId, ReactionTargetKind targetKind, string actorId, CancellationToken ct = default);

    /// <summary>
    /// Gets reaction counts by type for a target.
    /// </summary>
    Task<Dictionary<ReactionType, int>> GetCountsAsync(string tenantId, string targetId, ReactionTargetKind targetKind, CancellationToken ct = default);
}
