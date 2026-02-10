using Media.Abstractions;
using Media.Store.InMemory;

namespace ActivityStream.Tests.Media;

/// <summary>
/// Tests for InMemoryMediaStore implementation.
/// </summary>
public class InMemoryMediaStoreTests
{
    private readonly InMemoryMediaStore _store;

    public InMemoryMediaStoreTests()
    {
        _store = new InMemoryMediaStore();
    }

    private static MediaDto CreateTestMedia(string? tenantId = null, string? id = null) => new()
    {
        Id = id ?? "media_001",
        TenantId = tenantId ?? "tenant1",
        FileName = "test.jpg",
        ContentType = "image/jpeg",
        Type = MediaType.Image,
        Status = MediaStatus.Pending,
        SizeBytes = 1024,
        BlobPath = "tenant1/2024/01/media_001/test.jpg",
        CreatedAt = DateTimeOffset.UtcNow,
        Owner = new ActivityStream.Abstractions.EntityRefDto { Kind = "author", Type = "User", Id = "user_123" }
    };

    [Fact]
    public async Task UpsertAsync_stores_media_and_returns_it()
    {
        var media = CreateTestMedia();

        var result = await _store.UpsertAsync(media);

        Assert.NotNull(result);
        Assert.Equal(media.Id, result.Id);
        Assert.Equal(media.TenantId, result.TenantId);
        Assert.Equal(media.FileName, result.FileName);
    }

