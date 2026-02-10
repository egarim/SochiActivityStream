namespace ActivityStream.Abstractions;

public sealed class UpdatePostRequest
{
    public required string TenantId { get; set; }
    public required string PostId { get; set; }
    public EntityRefDto? Actor { get; set; }

    private string? _actorId;
    public string? ActorId
    {
        get => _actorId ?? Actor?.Id;
        set => _actorId = value;
    }
    public string? Body { get; set; }
    public List<string>? MediaIds { get; set; }
    public ContentVisibility? Visibility { get; set; }
}
