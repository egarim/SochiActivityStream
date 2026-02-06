using ActivityStream.Abstractions;

namespace Media.Abstractions;

/// <summary>
/// Request to update media metadata.
/// </summary>
public sealed class UpdateMediaRequest
{
    /// <summary>
    /// Tenant ID for multi-tenancy.
    /// </summary>
    public required string TenantId { get; set; }

    /// <summary>
    /// Media item ID to update.
    /// </summary>
    public required string MediaId { get; set; }

    /// <summary>
    /// Actor performing the update.
    /// </summary>
    public required EntityRefDto Actor { get; set; }

    /// <summary>
    /// New alt text (null = no change).
    /// </summary>
    public string? AltText { get; set; }

    /// <summary>
    /// New metadata (null = no change).
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}
