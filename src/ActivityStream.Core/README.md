# ActivityStream.Core

Service implementation with validation, idempotency, and cursor pagination for the ActivityStream library.

## Overview

This library provides the core service implementation that orchestrates validation, storage, and query operations for activity streams.

**NuGet:** `ActivityStream.Core`  
**Dependencies:** `ActivityStream.Abstractions`, NUlid

## Installation

```xml
<PackageReference Include="ActivityStream.Core" Version="1.0.0" />
<PackageReference Include="ActivityStream.Abstractions" Version="1.0.0" />
```

## Key Classes

### ActivityStreamService

Main service implementation of `IActivityStreamService`:

```csharp
public class ActivityStreamService : IActivityStreamService
{
    public ActivityStreamService(
        IActivityStore store,
        IIdGenerator idGenerator,
        IActivityValidator validator)
    {
        // ...
    }

    Task<ActivityDto> PublishAsync(ActivityDto activity, CancellationToken ct = default);
    Task<ActivityDto?> GetByIdAsync(string tenantId, string id, CancellationToken ct = default);
    Task<PagedResult<ActivityDto>> QueryAsync(ActivityQuery query, CancellationToken ct = default);
}
```

**Features:**
- Validates activities before storage
- Generates IDs if not provided
- Enforces idempotency based on Source.System + Source.IdempotencyKey
- Returns deterministically ordered results (OccurredAt DESC, Id DESC)
- Cursor-based pagination with no gaps or duplicates

### DefaultActivityValidator

Built-in validator with sensible defaults:

```csharp
public class DefaultActivityValidator : IActivityValidator
{
    public IReadOnlyList<ActivityValidationError> Validate(ActivityDto activity)
    {
        // Validation logic
    }
}
```

**Validation Rules:**
- `TenantId` - Required, non-empty string
- `TypeKey` - Required, max 200 characters
- `OccurredAt` - Must not be default value
- `Actor` - Required with valid Kind, Type, Id
- `Payload` - Required (not null)
- `Targets` - Each must have valid Kind, Type, Id
- `Summary` - Max 500 characters
- `Tags` - Max 50 items, each max 100 characters
- `EntityRefDto` properties trimmed and validated

**Errors Throw:** `ActivityValidationException` with detailed error information.

### UlidIdGenerator

Time-ordered ID generator using ULID:

```csharp
public class UlidIdGenerator : IIdGenerator
{
    public string GenerateId() => Ulid.NewUlid().ToString();
}
```

**Benefits:**
- Lexicographically sortable
- 128-bit uniqueness
- Timestamp-prefixed
- URL-safe Base32 encoding
- Compatible with string columns

### CursorHelper

Internal helper for cursor pagination:

```csharp
public static class CursorHelper
{
    public static string EncodeCursor(DateTimeOffset occurredAt, string id);
    public static (DateTimeOffset OccurredAt, string Id)? DecodeCursor(string cursor);
}
```

**Cursor Format:** Base64-encoded `{OccurredAt:O}|{Id}`

## Usage Example

### Basic Setup

```csharp
using ActivityStream.Abstractions;
using ActivityStream.Core;
using ActivityStream.Store.InMemory;

// Setup dependencies
var store = new InMemoryActivityStore();
var idGenerator = new UlidIdGenerator();
var validator = new DefaultActivityValidator();

// Create service
var service = new ActivityStreamService(store, idGenerator, validator);
```

### Publishing Activities

```csharp
// Publish a new activity
var activity = new ActivityDto
{
    TenantId = "acme-corp",
    TypeKey = "invoice.paid",
    OccurredAt = DateTimeOffset.UtcNow,
    Actor = new EntityRefDto 
    { 
        Kind = "user", 
        Type = "User", 
        Id = "u_123", 
        Display = "John Doe" 
    },
    Targets = new List<EntityRefDto>
    {
        new() 
        { 
            Kind = "object", 
            Type = "Invoice", 
            Id = "inv_332", 
            Display = "Invoice #332" 
        }
    },
    Summary = "Invoice #332 paid by John Doe",
    Payload = new 
    { 
        invoiceNumber = 332, 
        amount = 500m, 
        currency = "USD",
        paymentMethod = "credit-card"
    },
    Source = new ActivitySourceDto 
    { 
        System = "erp", 
        IdempotencyKey = "inv_332_payment_2024" 
    },
    Tags = new List<string> { "payment", "invoice" }
};

try
{
    var stored = await service.PublishAsync(activity);
    Console.WriteLine($"Published activity: {stored.Id}");
    Console.WriteLine($"Created at: {stored.CreatedAt}");
}
catch (ActivityValidationException ex)
{
    foreach (var error in ex.Errors)
    {
        Console.WriteLine($"Validation error: {error.Field} - {error.Message}");
    }
}
```

### Idempotent Publishing

```csharp
// First publish
var result1 = await service.PublishAsync(activity);
Console.WriteLine($"First: {result1.Id}");

// Second publish with same Source.System + Source.IdempotencyKey
var result2 = await service.PublishAsync(activity);
Console.WriteLine($"Second: {result2.Id}");

// Both return the same activity ID
Assert.Equal(result1.Id, result2.Id);
```

### Querying Activities

