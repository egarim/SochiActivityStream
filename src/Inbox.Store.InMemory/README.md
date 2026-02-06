# Inbox.Store.InMemory

In-memory implementations of inbox stores for development and testing.

## Overview

This library provides thread-safe in-memory implementations of:
- `IInboxStore` - Storage for inbox notification items
- `IFollowRequestStore` - Storage for follow/subscribe requests

**NuGet:** `Inbox.Store.InMemory`  
**Dependencies:** `Inbox.Abstractions`

## Installation

```xml
<PackageReference Include="Inbox.Store.InMemory" Version="1.0.0" />
```

## Key Components

### InMemoryInboxStore

Thread-safe in-memory implementation of `IInboxStore`:

```csharp
var store = new InMemoryInboxStore();
```

**Features:**
- Primary storage by `{tenantId}|{id}`
- Secondary index for DedupKey lookups
- Secondary index for ThreadKey lookups
- Uses `ConcurrentDictionary` + lock for thread safety

### InMemoryFollowRequestStore

Thread-safe in-memory implementation of `IFollowRequestStore`:

```csharp
var store = new InMemoryFollowRequestStore();
```

**Features:**
- Primary storage by `{tenantId}|{id}`
- Secondary index for IdempotencyKey lookups
- Uses `ConcurrentDictionary` + lock for thread safety

## Usage

### Development/Testing

```csharp
var inboxStore = new InMemoryInboxStore();
var requestStore = new InMemoryFollowRequestStore();

var service = new InboxNotificationService(
    inboxStore,
    requestStore,
    relationshipService,
    governancePolicy,
    idGenerator);
```

### Unit Tests

```csharp
public class MyTests
{
    private readonly InMemoryInboxStore _store;
    
    public MyTests()
    {
        _store = new InMemoryInboxStore();
    }
    
    [Fact]
    public async Task Upsert_and_retrieve()
    {
        var item = new InboxItemDto
        {
            Id = "item_1",
            TenantId = "acme",
            Recipient = new EntityRefDto { Kind = "identity", Type = "Profile", Id = "p_1" },
            Event = new InboxEventRefDto { Kind = "activity", Id = "act_1" }
        };
        
        await _store.UpsertAsync(item);
        
        var retrieved = await _store.GetByIdAsync("acme", "item_1");
        Assert.NotNull(retrieved);
    }
}
```

## Thread Safety

Both stores use:
- `ConcurrentDictionary<string, T>` for primary storage
- `lock` for compound read-modify-write operations
- Separate secondary indexes for efficient lookups

This ensures safe concurrent access without external synchronization.

## Limitations

- Data is not persisted across restarts
- Not suitable for production workloads with large data volumes
- No horizontal scaling (single instance only)

For production use, implement `IInboxStore` and `IFollowRequestStore` with a real database backend (SQL, Cosmos DB, etc.).
