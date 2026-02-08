using Media.Abstractions;
using Microsoft.EntityFrameworkCore;
using BlazorBook.Web.Data;

namespace BlazorBook.Web.Stores.EFCore;

/// <summary>
/// EF Core implementation of IMediaStore
/// </summary>
public class EFCoreMediaStore : IMediaStore
{
    private readonly ApplicationDbContext _context;

    public EFCoreMediaStore(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<MediaDto> UpsertAsync(MediaDto media, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(media);

        if (string.IsNullOrEmpty(media.Id))
        {
            media.Id = Guid.NewGuid().ToString();
        }

        var existing = await _context.Media
            .FirstOrDefaultAsync(m => m.Id == media.Id && m.TenantId == media.TenantId, ct);

        if (existing == null)
        {
            _context.Media.Add(media);
        }
        else
        {
            existing.Status = media.Status;
            existing.BlobPath = media.BlobPath;
            existing.ThumbnailBlobPath = media.ThumbnailBlobPath;
            existing.Width = media.Width;
            existing.Height = media.Height;
            existing.DurationSeconds = media.DurationSeconds;
            existing.AltText = media.AltText;
            existing.DeletedAt = media.DeletedAt;
        }

        await _context.SaveChangesAsync(ct);
        return media;
    }

    public async Task<MediaDto?> GetByIdAsync(string tenantId, string mediaId, CancellationToken ct = default)
    {
        return await _context.Media
            .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.Id == mediaId, ct);
    }

    public async Task<List<MediaDto>> GetByIdsAsync(string tenantId, IEnumerable<string> mediaIds, CancellationToken ct = default)
    {
        var idList = mediaIds.ToList();
        return await _context.Media
            .Where(m => m.TenantId == tenantId && idList.Contains(m.Id!))
            .ToListAsync(ct);
    }

    public async Task DeleteAsync(string tenantId, string mediaId, CancellationToken ct = default)
    {
        var media = await _context.Media
            .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.Id == mediaId, ct);

        if (media != null)
        {
            _context.Media.Remove(media);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<MediaPageResult> QueryAsync(MediaQuery query, CancellationToken ct = default)
    {
        var queryable = _context.Media
            .Where(m => m.TenantId == query.TenantId)
            .AsQueryable();

        // Filter by owner
        if (query.Owner != null)
        {
            queryable = queryable.Where(m => m.Owner.Id == query.Owner.Id);
        }

        // Filter by type
        if (query.Type.HasValue)
        {
            queryable = queryable.Where(m => m.Type == query.Type.Value);
        }

        // Filter by status
        if (query.Status.HasValue)
        {
            queryable = queryable.Where(m => m.Status == query.Status.Value);
        }

        // Client-side sorting due to SQLite DateTimeOffset limitation
        var items = await queryable.ToListAsync(ct);
        
        items = items
            .OrderByDescending(m => m.CreatedAt)
            .Take(query.Limit + 1)
            .ToList();

        var hasMore = items.Count > query.Limit;
        var resultItems = items.Take(query.Limit).ToList();

        return new MediaPageResult
        {
            Items = resultItems,
            NextCursor = hasMore && resultItems.Count > 0 ? resultItems[^1].Id : null
        };
    }

    public async Task<List<MediaDto>> GetExpiredPendingAsync(
        string tenantId,
        DateTimeOffset expiredBefore,
        int limit = 100,
        CancellationToken ct = default)
    {
        return await _context.Media
            .Where(m => m.TenantId == tenantId)
            .Where(m => m.Status == MediaStatus.Pending)
            .Where(m => m.CreatedAt < expiredBefore)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<List<MediaDto>> GetDeletedForCleanupAsync(
        string tenantId,
        DateTimeOffset deletedBefore,
        int limit = 100,
        CancellationToken ct = default)
    {
        return await _context.Media
            .Where(m => m.TenantId == tenantId)
            .Where(m => m.Status == MediaStatus.Deleted)
            .Where(m => m.DeletedAt.HasValue && m.DeletedAt.Value < deletedBefore)
            .Take(limit)
            .ToListAsync(ct);
    }
}
