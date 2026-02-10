namespace ActivityStream.Abstractions.RelationshipService;

public interface IRelationshipService
{
    Task<IEnumerable<RelationshipEdgeDto>> QueryAsync(RelationshipQuery query, CancellationToken ct = default);
}
