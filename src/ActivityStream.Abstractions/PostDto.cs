namespace ActivityStream.Abstractions;

/// <summary>
/// Represents a post in the content system.
/// </summary>
public sealed class PostDto
{
    public string? Id { get; set; }
    public required string TenantId { get; set; }
    public required EntityRefDto Author { get; set; }
    public required string Body { get; set; }
    public List<string>? MediaIds { get; set; }
    public ContentVisibility Visibility { get; set; } = ContentVisibility.Public;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public int CommentCount { get; set; }
    public Dictionary<ReactionType, int> ReactionCounts { get; set; } = new();
    public ReactionType? ViewerReaction { get; set; }
    public List<string>? MediaUrls { get; set; }
}
