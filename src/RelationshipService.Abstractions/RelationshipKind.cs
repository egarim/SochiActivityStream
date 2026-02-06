namespace RelationshipService.Abstractions;

/// <summary>
/// Defines the type of relationship between entities.
/// </summary>
public enum RelationshipKind
{
    /// <summary>
    /// Viewer wants to see posts by/related to the target entity.
    /// Used for feed building; does NOT affect visibility decisions.
    /// </summary>
    Follow = 0,

    /// <summary>
    /// Viewer wants updates about a specific target/entity timeline.
    /// Used for feed building; does NOT affect visibility decisions.
    /// </summary>
    Subscribe = 1,

    /// <summary>
    /// Hard deny - strongest relationship that blocks visibility.
    /// </summary>
    Block = 2,

    /// <summary>
    /// Soft hide - hides activities from feeds but not forbidden.
    /// </summary>
    Mute = 3,

    /// <summary>
    /// Explicit allow rule - does not override Block or Deny.
    /// </summary>
    Allow = 4,

    /// <summary>
    /// Rule-based deny - stronger than Mute and Allow.
    /// </summary>
    Deny = 5
}
