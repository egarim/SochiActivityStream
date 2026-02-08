namespace Content.Abstractions;

/// <summary>
/// Represents a reaction on a post or comment.
/// </summary>
public sealed class ReactionDto
{
    /// <summary>
    /// Unique identifier for the reaction.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Tenant partition.
    /// </summary>
    public required string TenantId { get; set; }

    /// <summary>
    /// The actor who reacted.
    /// </summary>
    public required EntityRefDto Actor { get; set; }

    /// <summary>
    /// Denormalized Actor ID for unique constraint indexing.
    /// </summary>
    public string? ActorId { get; set; }

    /// <summary>
    /// The ID of the target (post or comment).
    /// </summary>
    public required string TargetId { get; set; }

    /// <summary>
    /// The kind of target (Post or Comment).
    /// </summary>
    public required ReactionTargetKind TargetKind { get; set; }

    /// <summary>
    /// The type of reaction.
    /// </summary>
    public required ReactionType Type { get; set; }

    /// <summary>
    /// When the reaction was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
