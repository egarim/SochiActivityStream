namespace Chat.Abstractions;

/// <summary>
/// Reference to a profile/user entity.
/// </summary>
public sealed class EntityRefDto
{
    public required string Id { get; set; }
    public required string Type { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }

    public static EntityRefDto Profile(string id, string? displayName = null) =>
        new() { Id = id, Type = "profile", DisplayName = displayName };

    public override bool Equals(object? obj) =>
        obj is EntityRefDto other && Id == other.Id && Type == other.Type;

    public override int GetHashCode() => HashCode.Combine(Id, Type);
}
