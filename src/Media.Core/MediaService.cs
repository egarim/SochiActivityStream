using ActivityStream.Abstractions;
using Media.Abstractions;

namespace Media.Core;

/// <summary>
/// Implementation of IMediaService for managing media uploads and metadata.
/// </summary>
public sealed class MediaService : IMediaService
{
    private readonly IMediaStore _store;
    private readonly IMediaStorageProvider _storage;
    private readonly IIdGenerator _idGenerator;
    private readonly MediaServiceOptions _options;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Creates a new MediaService.
    /// </summary>
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

    /// <inheritdoc />
    public async Task<UploadUrlResult> RequestUploadUrlAsync(
        RequestUploadRequest request,
        CancellationToken ct = default)
    {
        // 1. Validate request
        ValidateUploadRequest(request);

        // 2. Determine media type from content type
        var mediaType = DetermineMediaType(request.ContentType);
        var maxSize = _options.GetMaxSize(mediaType);

        // Validate size if provided
        if (request.SizeBytes.HasValue && request.SizeBytes.Value > maxSize)
        {
            throw new MediaValidationException(MediaValidationError.FileTooLarge);
        }

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
            AllowedContentTypes = _options.AllowedContentTypes[mediaType],
            BlobPath = blobPath
        };
    }

    /// <inheritdoc />
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
            if (request.AltText != null && request.AltText.Length > 500)
            {
                throw new MediaValidationException(MediaValidationError.AltTextTooLong);
            }

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

    /// <inheritdoc />
    public async Task<MediaDto?> GetMediaAsync(
        string tenantId,
        string mediaId,
        CancellationToken ct = default)
    {
        var media = await _store.GetByIdAsync(tenantId, mediaId, ct);
        if (media == null) return null;

        // Generate download URL if ready
        if (media.Status == MediaStatus.Ready && media.BlobPath != null)
        {
            media.Url = await _storage.GenerateDownloadUrlAsync(
                media.BlobPath,
                _options.DownloadUrlExpiry,
                ct);

            if (media.ThumbnailBlobPath != null)
            {
                media.ThumbnailUrl = await _storage.GenerateDownloadUrlAsync(
                    media.ThumbnailBlobPath,
                    _options.DownloadUrlExpiry,
                    ct);
            }
        }

        return media;
    }

    /// <inheritdoc />
    public async Task<List<MediaDto>> GetMediaBatchAsync(
        string tenantId,
        IEnumerable<string> mediaIds,
        CancellationToken ct = default)
    {
        var mediaList = await _store.GetByIdsAsync(tenantId, mediaIds, ct);

        // Generate download URLs for ready items
        foreach (var media in mediaList.Where(m => m.Status == MediaStatus.Ready && m.BlobPath != null))
        {
            media.Url = await _storage.GenerateDownloadUrlAsync(
                media.BlobPath!,
                _options.DownloadUrlExpiry,
                ct);

            if (media.ThumbnailBlobPath != null)
            {
                media.ThumbnailUrl = await _storage.GenerateDownloadUrlAsync(
                    media.ThumbnailBlobPath,
                    _options.DownloadUrlExpiry,
                    ct);
            }
        }

        return mediaList;
    }

    /// <inheritdoc />
    public async Task<MediaDto> UpdateMediaAsync(
        UpdateMediaRequest request,
        CancellationToken ct = default)
    {
        var media = await _store.GetByIdAsync(request.TenantId, request.MediaId, ct)
            ?? throw new MediaValidationException(MediaValidationError.MediaNotFound);

        // Check authorization (only owner can update)
        if (!IsOwner(media, request.Actor))
        {
            throw new MediaValidationException(MediaValidationError.NotAuthorized);
        }

        if (media.Status == MediaStatus.Deleted)
        {
            throw new MediaValidationException(MediaValidationError.MediaAlreadyDeleted);
        }

        // Validate alt text
        if (request.AltText != null && request.AltText.Length > 500)
        {
            throw new MediaValidationException(MediaValidationError.AltTextTooLong);
        }

        // Update fields
        if (request.AltText != null)
        {
            media.AltText = request.AltText;
        }

        if (request.Metadata != null)
        {
            media.Metadata = request.Metadata;
        }

        await _store.UpsertAsync(media, ct);

        // Generate download URL if ready
        if (media.Status == MediaStatus.Ready && media.BlobPath != null)
        {
            media.Url = await _storage.GenerateDownloadUrlAsync(
                media.BlobPath,
                _options.DownloadUrlExpiry,
                ct);
        }

        return media;
    }

    /// <inheritdoc />
    public async Task DeleteMediaAsync(
        string tenantId,
        string mediaId,
        EntityRefDto actor,
        CancellationToken ct = default)
    {
        var media = await _store.GetByIdAsync(tenantId, mediaId, ct)
            ?? throw new MediaValidationException(MediaValidationError.MediaNotFound);

        // Check authorization (only owner can delete)
        if (!IsOwner(media, actor))
        {
            throw new MediaValidationException(MediaValidationError.NotAuthorized);
        }

        if (media.Status == MediaStatus.Deleted)
        {
            return; // Already deleted, idempotent
        }

        // Soft delete
        media.Status = MediaStatus.Deleted;
        media.DeletedAt = _timeProvider.GetUtcNow();

        await _store.UpsertAsync(media, ct);
    }

    /// <inheritdoc />
    public async Task<MediaPageResult> ListMediaAsync(
        MediaQuery query,
        CancellationToken ct = default)
    {
        var result = await _store.QueryAsync(query, ct);

        // Generate download URLs for ready items
        foreach (var media in result.Items.Where(m => m.Status == MediaStatus.Ready && m.BlobPath != null))
        {
            media.Url = await _storage.GenerateDownloadUrlAsync(
                media.BlobPath!,
                _options.DownloadUrlExpiry,
                ct);

            if (media.ThumbnailBlobPath != null)
            {
                media.ThumbnailUrl = await _storage.GenerateDownloadUrlAsync(
                    media.ThumbnailBlobPath,
                    _options.DownloadUrlExpiry,
                    ct);
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<CleanupResult> CleanupAsync(
        string tenantId,
        CleanupOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new CleanupOptions();
        var result = new CleanupResult();
        var now = _timeProvider.GetUtcNow();

        // 1. Clean up expired pending uploads
        var expiredPending = await _store.GetExpiredPendingAsync(
            tenantId,
            now.Subtract(options.PendingExpiry),
            options.BatchSize,
            ct);

        foreach (var media in expiredPending)
        {
            try
            {
                // Delete blob if exists
                if (media.BlobPath != null)
                {
                    await _storage.DeleteAsync(media.BlobPath, ct);
                }

                // Mark as Failed and delete record
                media.Status = MediaStatus.Failed;
                await _store.DeleteAsync(tenantId, media.Id!, ct);
                result.ExpiredPendingCleaned++;
            }
            catch
            {
                result.Errors++;
            }
        }

        // 2. Clean up deleted items past retention
        var deletedItems = await _store.GetDeletedForCleanupAsync(
            tenantId,
            now.Subtract(options.DeletedRetention),
            options.BatchSize,
            ct);

        foreach (var media in deletedItems)
        {
            try
            {
                // Delete blob
                if (media.BlobPath != null)
                {
                    await _storage.DeleteAsync(media.BlobPath, ct);
                }

                // Delete thumbnail blob
                if (media.ThumbnailBlobPath != null)
                {
                    await _storage.DeleteAsync(media.ThumbnailBlobPath, ct);
                }

                // Delete record
                await _store.DeleteAsync(tenantId, media.Id!, ct);
                result.DeletedBlobsCleaned++;
            }
            catch
            {
                result.Errors++;
            }
        }

        return result;
    }

    #region Private Helpers

    private void ValidateUploadRequest(RequestUploadRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TenantId))
            throw new MediaValidationException(MediaValidationError.TenantIdRequired);

        if (request.TenantId.Length > 100)
            throw new MediaValidationException(MediaValidationError.TenantIdTooLong);

        if (request.Owner == null)
            throw new MediaValidationException(MediaValidationError.OwnerRequired);

        if (string.IsNullOrWhiteSpace(request.FileName))
            throw new MediaValidationException(MediaValidationError.FileNameRequired);

        if (request.FileName.Length > 255)
            throw new MediaValidationException(MediaValidationError.FileNameTooLong);

        if (string.IsNullOrWhiteSpace(request.ContentType))
            throw new MediaValidationException(MediaValidationError.ContentTypeRequired);

        // Check if content type is allowed
        if (!IsContentTypeAllowed(request.ContentType))
            throw new MediaValidationException(MediaValidationError.ContentTypeNotAllowed);
    }

    private bool IsContentTypeAllowed(string contentType)
    {
        return _options.AllowedContentTypes.Values
            .Any(types => types.Contains(contentType, StringComparer.OrdinalIgnoreCase));
    }

    private MediaType DetermineMediaType(string contentType)
    {
        foreach (var kvp in _options.AllowedContentTypes)
        {
            if (kvp.Value.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            {
                return kvp.Key;
            }
        }

        // Default to Image if unknown (validation should catch this before)
        return MediaType.Image;
    }

    private static string GenerateBlobPath(string tenantId, string mediaId, string fileName, DateTimeOffset timestamp)
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

    private static bool IsOwner(MediaDto media, EntityRefDto actor)
    {
        return string.Equals(media.Owner.Kind, actor.Kind, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(media.Owner.Type, actor.Type, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(media.Owner.Id, actor.Id, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    /// <inheritdoc />
    public async Task<MediaDto> UploadFromBytesAsync(
        string tenantId,
        EntityRefDto owner,
        string fileName,
        string contentType,
        byte[] data,
        CancellationToken ct = default)
    {
        // 1. Validate inputs
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Tenant ID is required", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required", nameof(fileName));
        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("Content type is required", nameof(contentType));
        if (data == null || data.Length == 0)
            throw new ArgumentException("Data is required", nameof(data));

        if (!IsContentTypeAllowed(contentType))
            throw new MediaValidationException(MediaValidationError.ContentTypeNotAllowed);

        // 2. Validate size
        var mediaType = DetermineMediaType(contentType);
        var maxSize = _options.GetMaxSize(mediaType);
        if (data.Length > maxSize)
            throw new MediaValidationException(MediaValidationError.FileTooLarge);

        // 3. Generate ID and blob path
        var mediaId = _idGenerator.NewId();
        var now = _timeProvider.GetUtcNow();
        var blobPath = GenerateBlobPath(tenantId, mediaId, fileName, now);

        // 4. Upload to blob storage
        await _storage.UploadBytesAsync(blobPath, data, contentType, ct);

        // 5. Create media record
        var media = new MediaDto
        {
            Id = mediaId,
            TenantId = tenantId,
            Owner = owner,
            Type = mediaType,
            FileName = SanitizeFileName(fileName),
            ContentType = contentType,
            SizeBytes = data.Length,
            BlobPath = blobPath,
            Status = MediaStatus.Ready,
            CreatedAt = now,
            ConfirmedAt = now
        };

        await _store.UpsertAsync(media, ct);

        // 6. Generate download URL
        media.Url = await _storage.GenerateDownloadUrlAsync(
            blobPath,
            _options.DownloadUrlExpiry,
            ct);

        return media;
    }
}
