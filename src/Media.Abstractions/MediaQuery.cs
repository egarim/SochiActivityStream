using ActivityStream.Abstractions;

namespace Media.Abstractions;

/// <summary>
/// Query parameters for listing media.
/// </summary>
public sealed class MediaQuery
{
    /// <summary>
    /// Tenant ID for multi-tenancy.
    /// </summary>
    public required string TenantId { get; set; }

    /// <summary>
    /// Filter by owner entity.
    /// </summary>
    public EntityRefDto? Owner { get; set; }

    /// <summary>
    /// Filter by media type.
    /// </summary>
    public MediaType? Type { get; set; }

    /// <summary>
    /// Filter by status.
    /// </summary>
    public MediaStatus? Status { get; set; }

    /// <summary>
    /// Pagination cursor.
    /// </summary>
    public string? Cursor { get; set; }

    /// <summary>
    /// Maximum items to return.
    /// </summary>
    public int Limit { get; set; } = 20;
}
