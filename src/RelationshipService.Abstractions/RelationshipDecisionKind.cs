namespace RelationshipService.Abstractions;

/// <summary>
/// The kind of decision returned by CanSeeAsync.
/// </summary>
public enum RelationshipDecisionKind
{
    /// <summary>
    /// The activity is allowed to be shown.
    /// </summary>
    Allowed = 0,

    /// <summary>
    /// The activity is explicitly denied (hard block or deny rule).
    /// </summary>
    Denied = 1,

    /// <summary>
    /// The activity is hidden (e.g., mute) - not forbidden but hidden from feeds.
    /// </summary>
    Hidden = 2
}
