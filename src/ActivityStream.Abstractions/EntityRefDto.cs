namespace ActivityStream.Abstractions;

/// <summary>
/// Universal pointer to any entity in the system.
/// </summary>
public class EntityRefDto
{
    /// <summary>
    /// Broad category: user | service | system | object | ai | tenant | etc
    /// </summary>
    public required string Kind { get; set; }

    /// <summary>
    /// Concrete type: User | Invoice | Project | Agent | Build | etc
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Identifier within the type.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Optional human label.
    /// </summary>
    public string? Display { get; set; }

    /// <summary>
    /// Optional metadata (never required for identity).
    /// </summary>
    public Dictionary<string, object?>? Meta { get; set; }
}
