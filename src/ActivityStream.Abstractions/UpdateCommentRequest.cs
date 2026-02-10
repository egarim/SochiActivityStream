namespace ActivityStream.Abstractions;

public sealed class UpdateCommentRequest
{
    public required string TenantId { get; set; }
    public required string CommentId { get; set; }
    public EntityRefDto? Actor { get; set; }

    private string? _actorId;
    public string? ActorId
    {
        get => _actorId ?? Actor?.Id;
        set => _actorId = value;
    }
    public string? Body { get; set; }
}
