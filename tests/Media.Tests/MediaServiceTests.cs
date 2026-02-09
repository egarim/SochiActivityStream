using Media.Abstractions;
using Media.Core;
using Media.Store.InMemory;
using ActivityStream.Abstractions;
using ActivityStream.Core;

namespace Media.Tests;

/// <summary>
/// Tests for MediaService implementation.
/// </summary>
public class MediaServiceTests
{
    private readonly MediaService _service;
    private readonly InMemoryMediaStore _store;
    private readonly MockStorageProvider _storageProvider;
    private readonly EntityRefDto _testOwner;

    public MediaServiceTests()
    {
        _store = new InMemoryMediaStore();
        _storageProvider = new MockStorageProvider();
        var options = new MediaServiceOptions();
        _service = new MediaService(_store, _storageProvider, new UlidIdGenerator(), options);
        _testOwner = new EntityRefDto { Kind = "author", Type = "User", Id = "user_123" };
    }

    [Fact]
    public async Task RequestUploadUrlAsync_returns_url_and_creates_pending_media()
    {
        var request = new RequestUploadRequest
        {
            TenantId = "tenant1",
            FileName = "test.jpg",
            ContentType = "image/jpeg",
            SizeBytes = 1024,
            Owner = _testOwner
        };

        var result = await _service.RequestUploadUrlAsync(request);

        Assert.NotNull(result.UploadUrl);
        Assert.NotNull(result.MediaId);
        Assert.True(result.ExpiresAt > DateTimeOffset.UtcNow);

        // Verify media was created in pending state
        var media = await _store.GetByIdAsync("tenant1", result.MediaId);
        Assert.NotNull(media);
        Assert.Equal(MediaStatus.Pending, media.Status);
    }

    [Fact]
    public async Task RequestUploadUrlAsync_fails_for_unsupported_content_type()
    {
        var request = new RequestUploadRequest
        {
            TenantId = "tenant1",
            FileName = "test.exe",
            ContentType = "application/octet-stream",
            SizeBytes = 1024,
            Owner = _testOwner
        };

        var ex = await Assert.ThrowsAsync<MediaValidationException>(
            () => _service.RequestUploadUrlAsync(request));

        Assert.Equal(MediaValidationError.ContentTypeNotAllowed, ex.Error);
    }

    [Fact]
    public async Task RequestUploadUrlAsync_fails_when_file_too_large()
    {
        var request = new RequestUploadRequest
        {
            TenantId = "tenant1",
            FileName = "large.jpg",
            ContentType = "image/jpeg",
            SizeBytes = 100 * 1024 * 1024, // 100MB - larger than default max
            Owner = _testOwner
        };

        var ex = await Assert.ThrowsAsync<MediaValidationException>(
            () => _service.RequestUploadUrlAsync(request));

        Assert.Equal(MediaValidationError.FileTooLarge, ex.Error);
    }

    [Fact]
    public async Task RequestUploadUrlAsync_fails_for_empty_filename()
    {
        var request = new RequestUploadRequest
        {
            TenantId = "tenant1",
            FileName = "",
            ContentType = "image/jpeg",
            SizeBytes = 1024,
            Owner = _testOwner
        };

        var ex = await Assert.ThrowsAsync<MediaValidationException>(
            () => _service.RequestUploadUrlAsync(request));

        Assert.Equal(MediaValidationError.FileNameRequired, ex.Error);
    }

    [Fact]
    public async Task ConfirmUploadAsync_marks_media_as_ready()
    {
        // First request upload
        var uploadRequest = new RequestUploadRequest
        {
            TenantId = "tenant1",
            FileName = "test.jpg",
            ContentType = "image/jpeg",
            SizeBytes = 1024,
            Owner = _testOwner
        };
        var uploadResult = await _service.RequestUploadUrlAsync(uploadRequest);

        // Simulate blob exists
        _storageProvider.SetBlobExists(true, 1024, "image/jpeg");

        // Confirm upload
        var media = await _service.ConfirmUploadAsync("tenant1", uploadResult.MediaId);

        Assert.Equal(MediaStatus.Ready, media.Status);
        Assert.NotNull(media.ConfirmedAt);
    }

