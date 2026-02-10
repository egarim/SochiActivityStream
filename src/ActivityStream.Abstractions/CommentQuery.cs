namespace ActivityStream.Abstractions;

public sealed class CommentQuery
{
    public required string TenantId { get; set; }
    public int Limit { get; set; } = 20;
    public string? Cursor { get; set; }
    public bool IncludeDeleted { get; set; }
    public string? PostId { get; set; }
    public string? ParentCommentId { get; set; }
    public EntityRefDto? Viewer { get; set; }
}
