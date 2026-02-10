namespace ActivityStream.Abstractions;

public sealed class ReactionDto
{
    public string? Id { get; set; }
    public required string TenantId { get; set; }
    public required EntityRefDto Actor { get; set; }
    private string? _actorId;
    public string? ActorId
    {
        get => _actorId ?? Actor?.Id;
        set => _actorId = value;
    }
    public required string TargetId { get; set; }
    public required ReactionTargetKind TargetKind { get; set; }
    public required ReactionType Type { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
