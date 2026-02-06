using ActivityStream.Abstractions;

namespace RelationshipService.Abstractions;

/// <summary>
/// Query parameters for retrieving relationship edges.
/// </summary>
public sealed class RelationshipQuery
{
    /// <summary>
    /// Required tenant identifier.
    /// </summary>
    public required string TenantId { get; set; }

    /// <summary>
    /// Optional filter by the "From" entity (owner of the preference).
    /// </summary>
    public EntityRefDto? From { get; set; }

    /// <summary>
    /// Optional filter by the "To" entity (subject of the relationship).
    /// </summary>
    public EntityRefDto? To { get; set; }

    /// <summary>
    /// Optional filter by relationship kind.
    /// </summary>
    public RelationshipKind? Kind { get; set; }

    /// <summary>
    /// Optional filter by scope.
    /// </summary>
    public RelationshipScope? Scope { get; set; }

    /// <summary>
    /// Filter by active status. Defaults to true (only active edges).
    /// Set to null to include both active and inactive edges.
    /// </summary>
    public bool? IsActive { get; set; } = true;

    /// <summary>
    /// Maximum number of edges to return. Default 200.
    /// </summary>
    public int Limit { get; set; } = 200;
}
