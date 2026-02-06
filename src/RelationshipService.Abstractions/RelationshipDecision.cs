namespace RelationshipService.Abstractions;

/// <summary>
/// Result of a visibility decision from CanSeeAsync.
/// </summary>
/// <param name="Kind">The type of decision (Allowed, Denied, Hidden).</param>
/// <param name="Allowed">Whether the activity should be shown to the viewer.</param>
/// <param name="Reason">Human-readable reason for the decision.</param>
/// <param name="MatchedEdgeId">Optional ID of the edge that caused this decision.</param>
public sealed record RelationshipDecision(
    RelationshipDecisionKind Kind,
    bool Allowed,
    string Reason,
    string? MatchedEdgeId = null);
