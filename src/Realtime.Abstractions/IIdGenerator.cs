namespace Realtime.Abstractions;

/// <summary>
/// Generates unique identifiers.
/// </summary>
public interface IIdGenerator
{
    /// <summary>
    /// Generates a new unique identifier.
    /// </summary>
    string NewId();
}