```csharp
// Query all activities for a tenant
var page = await service.QueryAsync(new ActivityQuery
{
    TenantId = "acme-corp",
    Limit = 50
});

Console.WriteLine($"Found {page.Items.Count} activities");
foreach (var item in page.Items)
{
    Console.WriteLine($"{item.OccurredAt:u} - {item.TypeKey}: {item.Summary}");
}
```

### Filtering Queries

```csharp
// Filter by type prefix
var invoiceActivities = await service.QueryAsync(new ActivityQuery
{
    TenantId = "acme-corp",
    TypeKeyPrefixes = new List<string> { "invoice" },
    Limit = 20
});

// Filter by actor
var userActivities = await service.QueryAsync(new ActivityQuery
{
    TenantId = "acme-corp",
    Actor = new EntityRefDto { Kind = "user", Type = "User", Id = "u_123" },
    Limit = 20
});

// Filter by target
var objectTimeline = await service.QueryAsync(new ActivityQuery
{
    TenantId = "acme-corp",
    Target = new EntityRefDto { Kind = "object", Type = "Invoice", Id = "inv_332" },
    Limit = 20
});

// Filter by date range
var recentActivities = await service.QueryAsync(new ActivityQuery
{
    TenantId = "acme-corp",
    FromDate = DateTimeOffset.UtcNow.AddDays(-7),
    ToDate = DateTimeOffset.UtcNow,
    Limit = 100
});

// Filter by tags
var taggedActivities = await service.QueryAsync(new ActivityQuery
{
    TenantId = "acme-corp",
    Tags = new List<string> { "urgent", "payment" },
    Limit = 20
});
```

### Cursor Pagination

```csharp
var allActivities = new List<ActivityDto>();
string? cursor = null;

do
{
    var page = await service.QueryAsync(new ActivityQuery
    {
        TenantId = "acme-corp",
        Cursor = cursor,
        Limit = 50
    });

    allActivities.AddRange(page.Items);
    cursor = page.NextCursor;
    
    Console.WriteLine($"Fetched {page.Items.Count} activities, total: {allActivities.Count}");
    
} while (cursor != null);

Console.WriteLine($"Total activities: {allActivities.Count}");
```

## Behavioral Guarantees

### Ordering

All queries return activities in deterministic order:
1. **Primary sort:** `OccurredAt` descending (most recent first)
2. **Tie-breaker:** `Id` descending (for activities with same timestamp)

This ensures stable pagination even when many activities share the same timestamp.

### Cursor Pagination

- Cursors are opaque strings encoding the position of the last item
- Using a cursor guarantees no duplicates or gaps between pages
- Works correctly when activities share the same `OccurredAt`
- `NextCursor` is `null` when no more items exist
- Cursors remain valid even if new activities are published

### Idempotency

When `Source.System` and `Source.IdempotencyKey` are both provided:
- Duplicate publishes return the existing activity (same `Id`)
- Idempotency is scoped per `TenantId + System + IdempotencyKey`
- Only the first publish is stored; subsequent publishes are lookups
- `CreatedAt` reflects the first publish time

### Validation

Validation occurs before storage:
- Throws `ActivityValidationException` if validation fails
- Exception contains all validation errors (not just the first one)
- No partial writes - either validates completely or throws
- Custom validators can be provided for domain-specific rules

## Custom Validation

Create a custom validator for domain-specific rules:

```csharp
public class CustomActivityValidator : IActivityValidator
{
    private readonly DefaultActivityValidator _default = new();

    public IReadOnlyList<ActivityValidationError> Validate(ActivityDto activity)
    {
        var errors = new List<ActivityValidationError>();
        
        // Start with default validation
        errors.AddRange(_default.Validate(activity));
        
        // Add custom rules
        if (activity.TypeKey.StartsWith("invoice.") && 
            activity.Payload is not JsonElement payload ||
            !payload.TryGetProperty("invoiceNumber", out _))
        {
            errors.Add(new ActivityValidationError
            {
                Field = "Payload.invoiceNumber",
                Message = "Invoice activities must include invoiceNumber in payload"
            });
        }
        
        return errors;
    }
}

// Use custom validator
var service = new ActivityStreamService(store, idGenerator, new CustomActivityValidator());
```

## Performance Considerations

### Indexing

For optimal query performance, ensure your store implementation indexes:
- `(TenantId, OccurredAt DESC, Id DESC)` - Primary query index
- `(TenantId, SourceSystem, IdempotencyKey)` - Idempotency lookup (unique)
- Consider separate target table for fast object timelines

### Batch Operations

The service doesn't currently support batch operations, but you can implement batching in your store:

```csharp
// In your custom store
public async Task AppendBatchAsync(IEnumerable<ActivityDto> activities, CancellationToken ct = default)
{
    // Batch insert implementation
}
```

### Query Limits

- Default limit: 50
- Maximum recommended: 1000
- Use cursor pagination for large result sets
- Consider caching for frequently accessed pages

## See Also

- [ActivityStream.Abstractions](../ActivityStream.Abstractions/README.md) - Core DTOs and interfaces
- [ActivityStream.Store.InMemory](../ActivityStream.Store.InMemory/README.md) - Reference storage
- [Main README](../../README.md) - All libraries overview