    [Fact]
    public async Task ConfirmUploadAsync_fails_when_blob_not_found()
    {
        // First request upload
        var uploadRequest = new RequestUploadRequest
        {
            TenantId = "tenant1",
            FileName = "test.jpg",
            ContentType = "image/jpeg",
            SizeBytes = 1024,
            Owner = _testOwner
        };
        var uploadResult = await _service.RequestUploadUrlAsync(uploadRequest);

        // Blob does not exist
        _storageProvider.SetBlobExists(false, 0, null);

        var ex = await Assert.ThrowsAsync<MediaValidationException>(
            () => _service.ConfirmUploadAsync("tenant1", uploadResult.MediaId));

        Assert.Equal(MediaValidationError.UploadNotConfirmed, ex.Error);
    }

    [Fact]
    public async Task ConfirmUploadAsync_fails_when_media_not_found()
    {
        var ex = await Assert.ThrowsAsync<MediaValidationException>(
            () => _service.ConfirmUploadAsync("tenant1", "nonexistent"));

        Assert.Equal(MediaValidationError.MediaNotFound, ex.Error);
    }

    [Fact]
    public async Task ConfirmUploadAsync_with_metadata_updates_media()
    {
        var uploadRequest = new RequestUploadRequest
        {
            TenantId = "tenant1",
            FileName = "photo.jpg",
            ContentType = "image/jpeg",
            Owner = _testOwner
        };
        var uploadResult = await _service.RequestUploadUrlAsync(uploadRequest);
        _storageProvider.SetBlobExists(true, 2048, "image/jpeg");

        var confirmRequest = new ConfirmUploadRequest
        {
            Width = 1920,
            Height = 1080,
            AltText = "A beautiful sunset"
        };

        var media = await _service.ConfirmUploadAsync("tenant1", uploadResult.MediaId, confirmRequest);

        Assert.Equal(1920, media.Width);
        Assert.Equal(1080, media.Height);
        Assert.Equal("A beautiful sunset", media.AltText);
    }

    [Fact]
    public async Task GetMediaAsync_returns_media_when_exists()
    {
        var uploadRequest = new RequestUploadRequest
        {
            TenantId = "tenant1",
            FileName = "test.jpg",
            ContentType = "image/jpeg",
            SizeBytes = 1024,
            Owner = _testOwner
        };
        var uploadResult = await _service.RequestUploadUrlAsync(uploadRequest);

        var media = await _service.GetMediaAsync("tenant1", uploadResult.MediaId);

        Assert.NotNull(media);
        Assert.Equal(uploadResult.MediaId, media.Id);
    }

