using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Media.Abstractions;

namespace Media.Core;

/// <summary>
/// Azure Blob Storage implementation of IMediaStorageProvider.
/// Works with both Azure Storage and Azurite emulator.
/// </summary>
public sealed class AzureBlobStorageProvider : IMediaStorageProvider
{
    private readonly BlobContainerClient _container;

    /// <summary>
    /// Creates a new AzureBlobStorageProvider.
    /// </summary>
    public AzureBlobStorageProvider(AzureBlobStorageOptions options)
    {
        var serviceClient = new BlobServiceClient(options.ConnectionString);
        _container = serviceClient.GetBlobContainerClient(options.ContainerName);

        if (options.CreateContainerIfNotExists)
        {
            _container.CreateIfNotExists();
        }
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string blobPath, CancellationToken ct = default)
    {
        var blob = _container.GetBlobClient(blobPath);
        var response = await blob.ExistsAsync(ct);
        return response.Value;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task DeleteAsync(string blobPath, CancellationToken ct = default)
    {
        var blob = _container.GetBlobClient(blobPath);
        await blob.DeleteIfExistsAsync(cancellationToken: ct);
    }

    /// <inheritdoc />
    public async Task CopyAsync(string sourcePath, string destPath, CancellationToken ct = default)
    {
        var sourceBlob = _container.GetBlobClient(sourcePath);
        var destBlob = _container.GetBlobClient(destPath);

        await destBlob.StartCopyFromUriAsync(sourceBlob.Uri, cancellationToken: ct);
    }
}
