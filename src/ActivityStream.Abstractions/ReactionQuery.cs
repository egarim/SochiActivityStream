namespace ActivityStream.Abstractions;

public sealed class ReactionQuery
{
    public required string TenantId { get; set; }
    public int Limit { get; set; } = 20;
    public string? Cursor { get; set; }
    public required string TargetId { get; set; }
    public required ReactionTargetKind TargetKind { get; set; }
    public ReactionType? Type { get; set; }
}
