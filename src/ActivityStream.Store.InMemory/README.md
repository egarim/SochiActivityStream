# ActivityStream.Store.InMemory

Reference in-memory storage implementation for the ActivityStream library.

## Overview

This library provides a thread-safe, in-memory implementation of `IActivityStore` for testing, development, and prototyping.

**NuGet:** `ActivityStream.Store.InMemory`  
**Dependencies:** `ActivityStream.Abstractions`

## Installation

```xml
<PackageReference Include="ActivityStream.Store.InMemory" Version="1.0.0" />
```

## Key Classes

### InMemoryActivityStore

Thread-safe in-memory implementation of `IActivityStore`:

```csharp
public class InMemoryActivityStore : IActivityStore
{
    public Task<ActivityDto?> GetByIdAsync(
        string tenantId, string id, CancellationToken ct = default);
    
    public Task<ActivityDto?> FindByIdempotencyAsync(
        string tenantId, string sourceSystem, string idempotencyKey, 
        CancellationToken ct = default);
    
    public Task AppendAsync(ActivityDto activity, CancellationToken ct = default);
    
    public Task<IReadOnlyList<ActivityDto>> QueryAsync(
        ActivityQuery query, CancellationToken ct = default);
}
```

**Features:**
- Thread-safe using `ReaderWriterLockSlim`
- Supports all query operations
- Idempotency enforcement
- Deterministic ordering (OccurredAt DESC, Id DESC)
- Cursor pagination
- Perfect for unit tests and development

**Limitations:**
- Not persistent - data lost on restart
- No data limits - will consume memory
- Linear query performance (not indexed)
- Not suitable for production

## Usage Example

### Basic Setup

```csharp
using ActivityStream.Abstractions;
using ActivityStream.Core;
using ActivityStream.Store.InMemory;

// Create in-memory store
var store = new InMemoryActivityStore();

// Use with service
var service = new ActivityStreamService(
    store: store,
    idGenerator: new UlidIdGenerator(),
    validator: new DefaultActivityValidator()
);
```

### Unit Testing

```csharp
public class ActivityStreamTests
{
    private readonly InMemoryActivityStore _store;
    private readonly ActivityStreamService _service;

    public ActivityStreamTests()
    {
        _store = new InMemoryActivityStore();
        _service = new ActivityStreamService(
            _store,
            new UlidIdGenerator(),
            new DefaultActivityValidator()
        );
    }

    [Fact]
    public async Task PublishAsync_StoresActivity()
    {
        // Arrange
        var activity = new ActivityDto
        {
            TenantId = "test",
            TypeKey = "test.action",
            OccurredAt = DateTimeOffset.UtcNow,
            Actor = new EntityRefDto { Kind = "user", Type = "User", Id = "u_1" },
            Payload = new { message = "test" }
        };

        // Act
        var stored = await _service.PublishAsync(activity);

        // Assert
        Assert.NotNull(stored.Id);
        var retrieved = await _service.GetByIdAsync("test", stored.Id);
        Assert.NotNull(retrieved);
        Assert.Equal(stored.Id, retrieved.Id);
    }

    [Fact]
    public async Task QueryAsync_ReturnsInCorrectOrder()
    {
        // Arrange
        var baseTime = DateTimeOffset.UtcNow;
        await _service.PublishAsync(CreateActivity("test", "a.1", baseTime.AddMinutes(-2)));
        await _service.PublishAsync(CreateActivity("test", "a.2", baseTime.AddMinutes(-1)));
        await _service.PublishAsync(CreateActivity("test", "a.3", baseTime));

        // Act
        var result = await _service.QueryAsync(new ActivityQuery
        {
            TenantId = "test",
            Limit = 10
        });

        // Assert
        Assert.Equal(3, result.Items.Count);
        Assert.Equal("a.3", result.Items[0].TypeKey); // Most recent first
        Assert.Equal("a.2", result.Items[1].TypeKey);
        Assert.Equal("a.1", result.Items[2].TypeKey);
    }

    private ActivityDto CreateActivity(string tenantId, string typeKey, DateTimeOffset occurredAt)
    {
        return new ActivityDto
        {
            TenantId = tenantId,
            TypeKey = typeKey,
            OccurredAt = occurredAt,
            Actor = new EntityRefDto { Kind = "user", Type = "User", Id = "u_1" },
            Payload = new { }
        };
    }
}
```

### Integration Testing

