namespace Media.Abstractions;

/// <summary>
/// Status of a media item in the upload lifecycle.
/// </summary>
public enum MediaStatus
{
    /// <summary>
    /// Upload URL generated, awaiting client upload.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Upload confirmed, file accessible.
    /// </summary>
    Ready = 1,

    /// <summary>
    /// Upload timed out or failed verification.
    /// </summary>
    Failed = 2,

    /// <summary>
    /// Soft deleted, blob pending cleanup.
    /// </summary>
    Deleted = 3
}
