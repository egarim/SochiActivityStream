namespace Content.Abstractions;

/// <summary>
/// Generates unique identifiers for content entities.
/// </summary>
public interface IIdGenerator
{
    /// <summary>
    /// Generates a new unique identifier.
    /// </summary>
    string NewId();
}
