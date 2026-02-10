namespace ActivityStream.Abstractions;

public interface IReactionStore
{
    Task<ReactionDto> UpsertAsync(ReactionDto reaction, CancellationToken ct = default);
    Task<ReactionDto?> GetAsync(string tenantId, string targetId, ReactionTargetKind kind, string actorId, CancellationToken ct = default);
    Task DeleteAsync(string tenantId, string targetId, ReactionTargetKind kind, string actorId, CancellationToken ct = default);
    Task<ContentPageResult<ReactionDto>> QueryAsync(ReactionQuery query, CancellationToken ct = default);
    Task<Dictionary<ReactionType, int>> GetCountsAsync(string tenantId, string targetId, ReactionTargetKind targetKind, CancellationToken ct = default);
}
