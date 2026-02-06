namespace Content.Abstractions;

/// <summary>
/// The kind of entity a reaction targets.
/// </summary>
public enum ReactionTargetKind
{
    /// <summary>Reaction on a post.</summary>
    Post = 0,

    /// <summary>Reaction on a comment.</summary>
    Comment = 1
}
