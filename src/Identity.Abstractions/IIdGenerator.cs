namespace Identity.Abstractions;

/// <summary>
/// Generates unique identifiers.
/// </summary>
public interface IIdGenerator
{
    /// <summary>
    /// Generates a new unique ID. ULID recommended for time-ordering.
    /// </summary>
    /// <returns>A new unique identifier.</returns>
    string NewId();
}
