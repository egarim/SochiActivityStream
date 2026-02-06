# Media.Core

Implementation of the Media Service with Azure Blob Storage support.

## Overview

This library provides:
- `MediaService` - Main service implementation
- `AzureBlobStorageProvider` - Azure Blob Storage integration (works with Azurite)

## Usage

```csharp
// Setup with Azurite (local development)
var storageOptions = new AzureBlobStorageOptions
{
    ConnectionString = "UseDevelopmentStorage=true",
    ContainerName = "media",
    CreateContainerIfNotExists = true
};

var storage = new AzureBlobStorageProvider(storageOptions);
var store = new InMemoryMediaStore(); // From Media.Store.InMemory

var service = new MediaService(
    store,
    storage,
    new UlidIdGenerator(),
    new MediaServiceOptions
    {
        MaxImageSizeBytes = 5 * 1024 * 1024 // 5 MB
    });

// Request upload URL
var uploadResult = await service.RequestUploadUrlAsync(new RequestUploadRequest
{
    TenantId = "my-tenant",
    Owner = new EntityRefDto { Kind = "identity", Type = "Profile", Id = "user123" },
    FileName = "photo.jpg",
    ContentType = "image/jpeg"
});

// Client uploads directly to: uploadResult.UploadUrl

// Confirm upload
var media = await service.ConfirmUploadAsync("my-tenant", uploadResult.MediaId);
Console.WriteLine($"File ready at: {media.Url}");
```

## Configuration

### MediaServiceOptions

| Option | Default | Description |
|--------|---------|-------------|
| ContainerName | "media" | Blob container name |
| UploadUrlExpiry | 15 min | How long upload URLs are valid |
| DownloadUrlExpiry | 1 hour | How long download URLs are valid |
| MaxImageSizeBytes | 10 MB | Maximum image file size |
| MaxVideoSizeBytes | 100 MB | Maximum video file size |
| MaxDocumentSizeBytes | 50 MB | Maximum document file size |

### AzureBlobStorageOptions

| Option | Default | Description |
|--------|---------|-------------|
| ConnectionString | (required) | Azure Storage connection string |
| ContainerName | "media" | Blob container name |
| CreateContainerIfNotExists | true | Auto-create container |

## Azurite Setup

For local development, use Azurite (Azure Storage Emulator):

```bash
# Install globally
npm install -g azurite

# Start Azurite
azurite --silent --location ./azurite-data
```

Connection string for Azurite:
```
UseDevelopmentStorage=true
```

Or explicit:
```
DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;
```
