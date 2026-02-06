namespace Content.Abstractions;

/// <summary>
/// Request to add or change a reaction.
/// </summary>
public sealed class ReactRequest
{
    /// <summary>
    /// Tenant partition.
    /// </summary>
    public required string TenantId { get; set; }

    /// <summary>
    /// The actor reacting.
    /// </summary>
    public required EntityRefDto Actor { get; set; }

    /// <summary>
    /// The target ID (post or comment).
    /// </summary>
    public required string TargetId { get; set; }

    /// <summary>
    /// The kind of target.
    /// </summary>
    public required ReactionTargetKind TargetKind { get; set; }

    /// <summary>
    /// The type of reaction.
    /// </summary>
    public required ReactionType Type { get; set; }
}
