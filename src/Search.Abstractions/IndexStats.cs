namespace Search.Abstractions;

/// <summary>
/// Index statistics.
/// </summary>
public sealed class IndexStats
{
    /// <summary>
    /// Total document count.
    /// </summary>
    public long DocumentCount { get; set; }

    /// <summary>
    /// Document count by type.
    /// </summary>
    public Dictionary<string, long> CountByType { get; set; } = new();

    /// <summary>
    /// Index size in bytes (if available).
    /// </summary>
    public long? SizeBytes { get; set; }

    /// <summary>
    /// When the index was last updated.
    /// </summary>
    public DateTimeOffset? LastUpdated { get; set; }
}
