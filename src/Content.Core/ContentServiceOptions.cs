namespace Content.Core;

/// <summary>
/// Configuration options for ContentService.
/// </summary>
public sealed class ContentServiceOptions
{
    /// <summary>
    /// Maximum allowed length for post body text.
    /// </summary>
    public int MaxPostBodyLength { get; set; } = 10_000;

    /// <summary>
    /// Maximum allowed length for comment body text.
    /// </summary>
    public int MaxCommentBodyLength { get; set; } = 5_000;

    /// <summary>
    /// Default page size for queries.
    /// </summary>
    public int DefaultPageSize { get; set; } = 20;

    /// <summary>
    /// Maximum page size for queries.
    /// </summary>
    public int MaxPageSize { get; set; } = 100;
}
