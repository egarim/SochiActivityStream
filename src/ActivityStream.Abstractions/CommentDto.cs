namespace ActivityStream.Abstractions;

public sealed class CommentDto
{
    public string? Id { get; set; }
    public required string TenantId { get; set; }
    public required string PostId { get; set; }
    public string? ParentCommentId { get; set; }
    public required EntityRefDto Author { get; set; }
    public required string Body { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public int ReplyCount { get; set; }
    public Dictionary<ReactionType, int> ReactionCounts { get; set; } = new();
    public ReactionType? ViewerReaction { get; set; }
}
