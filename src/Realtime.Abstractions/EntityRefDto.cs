namespace Realtime.Abstractions;

/// <summary>
/// A reference to an entity (profile, group, etc.) for targeting events.
/// </summary>
public sealed class EntityRefDto
{
    /// <summary>
    /// The entity's unique identifier.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// The type of entity (e.g., "profile", "group").
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Optional display name for logging/debugging.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Creates a profile reference.
    /// </summary>
    public static EntityRefDto Profile(string id, string? displayName = null) =>
        new() { Id = id, Type = "profile", DisplayName = displayName };

    /// <summary>
    /// Creates a group reference.
    /// </summary>
    public static EntityRefDto Group(string id, string? displayName = null) =>
        new() { Id = id, Type = "group", DisplayName = displayName };

    public override bool Equals(object? obj) =>
        obj is EntityRefDto other && Id == other.Id && Type == other.Type;

    public override int GetHashCode() => HashCode.Combine(Id, Type);
}
