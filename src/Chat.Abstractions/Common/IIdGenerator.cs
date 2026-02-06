namespace Chat.Abstractions;

/// <summary>
/// Generates unique IDs for chat entities.
/// </summary>
public interface IIdGenerator
{
    /// <summary>
    /// Generates a new unique ID.
    /// </summary>
    string NewId();
}
