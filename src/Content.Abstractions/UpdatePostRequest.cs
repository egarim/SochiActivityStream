namespace Content.Abstractions;

/// <summary>
/// Request to update an existing post.
/// </summary>
public sealed class UpdatePostRequest
{
    /// <summary>
    /// Tenant partition.
    /// </summary>
    public required string TenantId { get; set; }

    /// <summary>
    /// The post ID to update.
    /// </summary>
    public required string PostId { get; set; }

    /// <summary>
    /// The actor performing the update (must be the author).
    /// </summary>
    public required EntityRefDto Actor { get; set; }

    /// <summary>
    /// New body text (null to keep existing).
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// New visibility (null to keep existing).
    /// </summary>
    public ContentVisibility? Visibility { get; set; }
}
