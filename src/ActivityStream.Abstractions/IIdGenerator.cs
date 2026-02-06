namespace ActivityStream.Abstractions;

/// <summary>
/// Generates unique identifiers for activities.
/// </summary>
public interface IIdGenerator
{
    /// <summary>
    /// Generates a new unique id. ULID recommended for time-ordering.
    /// </summary>
    /// <returns>A new unique identifier.</returns>
    string NewId();
}
