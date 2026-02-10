namespace ActivityStream.Abstractions;

public sealed class CreateCommentRequest
{
    public required string TenantId { get; set; }
    public required string PostId { get; set; }
    public string? ParentCommentId { get; set; }
    public required EntityRefDto Author { get; set; }
    public required string Body { get; set; }
}
