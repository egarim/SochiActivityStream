namespace Content.Abstractions;

/// <summary>
/// Request to remove a reaction.
/// </summary>
public sealed class RemoveReactionRequest
{
    /// <summary>
    /// Tenant partition.
    /// </summary>
    public required string TenantId { get; set; }

    /// <summary>
    /// The actor removing their reaction.
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
}
