namespace ActivityStream.Abstractions;

public sealed class RemoveReactionRequest
{
    public required string TenantId { get; set; }
    public required string TargetId { get; set; }
    public required ReactionTargetKind TargetKind { get; set; }
    public EntityRefDto? Actor { get; set; }

    private string? _actorId;
    public string? ActorId
    {
        get => _actorId ?? Actor?.Id;
        set => _actorId = value;
    }
}
