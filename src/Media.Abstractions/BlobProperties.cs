namespace Media.Abstractions;

/// <summary>
/// Blob properties returned from storage.
/// </summary>
public sealed class BlobProperties
{
    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// MIME content type.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// When the blob was last modified.
    /// </summary>
    public DateTimeOffset? LastModified { get; set; }

    /// <summary>
    /// MD5 hash of the content (base64 encoded).
    /// </summary>
    public string? ContentMd5 { get; set; }
}
