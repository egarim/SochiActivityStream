namespace ActivityStream.Abstractions;

/// <summary>
/// A reference to an entity (profile, group, etc.) with optional display info.
/// </summary>
public sealed class EntityRefDto
{
    /// <summary>
    /// The type of entity (e.g., "Profile", "Group").
    /// </summary>
    private string? _type;
    public required string Type
    {
        get => _type!;
        set => _type = value;
    }

    /// <summary>
    /// The unique identifier of the entity.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Optional display name for UI rendering.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Backwards-compatible alias for `Type` used in older projects.
    /// </summary>
    private string? _kind;
    public string Kind
    {
        // If _kind was explicitly set (even to empty string) preserve it; otherwise derive from Type.
        get => _kind != null ? _kind : (_type?.ToLowerInvariant() ?? string.Empty);
        set => _kind = value;
    }

    /// <summary>
    /// Backwards-compatible alias for `DisplayName` used in older projects.
    /// </summary>
    public string? Display
    {
        get => DisplayName;
        set => DisplayName = value;
    }

    /// <summary>
    /// Optional image URL for avatar display.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Backwards-compatible alias for `ImageUrl` used by older code paths.
    /// </summary>
    public string? AvatarUrl
    {
        get => ImageUrl;
        set => ImageUrl = value;
    }

    /// <summary>
    /// Arbitrary metadata bag for consumers that need to attach extra info.
    /// Kept for backwards compatibility with older projects that used `Meta`.
    /// </summary>
    public Dictionary<string, object?>? Meta { get; set; }

    /// <summary>
    /// Creates a profile reference (convenience factory).
    /// </summary>
    public static EntityRefDto Profile(string id, string? displayName = null) =>
        new() { Id = id, Type = "Profile", DisplayName = displayName };

    /// <summary>
    /// Creates a group reference (convenience factory).
    /// </summary>
    public static EntityRefDto Group(string id, string? displayName = null) =>
        new() { Id = id, Type = "Group", DisplayName = displayName };
}