```csharp
public class ActivityStreamIntegrationTests
{
    [Fact]
    public async Task IdempotentPublish_ReturnsSameActivity()
    {
        // Arrange
        var store = new InMemoryActivityStore();
        var service = new ActivityStreamService(
            store,
            new UlidIdGenerator(),
            new DefaultActivityValidator()
        );

        var activity = new ActivityDto
        {
            TenantId = "test",
            TypeKey = "order.created",
            OccurredAt = DateTimeOffset.UtcNow,
            Actor = new EntityRefDto { Kind = "user", Type = "User", Id = "u_1" },
            Payload = new { orderId = 123 },
            Source = new ActivitySourceDto
            {
                System = "shop",
                IdempotencyKey = "order_123_created"
            }
        };

        // Act
        var result1 = await service.PublishAsync(activity);
        var result2 = await service.PublishAsync(activity);

        // Assert
        Assert.Equal(result1.Id, result2.Id);
        Assert.Equal(result1.CreatedAt, result2.CreatedAt);
    }

    [Fact]
    public async Task CursorPagination_NoDuplicates()
    {
        // Arrange
        var store = new InMemoryActivityStore();
        var service = new ActivityStreamService(
            store,
            new UlidIdGenerator(),
            new DefaultActivityValidator()
        );

        var baseTime = DateTimeOffset.UtcNow;
        for (int i = 0; i < 100; i++)
        {
            await service.PublishAsync(new ActivityDto
            {
                TenantId = "test",
                TypeKey = $"item.{i}",
                OccurredAt = baseTime.AddMinutes(-i),
                Actor = new EntityRefDto { Kind = "user", Type = "User", Id = "u_1" },
                Payload = new { index = i }
            });
        }

        // Act
        var allIds = new HashSet<string>();
        string? cursor = null;
        int pageCount = 0;

        do
        {
            var page = await service.QueryAsync(new ActivityQuery
            {
                TenantId = "test",
                Cursor = cursor,
                Limit = 25
            });

            foreach (var item in page.Items)
            {
                Assert.True(allIds.Add(item.Id), $"Duplicate ID found: {item.Id}");
            }

            cursor = page.NextCursor;
            pageCount++;
        } while (cursor != null);

        // Assert
        Assert.Equal(100, allIds.Count);
        Assert.Equal(4, pageCount);
    }
}
```

### Development Usage

```csharp
// Quick setup for development
var store = new InMemoryActivityStore();
var service = new ActivityStreamService(
    store,
    new UlidIdGenerator(),
    new DefaultActivityValidator()
);

// Populate with test data
await PopulateTestData(service);

// Use in your application
app.MapGet("/activities", async (string tenantId) =>
{
    var result = await service.QueryAsync(new ActivityQuery
    {
        TenantId = tenantId,
        Limit = 50
    });
    return Results.Ok(result);
});

async Task PopulateTestData(IActivityStreamService service)
{
    var tenants = new[] { "acme", "contoso", "fabrikam" };
    var typeKeys = new[] { "invoice.paid", "order.shipped", "user.registered" };

    foreach (var tenant in tenants)
    {
        for (int i = 0; i < 20; i++)
        {
            await service.PublishAsync(new ActivityDto
            {
                TenantId = tenant,
                TypeKey = typeKeys[i % typeKeys.Length],
                OccurredAt = DateTimeOffset.UtcNow.AddMinutes(-i),
                Actor = new EntityRefDto 
                { 
                    Kind = "user", 
                    Type = "User", 
                    Id = $"u_{i % 5}", 
                    Display = $"User {i % 5}" 
                },
                Payload = new { index = i },
                Summary = $"Test activity {i}"
            });
        }
    }
}
```

## Thread Safety

The store is thread-safe and can be used concurrently:

```csharp
var store = new InMemoryActivityStore();
var service = new ActivityStreamService(store, new UlidIdGenerator(), new DefaultActivityValidator());

// Safe to call from multiple threads
var tasks = Enumerable.Range(0, 100).Select(async i =>
{
    await service.PublishAsync(new ActivityDto
    {
        TenantId = "test",
        TypeKey = $"concurrent.{i}",
        OccurredAt = DateTimeOffset.UtcNow,
        Actor = new EntityRefDto { Kind = "user", Type = "User", Id = $"u_{i}" },
        Payload = new { index = i }
    });
});

await Task.WhenAll(tasks);
```

## Query Performance

The in-memory store uses linear scans for queries:
- **GetByIdAsync**: O(n) - scans all activities
- **FindByIdempotencyAsync**: O(n) - scans all activities
- **QueryAsync**: O(n) - scans all activities, then sorts

For production, implement `IActivityStore` with a real database that supports indexing.

## Migration to Production Store

When moving to production, replace the in-memory store with a database implementation:

```csharp
// Development
var store = new InMemoryActivityStore();

// Production - implement for your database
var store = new PostgresActivityStore(connectionString);
// or
var store = new SqlServerActivityStore(connectionString);
// or
var store = new MongoActivityStore(connectionString);

// Service usage remains the same
var service = new ActivityStreamService(store, idGenerator, validator);
```

## When to Use

**✅ Good For:**
- Unit tests
- Integration tests
- Development
- Prototyping
- Demos
- Small datasets (<10,000 activities)

**❌ Not For:**
- Production environments
- Large datasets
- Persistent storage requirements
- Performance-critical scenarios
- Multi-process scenarios

## See Also

- [ActivityStream.Abstractions](../ActivityStream.Abstractions/README.md) - Core DTOs and interfaces
- [ActivityStream.Core](../ActivityStream.Core/README.md) - Service implementation
- [Main README](../../README.md) - All libraries overview
