namespace Content.Abstractions;

/// <summary>
/// Request to delete a post.
/// </summary>
public sealed class DeletePostRequest
{
    /// <summary>
    /// Tenant partition.
    /// </summary>
    public required string TenantId { get; set; }

    /// <summary>
    /// The post ID to delete.
    /// </summary>
    public required string PostId { get; set; }

    /// <summary>
    /// The actor performing the delete (must be the author).
    /// </summary>
    public required EntityRefDto Actor { get; set; }
}
