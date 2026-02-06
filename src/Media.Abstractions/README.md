# Media.Abstractions

DTOs, interfaces, and exception types for the Media Service library.

## Overview

This library provides the foundational types for managing media uploads and downloads via Azure Blob Storage (or any compatible storage provider).

**Dependencies:** `ActivityStream.Abstractions` (for `EntityRefDto`, `IIdGenerator`)

## Key Types

### MediaDto
Represents a media item with metadata and access URLs.

### MediaType
- `Image` - JPEG, PNG, GIF, WebP
- `Video` - MP4, WebM
- `Document` - PDF

### MediaStatus
- `Pending` - Upload URL generated, awaiting upload
- `Ready` - Upload confirmed, file accessible
- `Failed` - Upload timed out or failed
- `Deleted` - Soft deleted, pending cleanup

## Interfaces

### IMediaService
Main service interface for media operations:
- `RequestUploadUrlAsync` - Get signed upload URL
- `ConfirmUploadAsync` - Confirm upload completed
- `GetMediaAsync` - Get media with download URL
- `DeleteMediaAsync` - Soft delete
- `CleanupAsync` - Clean expired/deleted items

### IMediaStore
Persistence layer for media metadata.

### IMediaStorageProvider
Abstraction over blob storage (Azure Blob, Azurite, etc.).

## EntityRef Conventions

```csharp
// Media owner (typically a Profile)
new EntityRefDto { Kind = "identity", Type = "Profile", Id = profileId }
```
