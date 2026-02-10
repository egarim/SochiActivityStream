namespace ActivityStream.Abstractions;

public sealed class CreatePostRequest
{
    public required string TenantId { get; set; }
    public required EntityRefDto Author { get; set; }
    public required string Body { get; set; }
    public List<string>? MediaIds { get; set; }
    public ContentVisibility Visibility { get; set; } = ContentVisibility.Public;
}
