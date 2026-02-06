# Media Service (Azure Blob Storage) — C# Library Plan for an LLM Agent Programmer

**Goal:** Build a **Media Service** as a C# library for managing file uploads/downloads using Azure Blob Storage, with Azurite emulator support for local development and testing.

The service manages **metadata and references** to files stored in blob storage, supporting:
- Signed upload URLs (direct client-to-blob upload)
- Download URL generation
- Image thumbnails (metadata only; actual resizing is external)
- Soft delete and cleanup

> **Note:** This service does NOT perform image processing. It stores metadata and generates URLs. Image resizing/thumbnails should be handled by Azure Functions or a separate processor.

---

## 0) Definition of Done (v1 / MVP)

### 0.1 Project References

```
Media.Abstractions
  └── (no dependencies)

Media.Core
  └── Media.Abstractions
  └── Azure.Storage.Blobs (for blob operations)

Media.Store.InMemory
  └── Media.Abstractions

Media.Tests
  └── All of the above
  └── Azurite (for integration tests)
```

### 0.2 Deliverables (projects)

1. **Media.Abstractions**
   - DTOs for media items
   - Interfaces: service + store + storage provider
   - Result/Error types
   - No Azure dependencies (abstractions only)

2. **Media.Core**
   - `MediaService` implementing `IMediaService`
   - `AzureBlobStorageProvider` implementing `IMediaStorageProvider`
   - Validation and normalization
   - SAS token generation for uploads/downloads

3. **Media.Store.InMemory**
   - Reference store implementing `IMediaStore`
   - For testing without a database

4. **Media.Tests**
   - Unit tests for service logic
   - Integration tests with Azurite emulator

### 0.3 Success Criteria

- All tests green (including Azurite integration tests)
- Upload flow: request URL → client uploads → confirm → ready
- Download URLs with configurable expiry
- Soft delete with cleanup capability
- Works with both Azurite (local) and real Azure Blob Storage

---

## 1) Core Concepts

### 1.1 Metadata vs. Blobs
Media Service stores **metadata** in a database (via `IMediaStore`).
Actual files are stored in **Azure Blob Storage** (via `IMediaStorageProvider`).

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Client        │    │  Media Service  │    │  Blob Storage   │
│                 │    │                 │    │  (Azurite/Azure)│
└────────┬────────┘    └────────┬────────┘    └────────┬────────┘
         │                      │                      │
         │ 1. RequestUploadUrl  │                      │
         │─────────────────────>│                      │
         │                      │                      │
         │ 2. {mediaId, sasUrl} │                      │
         │<─────────────────────│                      │
         │                      │                      │
         │ 3. PUT blob directly │                      │
         │─────────────────────────────────────────────>
         │                      │                      │
         │ 4. ConfirmUpload     │                      │
         │─────────────────────>│                      │
         │                      │ 5. Verify exists     │
         │                      │─────────────────────>│
         │                      │                      │
         │ 6. MediaDto (Ready)  │                      │
         │<─────────────────────│                      │
```

### 1.2 Blob Naming Convention
```
{container}/{tenantId}/{year}/{month}/{mediaId}/{filename}

Example:
media/tenant-abc/2026/02/01JQXYZ123/profile-photo.jpg
```

### 1.3 Upload States
```
Pending → Ready
    ↓
  Failed (timeout)
    ↓
  Deleted (cleanup)
```

### 1.4 Security Model
- **Upload**: Client gets time-limited SAS token with write-only permissions
- **Download**: Service generates time-limited SAS token with read-only permissions
- **Private by default**: All blobs are private; access only via signed URLs

### 1.5 Content Types
Supported content types (configurable):
- Images: `image/jpeg`, `image/png`, `image/gif`, `image/webp`
- Videos: `video/mp4`, `video/webm`
- Documents: `application/pdf`

### 1.6 Size Limits
Default limits (configurable):
- Images: 10 MB
- Videos: 100 MB
- Documents: 50 MB

---

## 2) DTOs

### 2.1 MediaDto

```csharp
/// <summary>
/// Represents a media item with metadata and access URLs.
/// </summary>
public sealed class MediaDto
{
    /// <summary>
    /// Unique identifier (ULID recommended).
    /// </summary>
    public string? Id { get; set; }
    
