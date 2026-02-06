namespace RelationshipService.Abstractions;

/// <summary>
/// Defines which part of an activity the relationship applies to.
/// </summary>
public enum RelationshipScope
{
    /// <summary>
    /// Matches if To matches Actor, any Target, or Owner.
    /// </summary>
    Any = 0,

    /// <summary>
    /// Matches only if To matches the activity's Actor.
    /// </summary>
    ActorOnly = 1,

    /// <summary>
    /// Matches only if To matches any entity in activity's Targets list.
    /// </summary>
    TargetOnly = 2,

    /// <summary>
    /// Matches only if To matches the activity's Owner (if present).
    /// </summary>
    OwnerOnly = 3
}