    [Fact]
    public async Task GetByIdAsync_returns_media_when_exists()
    {
        var media = CreateTestMedia();
        await _store.UpsertAsync(media);

        var result = await _store.GetByIdAsync(media.TenantId, media.Id!);

        Assert.NotNull(result);
        Assert.Equal(media.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_when_not_exists()
    {
        var result = await _store.GetByIdAsync("tenant1", "nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_for_wrong_tenant()
    {
        var media = CreateTestMedia(tenantId: "tenant1");
        await _store.UpsertAsync(media);

        var result = await _store.GetByIdAsync("tenant2", media.Id!);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpsertAsync_updates_existing_media()
    {
        var media = CreateTestMedia();
        await _store.UpsertAsync(media);
        
        media.Status = MediaStatus.Ready;
        media.AltText = "Updated alt text";
        
        var updated = await _store.UpsertAsync(media);

        Assert.Equal(MediaStatus.Ready, updated.Status);
        Assert.Equal("Updated alt text", updated.AltText);
    }

    [Fact]
    public async Task DeleteAsync_removes_media()
    {
        var media = CreateTestMedia();
        await _store.UpsertAsync(media);

        await _store.DeleteAsync(media.TenantId, media.Id!);
        var retrieved = await _store.GetByIdAsync(media.TenantId, media.Id!);

        Assert.Null(retrieved);
    }

    [Fact]
    public async Task QueryAsync_returns_all_media_for_tenant()
    {
        await _store.UpsertAsync(CreateTestMedia(tenantId: "tenant1", id: "media_001"));
        await _store.UpsertAsync(CreateTestMedia(tenantId: "tenant1", id: "media_002"));
        await _store.UpsertAsync(CreateTestMedia(tenantId: "tenant2", id: "media_003"));

        var query = new MediaQuery { TenantId = "tenant1" };
        var result = await _store.QueryAsync(query);

        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, m => Assert.Equal("tenant1", m.TenantId));
    }

    [Fact]
    public async Task QueryAsync_filters_by_media_type()
    {
        var imageMedia = CreateTestMedia(id: "img_001");
        imageMedia.Type = MediaType.Image;
        await _store.UpsertAsync(imageMedia);

        var videoMedia = CreateTestMedia(id: "vid_001");
        videoMedia.Type = MediaType.Video;
        await _store.UpsertAsync(videoMedia);

        var query = new MediaQuery { TenantId = "tenant1", Type = MediaType.Image };
        var result = await _store.QueryAsync(query);

        Assert.Single(result.Items);
        Assert.Equal(MediaType.Image, result.Items[0].Type);
    }

    [Fact]
    public async Task QueryAsync_filters_by_status()
    {
        var pendingMedia = CreateTestMedia(id: "pending_001");
        pendingMedia.Status = MediaStatus.Pending;
        await _store.UpsertAsync(pendingMedia);

        var readyMedia = CreateTestMedia(id: "ready_001");
        readyMedia.Status = MediaStatus.Ready;
        await _store.UpsertAsync(readyMedia);

        var query = new MediaQuery { TenantId = "tenant1", Status = MediaStatus.Ready };
        var result = await _store.QueryAsync(query);

        Assert.Single(result.Items);
        Assert.Equal(MediaStatus.Ready, result.Items[0].Status);
    }

    [Fact]
    public async Task QueryAsync_respects_limit()
    {
        for (int i = 0; i < 10; i++)
        {
            await _store.UpsertAsync(CreateTestMedia(id: $"media_{i:D3}"));
        }

        var query = new MediaQuery { TenantId = "tenant1", Limit = 5 };
        var result = await _store.QueryAsync(query);

        Assert.Equal(5, result.Items.Count);
        Assert.NotNull(result.NextCursor);
    }

    [Fact]
    public async Task QueryAsync_supports_cursor_pagination()
    {
        for (int i = 0; i < 10; i++)
        {
            await _store.UpsertAsync(CreateTestMedia(id: $"media_{i:D3}"));
        }

        var query1 = new MediaQuery { TenantId = "tenant1", Limit = 5 };
        var result1 = await _store.QueryAsync(query1);

        var query2 = new MediaQuery { TenantId = "tenant1", Limit = 5, Cursor = result1.NextCursor };
        var result2 = await _store.QueryAsync(query2);

        Assert.Equal(5, result1.Items.Count);
        Assert.Equal(5, result2.Items.Count);
        
        // No overlap between pages
        var allIds = result1.Items.Select(m => m.Id).Concat(result2.Items.Select(m => m.Id)).ToList();
        Assert.Equal(10, allIds.Distinct().Count());
    }

    [Fact]
    public async Task GetExpiredPendingAsync_returns_only_expired_pending_media()
    {
        var recentPending = CreateTestMedia(id: "recent_001");
        recentPending.Status = MediaStatus.Pending;
        recentPending.UploadExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10); // Not expired yet
        await _store.UpsertAsync(recentPending);

        var oldPending = CreateTestMedia(id: "old_001");
        oldPending.Status = MediaStatus.Pending;
        oldPending.UploadExpiresAt = DateTimeOffset.UtcNow.AddHours(-2); // Expired
        await _store.UpsertAsync(oldPending);

        var oldReady = CreateTestMedia(id: "ready_001");
        oldReady.Status = MediaStatus.Ready;
        oldReady.UploadExpiresAt = DateTimeOffset.UtcNow.AddHours(-2);
        await _store.UpsertAsync(oldReady);

        var cutoff = DateTimeOffset.UtcNow;
        var expired = await _store.GetExpiredPendingAsync("tenant1", cutoff, 100);

        Assert.Single(expired);
        Assert.Equal("old_001", expired[0].Id);
    }

    [Fact]
    public async Task GetDeletedForCleanupAsync_returns_soft_deleted_media()
    {
        var active = CreateTestMedia(id: "active_001");
        active.Status = MediaStatus.Ready;
        await _store.UpsertAsync(active);

        var deleted = CreateTestMedia(id: "deleted_001");
        deleted.Status = MediaStatus.Deleted;
        deleted.DeletedAt = DateTimeOffset.UtcNow.AddDays(-10);
        await _store.UpsertAsync(deleted);

        var recentlyDeleted = CreateTestMedia(id: "deleted_002");
        recentlyDeleted.Status = MediaStatus.Deleted;
        recentlyDeleted.DeletedAt = DateTimeOffset.UtcNow.AddDays(-1);
        await _store.UpsertAsync(recentlyDeleted);

        var olderThan = DateTimeOffset.UtcNow.AddDays(-7);
        var result = await _store.GetDeletedForCleanupAsync("tenant1", olderThan, 100);

        Assert.Single(result);
        Assert.Equal("deleted_001", result[0].Id);
    }

    [Fact]
    public async Task UpsertAsync_creates_independent_copy()
    {
        var media = CreateTestMedia();
        await _store.UpsertAsync(media);
        
        // Modify original
        media.AltText = "Modified";

        var retrieved = await _store.GetByIdAsync(media.TenantId, media.Id!);

        Assert.Null(retrieved!.AltText); // Original modification should not affect stored copy
    }

    [Fact]
    public async Task GetByIdAsync_returns_independent_copy()
    {
        var media = CreateTestMedia();
        await _store.UpsertAsync(media);
        
        var retrieved1 = await _store.GetByIdAsync(media.TenantId, media.Id!);
        retrieved1!.AltText = "Modified";

        var retrieved2 = await _store.GetByIdAsync(media.TenantId, media.Id!);

        Assert.Null(retrieved2!.AltText); // Modification should not affect stored or subsequent reads
    }

    [Fact]
    public async Task GetByIdsAsync_returns_multiple_media()
    {
        await _store.UpsertAsync(CreateTestMedia(id: "media_001"));
        await _store.UpsertAsync(CreateTestMedia(id: "media_002"));
        await _store.UpsertAsync(CreateTestMedia(id: "media_003"));

        var result = await _store.GetByIdsAsync("tenant1", ["media_001", "media_003"]);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, m => m.Id == "media_001");
        Assert.Contains(result, m => m.Id == "media_003");
    }

    [Fact]
    public async Task GetByIdsAsync_ignores_nonexistent_ids()
    {
        await _store.UpsertAsync(CreateTestMedia(id: "media_001"));

        var result = await _store.GetByIdsAsync("tenant1", ["media_001", "nonexistent"]);

        Assert.Single(result);
        Assert.Equal("media_001", result[0].Id);
    }
}