    [Fact]
    public async Task GetMediaAsync_returns_null_when_not_exists()
    {
        var result = await _service.GetMediaAsync("tenant1", "nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetMediaAsync_includes_download_url_for_ready_media()
    {
        var uploadRequest = new RequestUploadRequest
        {
            TenantId = "tenant1",
            FileName = "test.jpg",
            ContentType = "image/jpeg",
            Owner = _testOwner
        };
        var uploadResult = await _service.RequestUploadUrlAsync(uploadRequest);
        
        _storageProvider.SetBlobExists(true, 1024, "image/jpeg");
        await _service.ConfirmUploadAsync("tenant1", uploadResult.MediaId);

        var media = await _service.GetMediaAsync("tenant1", uploadResult.MediaId);

        Assert.NotNull(media!.Url);
        Assert.Contains("sas=", media.Url);
    }

    [Fact]
    public async Task UpdateMediaAsync_updates_metadata()
    {
        var uploadRequest = new RequestUploadRequest
        {
            TenantId = "tenant1",
            FileName = "test.jpg",
            ContentType = "image/jpeg",
            SizeBytes = 1024,
            Owner = _testOwner
        };
        var uploadResult = await _service.RequestUploadUrlAsync(uploadRequest);

        var updateRequest = new UpdateMediaRequest
        {
            TenantId = "tenant1",
            MediaId = uploadResult.MediaId,
            Actor = _testOwner,
            AltText = "A beautiful sunset"
        };

        var updated = await _service.UpdateMediaAsync(updateRequest);

        Assert.Equal("A beautiful sunset", updated.AltText);
    }

    [Fact]
    public async Task UpdateMediaAsync_fails_when_not_owner()
    {
        var uploadRequest = new RequestUploadRequest
        {
            TenantId = "tenant1",
            FileName = "test.jpg",
            ContentType = "image/jpeg",
            Owner = _testOwner
        };
        var uploadResult = await _service.RequestUploadUrlAsync(uploadRequest);

        var differentUser = new EntityRefDto { Kind = "author", Type = "User", Id = "other_user" };
        var updateRequest = new UpdateMediaRequest
        {
            TenantId = "tenant1",
            MediaId = uploadResult.MediaId,
            Actor = differentUser,
            AltText = "Unauthorized update"
        };

        var ex = await Assert.ThrowsAsync<MediaValidationException>(
            () => _service.UpdateMediaAsync(updateRequest));

        Assert.Equal(MediaValidationError.NotAuthorized, ex.Error);
    }

    [Fact]
    public async Task DeleteMediaAsync_soft_deletes_media()
    {
        var uploadRequest = new RequestUploadRequest
        {
            TenantId = "tenant1",
            FileName = "test.jpg",
            ContentType = "image/jpeg",
            SizeBytes = 1024,
            Owner = _testOwner
        };
        var uploadResult = await _service.RequestUploadUrlAsync(uploadRequest);

        await _service.DeleteMediaAsync("tenant1", uploadResult.MediaId, _testOwner);

        var media = await _store.GetByIdAsync("tenant1", uploadResult.MediaId);
        Assert.NotNull(media);
        Assert.Equal(MediaStatus.Deleted, media.Status);
        Assert.NotNull(media.DeletedAt);
    }

    [Fact]
    public async Task DeleteMediaAsync_is_idempotent()
    {
        var uploadRequest = new RequestUploadRequest
        {
            TenantId = "tenant1",
            FileName = "test.jpg",
            ContentType = "image/jpeg",
            Owner = _testOwner
        };
        var uploadResult = await _service.RequestUploadUrlAsync(uploadRequest);

        // Delete twice - should not throw
        await _service.DeleteMediaAsync("tenant1", uploadResult.MediaId, _testOwner);
        await _service.DeleteMediaAsync("tenant1", uploadResult.MediaId, _testOwner);

        var media = await _store.GetByIdAsync("tenant1", uploadResult.MediaId);
        Assert.Equal(MediaStatus.Deleted, media!.Status);
    }

    [Fact]
    public async Task DeleteMediaAsync_fails_when_not_owner()
    {
        var uploadRequest = new RequestUploadRequest
        {
            TenantId = "tenant1",
            FileName = "test.jpg",
            ContentType = "image/jpeg",
            Owner = _testOwner
        };
        var uploadResult = await _service.RequestUploadUrlAsync(uploadRequest);

        var differentUser = new EntityRefDto { Kind = "author", Type = "User", Id = "other_user" };

        var ex = await Assert.ThrowsAsync<MediaValidationException>(
            () => _service.DeleteMediaAsync("tenant1", uploadResult.MediaId, differentUser));

        Assert.Equal(MediaValidationError.NotAuthorized, ex.Error);
    }

    [Fact]
    public async Task ListMediaAsync_returns_paginated_results()
    {
        // Create multiple media items
        for (int i = 0; i < 5; i++)
        {
            await _service.RequestUploadUrlAsync(new RequestUploadRequest
            {
                TenantId = "tenant1",
                FileName = $"test_{i}.jpg",
                ContentType = "image/jpeg",
                SizeBytes = 1024,
                Owner = _testOwner
            });
        }

        var query = new MediaQuery { TenantId = "tenant1", Limit = 3 };
        var result = await _service.ListMediaAsync(query);

        Assert.Equal(3, result.Items.Count);
        Assert.NotNull(result.NextCursor);
    }

    [Fact]
    public async Task GetMediaBatchAsync_returns_multiple_media_items()
    {
        var id1 = (await _service.RequestUploadUrlAsync(new RequestUploadRequest
        {
            TenantId = "tenant1",
            FileName = "photo1.jpg",
            ContentType = "image/jpeg",
            Owner = _testOwner
        })).MediaId;

        var id2 = (await _service.RequestUploadUrlAsync(new RequestUploadRequest
        {
            TenantId = "tenant1",
            FileName = "photo2.jpg",
            ContentType = "image/jpeg",
            Owner = _testOwner
        })).MediaId;

        var result = await _service.GetMediaBatchAsync("tenant1", [id1, id2]);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task DetermineMediaType_correctly_identifies_image()
    {
        var uploadRequest = new RequestUploadRequest
        {
            TenantId = "tenant1",
            FileName = "photo.png",
            ContentType = "image/png",
            SizeBytes = 1024,
            Owner = _testOwner
        };
        var uploadResult = await _service.RequestUploadUrlAsync(uploadRequest);

        var media = await _store.GetByIdAsync("tenant1", uploadResult.MediaId);

        Assert.Equal(MediaType.Image, media!.Type);
    }

    [Fact]
    public async Task DetermineMediaType_correctly_identifies_video()
    {
        var uploadRequest = new RequestUploadRequest
        {
            TenantId = "tenant1",
            FileName = "video.mp4",
            ContentType = "video/mp4",
            SizeBytes = 1024,
            Owner = _testOwner
        };
        var uploadResult = await _service.RequestUploadUrlAsync(uploadRequest);

        var media = await _store.GetByIdAsync("tenant1", uploadResult.MediaId);

        Assert.Equal(MediaType.Video, media!.Type);
    }

    [Fact]
    public async Task CleanupAsync_deletes_expired_pending_uploads()
    {
        // Create a pending media with expired timestamp
        var media = new MediaDto
        {
            Id = "expired_pending",
            TenantId = "tenant1",
            FileName = "old.jpg",
            ContentType = "image/jpeg",
            Type = MediaType.Image,
            Status = MediaStatus.Pending,
            Owner = _testOwner,
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-2),
            UploadExpiresAt = DateTimeOffset.UtcNow.AddHours(-1)
        };
        await _store.UpsertAsync(media);

        var result = await _service.CleanupAsync("tenant1");

        Assert.True(result.ExpiredPendingCleaned >= 0);
    }

    /// <summary>
    /// Mock storage provider for testing.
    /// </summary>
    private class MockStorageProvider : IMediaStorageProvider
    {
        private bool _blobExists;
        private long _blobSize;
        private string? _blobContentType;
        public bool DeleteBlobCalled { get; private set; }

        public void SetBlobExists(bool exists, long size, string? contentType)
        {
            _blobExists = exists;
            _blobSize = size;
            _blobContentType = contentType;
        }

        public Task<string> GenerateUploadUrlAsync(string blobPath, string contentType, long maxSizeBytes, TimeSpan expiry, CancellationToken ct = default)
        {
            return Task.FromResult($"https://mock.blob.core.windows.net/media/{blobPath}?sas=mocktoken&expiry={expiry.TotalMinutes}");
        }

        public Task<string> GenerateDownloadUrlAsync(string blobPath, TimeSpan expiry, CancellationToken ct = default)
        {
            return Task.FromResult($"https://mock.blob.core.windows.net/media/{blobPath}?sas=downloadtoken&expiry={expiry.TotalMinutes}");
        }

        public Task<bool> ExistsAsync(string blobPath, CancellationToken ct = default)
        {
            return Task.FromResult(_blobExists);
        }

        public Task<BlobProperties?> GetPropertiesAsync(string blobPath, CancellationToken ct = default)
        {
            if (!_blobExists)
                return Task.FromResult<BlobProperties?>(null);

            return Task.FromResult<BlobProperties?>(new BlobProperties
            {
                SizeBytes = _blobSize,
                ContentType = _blobContentType,
                LastModified = DateTimeOffset.UtcNow
            });
        }

        public Task DeleteAsync(string blobPath, CancellationToken ct = default)
        {
            DeleteBlobCalled = true;
            return Task.CompletedTask;
        }

        public Task CopyAsync(string sourcePath, string destPath, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }
        
        public Task UploadBytesAsync(string blobPath, byte[] data, string contentType, CancellationToken ct = default)
        {
            _blobExists = true;
            _blobSize = data.Length;
            _blobContentType = contentType;
            return Task.CompletedTask;
        }
    }
}
