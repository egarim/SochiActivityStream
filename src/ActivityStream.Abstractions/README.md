# ActivityStream.Abstractions

Core DTOs, interfaces, and error types for the ActivityStream library.

## Overview

This library provides the foundational types for building activity stream systems. It contains no implementation code - only contracts and data structures.

**NuGet:** `ActivityStream.Abstractions`  
**Dependencies:** None (pure .NET 8)

## Installation

```xml
<PackageReference Include="ActivityStream.Abstractions" Version="1.0.0" />
```

## Key Types

### ActivityDto

The canonical activity envelope that all activity types share:

```csharp
public class ActivityDto
{
    public string? Id { get; set; }
    public required string TenantId { get; set; }
    public required string TypeKey { get; set; }
    public required DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public required EntityRefDto Actor { get; set; }
    public EntityRefDto? Owner { get; set; }
    public List<EntityRefDto> Targets { get; set; }
    public ActivityVisibility Visibility { get; set; }
    public string? Summary { get; set; }
    public required object Payload { get; set; }
    public ActivitySourceDto? Source { get; set; }
    public List<string> Tags { get; set; }
}
```

**Key Properties:**
- `TenantId` - Required partition key for multi-tenancy
- `TypeKey` - Activity type (e.g., "invoice.paid", "build.completed")
- `OccurredAt` - When the activity actually happened
- `Actor` - Who/what caused the activity
- `Owner` - Optional timeline owner grouping
- `Targets` - Related entities for indexing/timelines
- `Visibility` - Public, Internal, or Private
- `Payload` - Type-specific data (schema-free)
- `Source` - Source system and idempotency key

### EntityRefDto

Universal entity reference used throughout the system:

```csharp
public class EntityRefDto
{
    public required string Kind { get; set; }    // "user", "object", "service"
    public required string Type { get; set; }    // "User", "Invoice", "CIServer"
    public required string Id { get; set; }      // "u_123", "inv_332"
    public string? Display { get; set; }         // "John Doe", "Invoice #332"
}
```

**Equality:** Two EntityRefDto instances are equal when Kind, Type, and Id match (case-insensitive, trimmed).

### ActivityVisibility

Controls who can see an activity:

```csharp
public enum ActivityVisibility
{
    Internal = 0,  // Default: visible within tenant
    Public = 1,    // Visible to everyone
    Private = 2    // Only visible to Actor, Owner, and Targets
}
```

### ActivitySourceDto

Tracks source system and enables idempotency:

```csharp
public class ActivitySourceDto
{
    public required string System { get; set; }           // "erp", "crm", "ci"
    public string? CorrelationId { get; set; }           // Optional trace ID
    public required string IdempotencyKey { get; set; }  // Unique within system
}
```

**Idempotency:** When both System and IdempotencyKey are provided, duplicate publishes return the existing activity.

### ActivityQuery

Query parameters for fetching activities:

```csharp
public class ActivityQuery
{
    public required string TenantId { get; set; }
    public string? TypeKey { get; set; }
    public List<string>? TypeKeyPrefixes { get; set; }
    public List<string>? Tags { get; set; }
    public EntityRefDto? Actor { get; set; }
    public EntityRefDto? Owner { get; set; }
    public EntityRefDto? Target { get; set; }
    public DateTimeOffset? FromDate { get; set; }
    public DateTimeOffset? ToDate { get; set; }
    public int Limit { get; set; } = 50;
    public string? Cursor { get; set; }
}
```

### PagedResult<T>

Standard paging response:

```csharp
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; }
    public string? NextCursor { get; init; }
    public int TotalCount { get; init; }
}
```

## Interfaces

### IActivityStore

Storage abstraction that you implement for your database:

```csharp
public interface IActivityStore
{
    Task<ActivityDto?> GetByIdAsync(
        string tenantId, string id, CancellationToken ct = default);
    
    Task<ActivityDto?> FindByIdempotencyAsync(
        string tenantId, string sourceSystem, string idempotencyKey, 
        CancellationToken ct = default);
    
    Task AppendAsync(ActivityDto activity, CancellationToken ct = default);
    
    Task<IReadOnlyList<ActivityDto>> QueryAsync(
        ActivityQuery query, CancellationToken ct = default);
}
```

**Implementation Notes:**
- `GetByIdAsync` - Direct lookup by activity ID
- `FindByIdempotencyAsync` - Lookup for idempotency check
- `AppendAsync` - Store new activity (should be append-only)
- `QueryAsync` - Query with filtering and pagination

### IActivityValidator

Validation abstraction:

```csharp
public interface IActivityValidator
{
    IReadOnlyList<ActivityValidationError> Validate(ActivityDto activity);
}
```

### IIdGenerator

ID generation abstraction:

```csharp
public interface IIdGenerator
{
    string GenerateId();
}
```

**Recommended:** ULID for time-ordered IDs.

## Usage Example

```csharp
using ActivityStream.Abstractions;

// Create an activity
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
        new() { Kind = "object", Type = "Invoice", Id = "inv_332" }
    },
    Summary = "John Doe paid Invoice #332",
    Payload = new 
    { 
        invoiceNumber = 332, 
        amount = 500m, 
        currency = "USD"
    },
    Source = new ActivitySourceDto 
    { 
        System = "erp", 
        IdempotencyKey = "inv_332_payment" 
    }
};
```

## Best Practices

### TypeKey Naming
Use dot-notation for hierarchical type keys:
- `invoice.created`, `invoice.paid`, `invoice.voided`
- `build.started`, `build.completed`, `build.failed`
- `status.posted`, `status.edited`, `status.deleted`

### EntityRef Usage
- **Kind**: Broad category (`user`, `object`, `service`, `system`)
- **Type**: Specific type (`User`, `Invoice`, `CIServer`, `Webhook`)
- **Id**: Unique identifier within type
- **Display**: Human-readable name (optional but recommended)

### Payload Structure
- Keep payloads JSON-serializable
- Include enough context for the activity to be meaningful standalone
- Don't rely on external lookups to understand the activity

### Tags
- Use lowercase kebab-case: `payment`, `high-priority`, `urgent`
- Keep tags reusable across activity types
- Limit to 5-10 tags per activity

### Visibility
- **Internal** (default): Most common for tenant-scoped activities
- **Public**: Use sparingly for truly public data
- **Private**: Direct messages, private notes, sensitive operations

## See Also

- [ActivityStream.Core](../ActivityStream.Core/README.md) - Service implementation
- [ActivityStream.Store.InMemory](../ActivityStream.Store.InMemory/README.md) - Reference storage
- [Main README](../../README.md) - All libraries overview
