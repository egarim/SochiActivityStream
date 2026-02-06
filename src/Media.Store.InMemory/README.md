# Media.Store.InMemory

In-memory reference implementation of IMediaStore.

## Usage

```csharp
var store = new InMemoryMediaStore();

// Use with MediaService
var service = new MediaService(
    store,
    new AzureBlobStorageProvider(storageOptions),
    new UlidIdGenerator());
```

## Features

- Primary index: `{tenantId}|{mediaId} → MediaDto`
- Query by owner, type, status
- Cursor-based pagination
- Cleanup queries for expired pending and deleted items

## Thread Safety

Uses `ConcurrentDictionary` with `lock` for multi-step operations.

## Notes

This is a **reference implementation** for development and testing.
For production, implement `IMediaStore` with a proper database.
