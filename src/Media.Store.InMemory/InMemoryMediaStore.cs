using System.Collections.Concurrent;
using Media.Abstractions;

namespace Media.Store.InMemory;

/// <summary>
/// In-memory implementation of IMediaStore for development and testing.
/// </summary>
public sealed class InMemoryMediaStore : IMediaStore
{
    private readonly ConcurrentDictionary<string, MediaDto> _media = new();
    private readonly object _lock = new();

    /// <inheritdoc />
    public Task<MediaDto> UpsertAsync(MediaDto media, CancellationToken ct = default)
    {
        var key = GetKey(media.TenantId, media.Id!);

        lock (_lock)
        {
            _media[key] = CloneMedia(media);
        }

        return Task.FromResult(CloneMedia(media));
    }

    /// <inheritdoc />
    public Task<MediaDto?> GetByIdAsync(string tenantId, string mediaId, CancellationToken ct = default)
    {
        var key = GetKey(tenantId, mediaId);

        if (_media.TryGetValue(key, out var media))
        {
            return Task.FromResult<MediaDto?>(CloneMedia(media));
        }

        return Task.FromResult<MediaDto?>(null);
    }

    /// <inheritdoc />
    public Task<List<MediaDto>> GetByIdsAsync(string tenantId, IEnumerable<string> mediaIds, CancellationToken ct = default)
    {
        var result = new List<MediaDto>();

        foreach (var mediaId in mediaIds)
        {
            var key = GetKey(tenantId, mediaId);
            if (_media.TryGetValue(key, out var media))
            {
                result.Add(CloneMedia(media));
            }
        }

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task DeleteAsync(string tenantId, string mediaId, CancellationToken ct = default)
    {
        var key = GetKey(tenantId, mediaId);
        _media.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<MediaPageResult> QueryAsync(MediaQuery query, CancellationToken ct = default)
    {
        var items = _media.Values
            .Where(m => m.TenantId == query.TenantId)
            .Where(m => query.Owner == null || IsEntityMatch(m.Owner, query.Owner))
            .Where(m => query.Type == null || m.Type == query.Type)
            .Where(m => query.Status == null || m.Status == query.Status)
            .OrderByDescending(m => m.CreatedAt)
            .ToList();

        // Apply cursor-based pagination
        var offset = DecodeCursor(query.Cursor);
        var page = items.Skip(offset).Take(query.Limit + 1).ToList();
        var hasMore = page.Count > query.Limit;

        if (hasMore)
        {
            page = page.Take(query.Limit).ToList();
        }

        return Task.FromResult(new MediaPageResult
        {
            Items = page.Select(CloneMedia).ToList(),
            NextCursor = hasMore ? EncodeCursor(offset + query.Limit) : null,
            HasMore = hasMore
        });
    }

    /// <inheritdoc />
    public Task<List<MediaDto>> GetExpiredPendingAsync(
        string tenantId,
        DateTimeOffset expiredBefore,
        int limit = 100,
        CancellationToken ct = default)
    {
        var items = _media.Values
            .Where(m => m.TenantId == tenantId)
            .Where(m => m.Status == MediaStatus.Pending)
            .Where(m => m.UploadExpiresAt.HasValue && m.UploadExpiresAt.Value < expiredBefore)
            .Take(limit)
            .Select(CloneMedia)
            .ToList();

        return Task.FromResult(items);
    }

    /// <inheritdoc />
    public Task<List<MediaDto>> GetDeletedForCleanupAsync(
        string tenantId,
        DateTimeOffset deletedBefore,
        int limit = 100,
        CancellationToken ct = default)
    {
        var items = _media.Values
            .Where(m => m.TenantId == tenantId)
            .Where(m => m.Status == MediaStatus.Deleted)
            .Where(m => m.DeletedAt.HasValue && m.DeletedAt.Value < deletedBefore)
            .Take(limit)
            .Select(CloneMedia)
            .ToList();

        return Task.FromResult(items);
    }

    #region Private Helpers

    private static string GetKey(string tenantId, string mediaId)
        => $"{tenantId}|{mediaId}";

    private static MediaDto CloneMedia(MediaDto media)
    {
        return new MediaDto
        {
            Id = media.Id,
            TenantId = media.TenantId,
            Owner = new ActivityStream.Abstractions.EntityRefDto
            {
                Kind = media.Owner.Kind,
                Type = media.Owner.Type,
                Id = media.Owner.Id,
                Display = media.Owner.Display
            },
            Type = media.Type,
            FileName = media.FileName,
            ContentType = media.ContentType,
            SizeBytes = media.SizeBytes,
            BlobPath = media.BlobPath,
            Url = media.Url,
            ThumbnailUrl = media.ThumbnailUrl,
            ThumbnailBlobPath = media.ThumbnailBlobPath,
            Width = media.Width,
            Height = media.Height,
            DurationSeconds = media.DurationSeconds,
            Status = media.Status,
            CreatedAt = media.CreatedAt,
            ConfirmedAt = media.ConfirmedAt,
            DeletedAt = media.DeletedAt,
            UploadExpiresAt = media.UploadExpiresAt,
            AltText = media.AltText,
            Metadata = media.Metadata != null
                ? new Dictionary<string, string>(media.Metadata)
                : null
        };
    }

    private static bool IsEntityMatch(
        ActivityStream.Abstractions.EntityRefDto a,
        ActivityStream.Abstractions.EntityRefDto b)
    {
        return string.Equals(a.Kind, b.Kind, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(a.Type, b.Type, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(a.Id, b.Id, StringComparison.OrdinalIgnoreCase);
    }

    private static string EncodeCursor(int offset)
        => Convert.ToBase64String(BitConverter.GetBytes(offset));

    private static int DecodeCursor(string? cursor)
    {
        if (string.IsNullOrEmpty(cursor)) return 0;
        try
        {
            return BitConverter.ToInt32(Convert.FromBase64String(cursor), 0);
        }
        catch
        {
            return 0;
        }
    }

    #endregion
}
