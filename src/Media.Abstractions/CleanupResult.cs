namespace Media.Abstractions;

/// <summary>
/// Result of a cleanup operation.
/// </summary>
public sealed class CleanupResult
{
    /// <summary>
    /// Number of expired pending uploads cleaned.
    /// </summary>
    public int ExpiredPendingCleaned { get; set; }

    /// <summary>
    /// Number of deleted blobs cleaned.
    /// </summary>
    public int DeletedBlobsCleaned { get; set; }

    /// <summary>
    /// Number of errors encountered.
    /// </summary>
    public int Errors { get; set; }
}
