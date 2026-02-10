namespace ActivityStream.Abstractions;

public sealed class DeleteCommentRequest
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
}
