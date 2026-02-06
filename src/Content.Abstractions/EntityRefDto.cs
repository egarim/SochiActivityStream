namespace Content.Abstractions;

/// <summary>
/// A reference to an entity (profile, group, etc.) with optional display info.
/// </summary>
public sealed class EntityRefDto
{
    /// <summary>
    /// The type of entity (e.g., "Profile", "Group").
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// The unique identifier of the entity.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Optional display name for UI rendering.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Optional image URL for avatar display.
    /// </summary>
    public string? ImageUrl { get; set; }
}
