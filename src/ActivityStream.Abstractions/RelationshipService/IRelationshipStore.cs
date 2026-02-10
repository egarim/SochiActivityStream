namespace ActivityStream.Abstractions.RelationshipService;

public interface IRelationshipStore
{
    Task AddAsync(RelationshipEdgeDto edge, CancellationToken ct = default);
}
