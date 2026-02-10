namespace ActivityStream.Abstractions;

public sealed class PostQuery
{
    public required string TenantId { get; set; }
    public int Limit { get; set; } = 20;
    public string? Cursor { get; set; }
    public bool IncludeDeleted { get; set; }
    public EntityRefDto? Author { get; set; }
    public EntityRefDto? Viewer { get; set; }
    public ContentVisibility? MinVisibility { get; set; }
}