    /// <summary>
    /// Multi-tenancy partition key.
    /// </summary>
    public required string TenantId { get; set; }
    
    /// <summary>
    /// Entity that owns this media (typically a Profile).
    /// </summary>
    public required EntityRefDto Owner { get; set; }
    
    /// <summary>
    /// Type of media (Image, Video, Document).
    /// </summary>
    public MediaType Type { get; set; }
    
    /// <summary>
    /// Original filename from upload.
    /// </summary>
    public required string FileName { get; set; }
    
    /// <summary>
    /// MIME content type (e.g., "image/jpeg").
    /// </summary>
    public required string ContentType { get; set; }
    
    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long SizeBytes { get; set; }
    
    /// <summary>
    /// Blob path in storage (internal use).
    /// </summary>
    public string? BlobPath { get; set; }
    
    /// <summary>
    /// Signed download URL (populated on read, not stored).
    /// </summary>
    public string? Url { get; set; }
    
    /// <summary>
    /// Signed thumbnail URL (if available).
    /// </summary>
    public string? ThumbnailUrl { get; set; }
    
    /// <summary>
    /// Thumbnail blob path (if generated).
    /// </summary>
    public string? ThumbnailBlobPath { get; set; }
    
    /// <summary>
    /// Image/video width in pixels.
    /// </summary>
    public int? Width { get; set; }
    
    /// <summary>
    /// Image/video height in pixels.
    /// </summary>
    public int? Height { get; set; }
    
    /// <summary>
    /// Video/audio duration in seconds.
    /// </summary>
    public double? DurationSeconds { get; set; }
    
    /// <summary>
    /// Current status of the media item.
    /// </summary>
    public MediaStatus Status { get; set; }
    
    /// <summary>
    /// When the upload URL was generated.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
    
    /// <summary>
    /// When the upload was confirmed.
    /// </summary>
    public DateTimeOffset? ConfirmedAt { get; set; }
    
    /// <summary>
    /// When the item was soft deleted.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; set; }
    
    /// <summary>
    /// Upload URL expiry (for pending items).
    /// </summary>
    public DateTimeOffset? UploadExpiresAt { get; set; }
    
    /// <summary>
    /// Optional alt text for accessibility.
    /// </summary>
    public string? AltText { get; set; }
    
    /// <summary>
    /// Optional metadata (e.g., EXIF data).
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}
```

### 2.2 MediaType

```csharp
public enum MediaType
{
    /// <summary>
    /// Image file (JPEG, PNG, GIF, WebP).
    /// </summary>
    Image = 1,
    
    /// <summary>
    /// Video file (MP4, WebM).
    /// </summary>
    Video = 2,
    
    /// <summary>
    /// Document file (PDF).
    /// </summary>
    Document = 3
}
```

### 2.3 MediaStatus

```csharp
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
```

### 2.4 UploadUrlResult

```csharp
/// <summary>
/// Result of requesting an upload URL.
/// </summary>
public sealed class UploadUrlResult
{
    /// <summary>
    /// Media item ID (use this to confirm upload).
    /// </summary>
    public required string MediaId { get; set; }
    
    /// <summary>
    /// Signed URL for direct upload to blob storage.
    /// </summary>
    public required string UploadUrl { get; set; }
    
    /// <summary>
    /// When the upload URL expires.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }
    
    /// <summary>
    /// Maximum allowed file size in bytes.
    /// </summary>
    public long MaxSizeBytes { get; set; }
    
