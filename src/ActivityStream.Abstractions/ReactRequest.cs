namespace ActivityStream.Abstractions;

public sealed class ReactRequest
{
    public required string TenantId { get; set; }
    public required string TargetId { get; set; }
    public required ReactionTargetKind TargetKind { get; set; }
    public required ReactionType Type { get; set; }
    public required EntityRefDto Actor { get; set; }

    private string? _actorId;
    /// <summary>
    /// Accept either an explicit ActorId or derive from Actor.Id.
    /// Backwards-compatible with older initializers.
    /// </summary>
    public string? ActorId
    {
        get => _actorId ?? Actor?.Id;
        set => _actorId = value;
    }
}
