namespace ActivityStream.Abstractions.RelationshipService;

public sealed class RelationshipEdgeDto
{
    public string? Id { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }
    public RelationshipKind Kind { get; set; }
}