    /// <summary>
    /// Allowed content types for this upload.
    /// </summary>
    public List<string> AllowedContentTypes { get; set; } = new();
}
```

### 2.5 EntityRefDto (from ActivityStream.Abstractions)

```csharp
// Already defined in ActivityStream.Abstractions
public class EntityRefDto
{
    public required string Kind { get; set; }
    public required string Type { get; set; }
    public required string Id { get; set; }
    public string? Display { get; set; }
}
```

---

## 3) Interfaces

### 3.1 IMediaService

```csharp
/// <summary>
/// Service for managing media uploads and metadata.
/// </summary>
public interface IMediaService
{
    /// <summary>
    /// Request a signed URL for uploading a file.
    /// Creates a Pending media record.
    /// </summary>
    Task<UploadUrlResult> RequestUploadUrlAsync(
        RequestUploadRequest request, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Confirm that an upload completed successfully.
    /// Verifies blob exists and transitions to Ready status.
    /// </summary>
    Task<MediaDto> ConfirmUploadAsync(
        string tenantId, 
        string mediaId, 
        ConfirmUploadRequest? request = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Get media metadata with signed download URL.
    /// </summary>
    Task<MediaDto?> GetMediaAsync(
        string tenantId, 
        string mediaId, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Get multiple media items by IDs.
    /// </summary>
    Task<List<MediaDto>> GetMediaBatchAsync(
        string tenantId, 
        IEnumerable<string> mediaIds, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Update media metadata (alt text, etc.).
    /// </summary>
    Task<MediaDto> UpdateMediaAsync(
        UpdateMediaRequest request, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Soft delete a media item. Blob cleanup happens later.
    /// </summary>
    Task DeleteMediaAsync(
        string tenantId, 
        string mediaId, 
        EntityRefDto actor, 
        CancellationToken ct = default);
    
    /// <summary>
    /// List media owned by an entity.
    /// </summary>
    Task<MediaPageResult> ListMediaAsync(
        MediaQuery query, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Clean up expired pending uploads and deleted items.
    /// Call periodically from a background job.
    /// </summary>
    Task<CleanupResult> CleanupAsync(
        string tenantId, 
        CleanupOptions? options = null, 
        CancellationToken ct = default);
}
```

### 3.2 IMediaStore

```csharp
/// <summary>
/// Persistence layer for media metadata.
/// </summary>
public interface IMediaStore
{
    Task<MediaDto> UpsertAsync(MediaDto media, CancellationToken ct = default);
    Task<MediaDto?> GetByIdAsync(string tenantId, string mediaId, CancellationToken ct = default);
    Task<List<MediaDto>> GetByIdsAsync(string tenantId, IEnumerable<string> mediaIds, CancellationToken ct = default);
    Task DeleteAsync(string tenantId, string mediaId, CancellationToken ct = default);
    Task<MediaPageResult> QueryAsync(MediaQuery query, CancellationToken ct = default);
    
    /// <summary>
    /// Find expired pending uploads for cleanup.
    /// </summary>
    Task<List<MediaDto>> GetExpiredPendingAsync(
        string tenantId, 
        DateTimeOffset expiredBefore, 
        int limit = 100, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Find soft-deleted items for blob cleanup.
    /// </summary>
    Task<List<MediaDto>> GetDeletedForCleanupAsync(
        string tenantId, 
        DateTimeOffset deletedBefore, 
        int limit = 100, 
        CancellationToken ct = default);
}
```

### 3.3 IMediaStorageProvider

```csharp
/// <summary>
/// Abstraction over blob storage operations.
/// Implemented by AzureBlobStorageProvider.
/// </summary>
public interface IMediaStorageProvider
{
    /// <summary>
    /// Generate a signed URL for uploading a blob.
    /// </summary>
    Task<string> GenerateUploadUrlAsync(
        string blobPath, 
        string contentType, 
        long maxSizeBytes, 
        TimeSpan expiry,
        CancellationToken ct = default);
    
    /// <summary>
    /// Generate a signed URL for downloading a blob.
    /// </summary>
    Task<string> GenerateDownloadUrlAsync(
        string blobPath, 
        TimeSpan expiry,
        CancellationToken ct = default);
    
    /// <summary>
    /// Check if a blob exists.
    /// </summary>
    Task<bool> ExistsAsync(string blobPath, CancellationToken ct = default);
    
    /// <summary>
    /// Get blob properties (size, content type).
    /// </summary>
    Task<BlobProperties?> GetPropertiesAsync(string blobPath, CancellationToken ct = default);
    
    /// <summary>
    /// Delete a blob.
    /// </summary>
    Task DeleteAsync(string blobPath, CancellationToken ct = default);
    
    /// <summary>
    /// Copy a blob (for thumbnail generation workflow).
    /// </summary>
    Task CopyAsync(string sourcePath, string destPath, CancellationToken ct = default);
}

public sealed class BlobProperties
{
    public long SizeBytes { get; set; }
    public string? ContentType { get; set; }
    public DateTimeOffset? LastModified { get; set; }
    public string? ContentMd5 { get; set; }
}
```

---

## 4) Request/Response Types

### 4.1 RequestUploadRequest

```csharp
public sealed class RequestUploadRequest
{
    /// <summary>
    /// Tenant ID for multi-tenancy.
    /// </summary>
    public required string TenantId { get; set; }
    
    /// <summary>
    /// Entity uploading the file (typically a Profile).
    /// </summary>
    public required EntityRefDto Owner { get; set; }
    
    /// <summary>
    /// Original filename (used for path and download name).
    /// </summary>
    public required string FileName { get; set; }
    
    /// <summary>
    /// MIME content type (e.g., "image/jpeg").
    /// </summary>
    public required string ContentType { get; set; }
    
    /// <summary>
    /// Expected file size in bytes (for validation).
    /// </summary>
    public long? SizeBytes { get; set; }
    
    /// <summary>
    /// Optional purpose hint (e.g., "profile-photo", "post-image").
    /// </summary>
    public string? Purpose { get; set; }
}
```

### 4.2 ConfirmUploadRequest

```csharp
public sealed class ConfirmUploadRequest
{
    /// <summary>
    /// Image/video width (client-provided).
    /// </summary>
    public int? Width { get; set; }
    
    /// <summary>
    /// Image/video height (client-provided).
    /// </summary>
    public int? Height { get; set; }
    
    /// <summary>
    /// Video/audio duration in seconds.
    /// </summary>
    public double? DurationSeconds { get; set; }
    
    /// <summary>
    /// Alt text for accessibility.
    /// </summary>
    public string? AltText { get; set; }
    
    /// <summary>
    /// Additional metadata.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}
```

### 4.3 UpdateMediaRequest

```csharp
public sealed class UpdateMediaRequest
{
    public required string TenantId { get; set; }
    public required string MediaId { get; set; }
    public required EntityRefDto Actor { get; set; }
    public string? AltText { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}
```

### 4.4 MediaQuery

```csharp
public sealed class MediaQuery
{
    public required string TenantId { get; set; }
    public EntityRefDto? Owner { get; set; }
    public MediaType? Type { get; set; }
    public MediaStatus? Status { get; set; }
    public string? Cursor { get; set; }
    public int Limit { get; set; } = 20;
}
```

### 4.5 MediaPageResult

```csharp
public sealed class MediaPageResult
{
    public List<MediaDto> Items { get; set; } = new();
    public string? NextCursor { get; set; }
    public bool HasMore { get; set; }
}
```

### 4.6 CleanupOptions and CleanupResult

```csharp
public sealed class CleanupOptions
{
    /// <summary>
    /// How long pending uploads can stay before expiring.
    /// Default: 1 hour.
    /// </summary>
    public TimeSpan PendingExpiry { get; set; } = TimeSpan.FromHours(1);
    
    /// <summary>
    /// How long after soft delete before blob is removed.
    /// Default: 7 days.
    /// </summary>
    public TimeSpan DeletedRetention { get; set; } = TimeSpan.FromDays(7);
    
    /// <summary>
    /// Maximum items to process per cleanup run.
    /// </summary>
    public int BatchSize { get; set; } = 100;
}

public sealed class CleanupResult
{
    public int ExpiredPendingCleaned { get; set; }
    public int DeletedBlobsCleaned { get; set; }
    public int Errors { get; set; }
}
```

---

## 5) Validation Rules

| Field | Rules |
|-------|-------|
| TenantId | Required, max 100 chars |
| FileName | Required, max 255 chars, valid filename chars |
| ContentType | Required, must be in allowed list |
| SizeBytes | Must be > 0 and ≤ max for type |
| AltText | Max 500 chars |
| Purpose | Max 50 chars, alphanumeric + hyphen |

### 5.1 MediaValidationError

```csharp
public enum MediaValidationError
{
    TenantIdRequired,
    TenantIdTooLong,
    OwnerRequired,
    FileNameRequired,
    FileNameTooLong,
    FileNameInvalid,
    ContentTypeRequired,
    ContentTypeNotAllowed,
    FileTooLarge,
    AltTextTooLong,
    MediaNotFound,
    MediaNotPending,
    MediaAlreadyDeleted,
    UploadNotConfirmed,
    NotAuthorized
}
```

### 5.2 MediaValidationException

```csharp
public sealed class MediaValidationException : Exception
{
    public MediaValidationError Error { get; }
    public string? Field { get; }
    
    public MediaValidationException(MediaValidationError error, string? field = null)
        : base($"Media validation failed: {error}" + (field != null ? $" ({field})" : ""))
    {
        Error = error;
        Field = field;
    }
}
```

---

## 6) Configuration

### 6.1 MediaServiceOptions

```csharp
public sealed class MediaServiceOptions
{
    /// <summary>
    /// Blob container name. Default: "media".
    /// </summary>
    public string ContainerName { get; set; } = "media";
    
    /// <summary>
    /// How long upload URLs are valid. Default: 15 minutes.
    /// </summary>
    public TimeSpan UploadUrlExpiry { get; set; } = TimeSpan.FromMinutes(15);
    
    /// <summary>
    /// How long download URLs are valid. Default: 1 hour.
    /// </summary>
    public TimeSpan DownloadUrlExpiry { get; set; } = TimeSpan.FromHours(1);
    
    /// <summary>
    /// Maximum image size in bytes. Default: 10 MB.
    /// </summary>
    public long MaxImageSizeBytes { get; set; } = 10 * 1024 * 1024;
    
    /// <summary>
    /// Maximum video size in bytes. Default: 100 MB.
    /// </summary>
    public long MaxVideoSizeBytes { get; set; } = 100 * 1024 * 1024;
    
    /// <summary>
    /// Maximum document size in bytes. Default: 50 MB.
    /// </summary>
    public long MaxDocumentSizeBytes { get; set; } = 50 * 1024 * 1024;
    
    /// <summary>
    /// Allowed content types per media type.
    /// </summary>
    public Dictionary<MediaType, List<string>> AllowedContentTypes { get; set; } = new()
    {
        [MediaType.Image] = new() { "image/jpeg", "image/png", "image/gif", "image/webp" },
        [MediaType.Video] = new() { "video/mp4", "video/webm" },
        [MediaType.Document] = new() { "application/pdf" }
    };
}
```

### 6.2 AzureBlobStorageOptions

```csharp
public sealed class AzureBlobStorageOptions
{
    /// <summary>
    /// Azure Storage connection string.
    /// For Azurite: "UseDevelopmentStorage=true"
    /// </summary>
    public required string ConnectionString { get; set; }
    
    /// <summary>
    /// Container name. Default: "media".
    /// </summary>
    public string ContainerName { get; set; } = "media";
    
    /// <summary>
    /// Whether to create container if it doesn't exist.
    /// </summary>
    public bool CreateContainerIfNotExists { get; set; } = true;
}
```

---

## 7) Implementation Details

### 7.1 MediaService

```csharp
public sealed class MediaService : IMediaService
{
    private readonly IMediaStore _store;
    private readonly IMediaStorageProvider _storage;
    private readonly IIdGenerator _idGenerator;
    private readonly MediaServiceOptions _options;
    private readonly TimeProvider _timeProvider;

    public MediaService(
        IMediaStore store,
        IMediaStorageProvider storage,
        IIdGenerator idGenerator,
        MediaServiceOptions? options = null,
        TimeProvider? timeProvider = null)
    {
        _store = store;
        _storage = storage;
        _idGenerator = idGenerator;
        _options = options ?? new MediaServiceOptions();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<UploadUrlResult> RequestUploadUrlAsync(
        RequestUploadRequest request, 
        CancellationToken ct = default)
    {
        // 1. Validate request
        Validate(request);
        
        // 2. Determine media type from content type
        var mediaType = DetermineMediaType(request.ContentType);
        var maxSize = GetMaxSize(mediaType);
        
        // 3. Generate ID and blob path
        var mediaId = _idGenerator.NewId();
        var now = _timeProvider.GetUtcNow();
        var blobPath = GenerateBlobPath(request.TenantId, mediaId, request.FileName, now);
        
        // 4. Create pending media record
        var media = new MediaDto
        {
            Id = mediaId,
            TenantId = request.TenantId,
            Owner = request.Owner,
            Type = mediaType,
            FileName = SanitizeFileName(request.FileName),
            ContentType = request.ContentType,
            SizeBytes = request.SizeBytes ?? 0,
            BlobPath = blobPath,
            Status = MediaStatus.Pending,
            CreatedAt = now,
            UploadExpiresAt = now.Add(_options.UploadUrlExpiry)
        };
        
        await _store.UpsertAsync(media, ct);
        
        // 5. Generate signed upload URL
        var uploadUrl = await _storage.GenerateUploadUrlAsync(
            blobPath, 
            request.ContentType, 
            maxSize, 
            _options.UploadUrlExpiry, 
            ct);
        
        return new UploadUrlResult
        {
            MediaId = mediaId,
            UploadUrl = uploadUrl,
            ExpiresAt = now.Add(_options.UploadUrlExpiry),
            MaxSizeBytes = maxSize,
            AllowedContentTypes = _options.AllowedContentTypes[mediaType]
        };
    }

    public async Task<MediaDto> ConfirmUploadAsync(
        string tenantId, 
        string mediaId, 
        ConfirmUploadRequest? request = null,
        CancellationToken ct = default)
    {
        // 1. Get pending media record
        var media = await _store.GetByIdAsync(tenantId, mediaId, ct)
            ?? throw new MediaValidationException(MediaValidationError.MediaNotFound);
        
        if (media.Status != MediaStatus.Pending)
            throw new MediaValidationException(MediaValidationError.MediaNotPending);
        
        // 2. Verify blob exists
        var exists = await _storage.ExistsAsync(media.BlobPath!, ct);
        if (!exists)
            throw new MediaValidationException(MediaValidationError.UploadNotConfirmed);
        
        // 3. Get actual blob properties
        var props = await _storage.GetPropertiesAsync(media.BlobPath!, ct);
        if (props != null)
        {
            media.SizeBytes = props.SizeBytes;
        }
        
        // 4. Update with client-provided metadata
        if (request != null)
        {
            media.Width = request.Width;
            media.Height = request.Height;
            media.DurationSeconds = request.DurationSeconds;
            media.AltText = request.AltText;
            media.Metadata = request.Metadata;
        }
        
        // 5. Transition to Ready
        media.Status = MediaStatus.Ready;
        media.ConfirmedAt = _timeProvider.GetUtcNow();
        media.UploadExpiresAt = null;
        
        await _store.UpsertAsync(media, ct);
        
        // 6. Generate download URL
        media.Url = await _storage.GenerateDownloadUrlAsync(
            media.BlobPath!, 
            _options.DownloadUrlExpiry, 
            ct);
        
        return media;
    }

    // ... other methods
    
    private string GenerateBlobPath(string tenantId, string mediaId, string fileName, DateTimeOffset timestamp)
    {
        var sanitizedFileName = SanitizeFileName(fileName);
        return $"{tenantId}/{timestamp.Year:D4}/{timestamp.Month:D2}/{mediaId}/{sanitizedFileName}";
    }
    
    private static string SanitizeFileName(string fileName)
    {
        // Remove path separators and other invalid chars
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(c => !invalid.Contains(c)).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "file" : sanitized;
    }
}
```

### 7.2 AzureBlobStorageProvider

```csharp
using Azure.Storage.Blobs;
using Azure.Storage.Sas;

public sealed class AzureBlobStorageProvider : IMediaStorageProvider
{
    private readonly BlobContainerClient _container;

    public AzureBlobStorageProvider(AzureBlobStorageOptions options)
    {
        var serviceClient = new BlobServiceClient(options.ConnectionString);
        _container = serviceClient.GetBlobContainerClient(options.ContainerName);
        
        if (options.CreateContainerIfNotExists)
        {
            _container.CreateIfNotExists();
        }
    }

    public Task<string> GenerateUploadUrlAsync(
        string blobPath, 
        string contentType, 
        long maxSizeBytes, 
        TimeSpan expiry,
        CancellationToken ct = default)
    {
        var blob = _container.GetBlobClient(blobPath);
        
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _container.Name,
            BlobName = blobPath,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiry)
        };
        
        sasBuilder.SetPermissions(BlobSasPermissions.Write | BlobSasPermissions.Create);
        
        var sasUri = blob.GenerateSasUri(sasBuilder);
        return Task.FromResult(sasUri.ToString());
    }

    public Task<string> GenerateDownloadUrlAsync(
        string blobPath, 
        TimeSpan expiry,
        CancellationToken ct = default)
    {
        var blob = _container.GetBlobClient(blobPath);
        
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _container.Name,
            BlobName = blobPath,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiry)
        };
        
        sasBuilder.SetPermissions(BlobSasPermissions.Read);
        
        var sasUri = blob.GenerateSasUri(sasBuilder);
        return Task.FromResult(sasUri.ToString());
    }

    public async Task<bool> ExistsAsync(string blobPath, CancellationToken ct = default)
    {
        var blob = _container.GetBlobClient(blobPath);
        var response = await blob.ExistsAsync(ct);
        return response.Value;
    }

    public async Task<BlobProperties?> GetPropertiesAsync(string blobPath, CancellationToken ct = default)
    {
        var blob = _container.GetBlobClient(blobPath);
        
        try
        {
            var props = await blob.GetPropertiesAsync(cancellationToken: ct);
            return new BlobProperties
            {
                SizeBytes = props.Value.ContentLength,
                ContentType = props.Value.ContentType,
                LastModified = props.Value.LastModified,
                ContentMd5 = props.Value.ContentHash != null 
                    ? Convert.ToBase64String(props.Value.ContentHash) 
                    : null
            };
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string blobPath, CancellationToken ct = default)
    {
        var blob = _container.GetBlobClient(blobPath);
        await blob.DeleteIfExistsAsync(cancellationToken: ct);
    }

    public async Task CopyAsync(string sourcePath, string destPath, CancellationToken ct = default)
    {
        var sourceBlob = _container.GetBlobClient(sourcePath);
        var destBlob = _container.GetBlobClient(destPath);
        
        await destBlob.StartCopyFromUriAsync(sourceBlob.Uri, cancellationToken: ct);
    }
}
```

---

## 8) Azurite Setup for Development

### 8.1 Docker Compose

```yaml
# docker-compose.yml
version: '3.8'
services:
  azurite:
    image: mcr.microsoft.com/azure-storage/azurite:latest
    container_name: azurite
    ports:
      - "10000:10000"  # Blob
      - "10001:10001"  # Queue
      - "10002:10002"  # Table
    volumes:
      - azurite-data:/data
    command: "azurite --blobHost 0.0.0.0 --queueHost 0.0.0.0 --tableHost 0.0.0.0"

volumes:
  azurite-data:
```

### 8.2 Connection Strings

```csharp
// Local development with Azurite
var localConnectionString = "UseDevelopmentStorage=true";

// Or explicit Azurite connection
var azuriteConnectionString = 
    "DefaultEndpointsProtocol=http;" +
    "AccountName=devstoreaccount1;" +
    "AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;" +
    "BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;";

// Production Azure Storage
var prodConnectionString = "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net";
```

### 8.3 Integration Test Setup

```csharp
[Collection("Azurite")]
public class MediaServiceIntegrationTests : IAsyncLifetime
{
    private readonly BlobContainerClient _container;
    private readonly IMediaService _service;

    public MediaServiceIntegrationTests()
    {
        var options = new AzureBlobStorageOptions
        {
            ConnectionString = "UseDevelopmentStorage=true",
            ContainerName = $"test-{Guid.NewGuid():N}",
            CreateContainerIfNotExists = true
        };
        
        var storage = new AzureBlobStorageProvider(options);
        var store = new InMemoryMediaStore();
        
        _service = new MediaService(
            store, 
            storage, 
            new UlidIdGenerator());
        
        _container = new BlobContainerClient(
            options.ConnectionString, 
            options.ContainerName);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _container.DeleteIfExistsAsync();
    }

    [Fact]
    public async Task UploadFlow_Success()
    {
        // 1. Request upload URL
        var uploadResult = await _service.RequestUploadUrlAsync(new RequestUploadRequest
        {
            TenantId = "tenant1",
            Owner = new EntityRefDto { Kind = "identity", Type = "Profile", Id = "p1" },
            FileName = "test.jpg",
            ContentType = "image/jpeg"
        });

        Assert.NotNull(uploadResult.UploadUrl);
        Assert.NotEmpty(uploadResult.MediaId);

        // 2. Simulate client upload (using Azure SDK directly)
        var blob = _container.GetBlobClient(/* extract path from URL */);
        await blob.UploadAsync(new MemoryStream(new byte[] { 1, 2, 3 }));

        // 3. Confirm upload
        var media = await _service.ConfirmUploadAsync(
            "tenant1", 
            uploadResult.MediaId);

        Assert.Equal(MediaStatus.Ready, media.Status);
        Assert.NotNull(media.Url);
    }
}
```

---

## 9) DI Registration

```csharp
public static class MediaServiceExtensions
{
    public static IServiceCollection AddMediaService(
        this IServiceCollection services,
        Action<MediaServiceOptions>? configureOptions = null,
        Action<AzureBlobStorageOptions>? configureStorage = null)
    {
        var mediaOptions = new MediaServiceOptions();
        configureOptions?.Invoke(mediaOptions);
        services.AddSingleton(mediaOptions);
        
        var storageOptions = new AzureBlobStorageOptions
        {
            ConnectionString = "UseDevelopmentStorage=true"
        };
        configureStorage?.Invoke(storageOptions);
        services.AddSingleton(storageOptions);
        
        services.AddSingleton<IMediaStorageProvider, AzureBlobStorageProvider>();
        services.AddSingleton<IMediaStore, InMemoryMediaStore>();
        services.AddSingleton<IMediaService, MediaService>();
        
        return services;
    }
}

// Usage in Program.cs
builder.Services.AddMediaService(
    options => 
    {
        options.MaxImageSizeBytes = 5 * 1024 * 1024; // 5 MB
    },
    storage => 
    {
        storage.ConnectionString = builder.Environment.IsDevelopment()
            ? "UseDevelopmentStorage=true"
            : builder.Configuration.GetConnectionString("AzureStorage")!;
    });
```

---

## 10) Implementation Order

| Step | Task | Time |
|------|------|------|
| 1 | Create `Media.Abstractions` project with DTOs + interfaces | 0.5 day |
| 2 | Create `Media.Core` project with `MediaService` | 0.5 day |
| 3 | Implement `AzureBlobStorageProvider` | 0.5 day |
| 4 | Create `Media.Store.InMemory` | 0.25 day |
| 5 | Unit tests for `MediaService` | 0.25 day |
| 6 | Integration tests with Azurite | 0.5 day |
| 7 | DI extensions and documentation | 0.25 day |
| **Total** | | **2.75 days** |

---

## 11) Next Steps

1. Create project structure
2. Implement DTOs and interfaces (Abstractions)
3. Implement MediaService and AzureBlobStorageProvider (Core)
4. Implement InMemoryMediaStore
5. Write unit and integration tests
6. Add Docker Compose for Azurite
7. Wire up to demo project
