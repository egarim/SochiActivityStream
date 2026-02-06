namespace Content.Abstractions;

/// <summary>
/// Request to create a new post.
/// </summary>
public sealed class CreatePostRequest
{
    /// <summary>
    /// Tenant partition.
    /// </summary>
    public required string TenantId { get; set; }

    /// <summary>
    /// The author of the post.
    /// </summary>
    public required EntityRefDto Author { get; set; }

    /// <summary>
    /// The main content body.
    /// </summary>
    public required string Body { get; set; }

    /// <summary>
    /// Optional list of media IDs to attach.
    /// </summary>
    public List<string>? MediaIds { get; set; }

    /// <summary>
    /// Visibility level (defaults to Public).
    /// </summary>
    public ContentVisibility Visibility { get; set; } = ContentVisibility.Public;
}
