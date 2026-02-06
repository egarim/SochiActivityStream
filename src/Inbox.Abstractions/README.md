# Inbox.Abstractions

DTOs, interfaces, and exception types for the inbox notification and follow/subscribe request system.

## Overview

This library provides the foundational types for building inbox notification systems that:
- Fan-out activities to followers and subscribers
- Support grouping and deduplication of notifications
- Manage follow/subscribe request workflows with approval
- Enforce governance policies on targetable entities

**NuGet:** `Inbox.Abstractions`  
**Dependencies:** `ActivityStream.Abstractions`, `RelationshipService.Abstractions`

## Installation

```xml
<PackageReference Include="Inbox.Abstractions" Version="1.0.0" />
```

## Key Types

### InboxItemDto

Represents a notification or request item in a user's inbox:

```csharp
public sealed class InboxItemDto
{
    public string? Id { get; set; }
    public required string TenantId { get; set; }
    public required EntityRefDto Recipient { get; set; }
    public InboxItemKind Kind { get; set; }
    public InboxItemStatus Status { get; set; }
    public required InboxEventRefDto Event { get; set; }
    public string? DedupKey { get; set; }
    public string? ThreadKey { get; set; }
    public int ThreadCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
```

**Key Properties:**
- `TenantId` - Multi-tenancy partition key
- `Recipient` - The profile/entity receiving this inbox item
- `Kind` - Type of item (Notification or Request)
- `Status` - Current status (Unread, Read, Archived)
- `Event` - Reference to the activity or request event
- `DedupKey` - Key for deduplication (same key = same item)
- `ThreadKey` - Key for grouping items into a thread
- `ThreadCount` - Number of items grouped into this thread

### InboxItemKind

Types of inbox items:

```csharp
public enum InboxItemKind
{
    Notification = 0,  // Notification about an activity
    Request = 1        // Request requiring action (follow approvals)
}
```

### InboxItemStatus

Status of an inbox item:

```csharp
public enum InboxItemStatus
{
    Unread = 0,   // New item, not yet read
    Read = 1,     // User has seen the item
    Archived = 2  // User archived/dismissed the item
}
```

### InboxEventRefDto

Lightweight reference to an event/activity:

```csharp
public sealed class InboxEventRefDto
{
    public required string Kind { get; set; }  // "activity", "follow_request"
    public required string Id { get; set; }    // Event identifier
}
```

### FollowRequestDto

Represents a pending follow/subscribe request:

```csharp
public sealed class FollowRequestDto
{
    public string? Id { get; set; }
    public required string TenantId { get; set; }
    public required EntityRefDto Requester { get; set; }
    public required EntityRefDto Target { get; set; }
    public required RelationshipKind RequestedKind { get; set; }
    public RelationshipScope Scope { get; set; }
    public FollowRequestStatus Status { get; set; }
    public EntityRefDto? DecidedBy { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public string? DecisionReason { get; set; }
    public string? IdempotencyKey { get; set; }
}
```

### FollowRequestStatus

Status of a follow/subscribe request:

```csharp
public enum FollowRequestStatus
{
    Pending = 0,   // Awaiting approval
    Approved = 1,  // Request approved, edge created
    Denied = 2     // Request denied
}
```

### InboxQuery

Query parameters for retrieving inbox items:

```csharp
public sealed class InboxQuery
{
    public required string TenantId { get; set; }
    public List<EntityRefDto> Recipients { get; set; }
    public InboxItemStatus? Status { get; set; }
    public InboxItemKind? Kind { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public int Limit { get; set; } = 50;
    public string? Cursor { get; set; }
}
```

## Interfaces

### IInboxNotificationService

Main service interface for inbox operations:

```csharp
public interface IInboxNotificationService
{
    // Activity fan-out
    Task OnActivityPublishedAsync(ActivityDto activity, CancellationToken ct = default);
    
    // Direct inbox operations
    Task<InboxItemDto> AddAsync(InboxItemDto item, CancellationToken ct = default);
    Task<InboxPageResult> QueryInboxAsync(InboxQuery query, CancellationToken ct = default);
    Task MarkReadAsync(string tenantId, string itemId, CancellationToken ct = default);
    Task ArchiveAsync(string tenantId, string itemId, CancellationToken ct = default);
    
    // Follow/Subscribe request workflow
    Task<FollowRequestDto> CreateFollowRequestAsync(FollowRequestDto request, CancellationToken ct = default);
    Task<FollowRequestDto> ApproveRequestAsync(string tenantId, string requestId, EntityRefDto decidedBy, string? reason, CancellationToken ct = default);
    Task<FollowRequestDto> DenyRequestAsync(string tenantId, string requestId, EntityRefDto decidedBy, string? reason, CancellationToken ct = default);
}
```

### IInboxStore

Storage interface for inbox items:

```csharp
public interface IInboxStore
{
    Task<InboxItemDto?> GetByIdAsync(string tenantId, string id, CancellationToken ct = default);
    Task<InboxItemDto?> FindByDedupKeyAsync(string tenantId, EntityRefDto recipient, string dedupKey, CancellationToken ct = default);
    Task<InboxItemDto?> FindByThreadKeyAsync(string tenantId, EntityRefDto recipient, string threadKey, CancellationToken ct = default);
    Task UpsertAsync(InboxItemDto item, CancellationToken ct = default);
    Task<IReadOnlyList<InboxItemDto>> QueryAsync(InboxQuery query, CancellationToken ct = default);
}
```

### IFollowRequestStore

Storage interface for follow/subscribe requests:

```csharp
public interface IFollowRequestStore
{
    Task<FollowRequestDto?> GetByIdAsync(string tenantId, string id, CancellationToken ct = default);
    Task<FollowRequestDto?> FindByIdempotencyAsync(string tenantId, string idempotencyKey, CancellationToken ct = default);
    Task UpsertAsync(FollowRequestDto request, CancellationToken ct = default);
}
```

### IEntityGovernancePolicy

Policy interface for controlling follow/subscribe behavior:

```csharp
public interface IEntityGovernancePolicy
{
    Task<bool> IsTargetableAsync(string tenantId, EntityRefDto entity, CancellationToken ct = default);
    Task<bool> RequiresApprovalToFollowAsync(string tenantId, EntityRefDto requester, EntityRefDto target, RelationshipKind requestedKind, CancellationToken ct = default);
    Task<IReadOnlyList<EntityRefDto>> GetApproversAsync(string tenantId, EntityRefDto target, CancellationToken ct = default);
}
```

### IRecipientExpansionPolicy

Policy for expanding recipients (e.g., team → members):

```csharp
public interface IRecipientExpansionPolicy
{
    Task<IReadOnlyList<EntityRefDto>> ExpandRecipientsAsync(string tenantId, EntityRefDto recipient, CancellationToken ct = default);
}
```

## Exceptions

### InboxValidationException

Thrown when validation fails:

```csharp
throw new InboxValidationException("REQUIRED", "TenantId is required.", "TenantId");
```

### InboxPolicyViolationException

Thrown when governance policy is violated:

```csharp
throw new InboxPolicyViolationException(entity, "NOT_TARGETABLE");
```

## Usage Examples

### Publishing an Activity

```csharp
var activity = new ActivityDto
{
    TenantId = "acme",
    TypeKey = "comment.created",
    Actor = new EntityRefDto { Kind = "identity", Type = "Profile", Id = "p_123" },
    Targets = new List<EntityRefDto> { new() { Kind = "object", Type = "Invoice", Id = "inv_456" } },
    Visibility = ActivityVisibility.Internal,
    Payload = new { comment = "Great work!" }
};

await inboxService.OnActivityPublishedAsync(activity);
// Fan-out to followers of actor and subscribers of target
```

### Querying Inbox

```csharp
var result = await inboxService.QueryInboxAsync(new InboxQuery
{
    TenantId = "acme",
    Recipients = new List<EntityRefDto> { myProfile },
    Status = InboxItemStatus.Unread,
    Limit = 20
});

foreach (var item in result.Items)
{
    Console.WriteLine($"{item.Kind}: {item.Event.Id} ({item.ThreadCount} grouped)");
}

// Pagination
if (result.NextCursor is not null)
{
    var nextPage = await inboxService.QueryInboxAsync(new InboxQuery
    {
        TenantId = "acme",
        Recipients = new List<EntityRefDto> { myProfile },
        Cursor = result.NextCursor
    });
}
```

### Follow Request Workflow

```csharp
// Create request
var request = await inboxService.CreateFollowRequestAsync(new FollowRequestDto
{
    TenantId = "acme",
    Requester = new EntityRefDto { Kind = "identity", Type = "Profile", Id = "p_123" },
    Target = new EntityRefDto { Kind = "identity", Type = "Profile", Id = "p_456" },
    RequestedKind = RelationshipKind.Follow,
    IdempotencyKey = "follow_p123_p456"
});

// If auto-approved, Status == Approved. Otherwise Pending.

// Approve (for pending requests)
var approved = await inboxService.ApproveRequestAsync(
    "acme", request.Id!, approverProfile, "Welcome!");

// Or deny
var denied = await inboxService.DenyRequestAsync(
    "acme", request.Id!, approverProfile, "Not accepting new followers.");
```

### Marking Items Read/Archived

```csharp
await inboxService.MarkReadAsync("acme", itemId);
await inboxService.ArchiveAsync("acme", itemId);
```

## Grouping and Deduplication

### DedupKey

Prevents duplicate inbox items. If an item with the same `DedupKey` already exists for a recipient, the existing item is returned:

```csharp
var item = new InboxItemDto
{
    TenantId = "acme",
    Recipient = myProfile,
    Event = new InboxEventRefDto { Kind = "activity", Id = "act_123" },
    DedupKey = "act:act_123" // Same activity won't create duplicate
};
```

### ThreadKey

Groups related items together, incrementing `ThreadCount`:

```csharp
// Multiple comments on same invoice group together
var item = new InboxItemDto
{
    TenantId = "acme",
    Recipient = myProfile,
    Event = new InboxEventRefDto { Kind = "activity", Id = "act_456" },
    ThreadKey = "target:Invoice:inv_123:type:comment"
};
// ThreadCount increments each time a new event is added to same thread
```

## Decision Reference

| # | Decision | Rationale |
|---|----------|-----------|
| 1 | DedupKey format | `{typeKey}:{activityId}` for activity-based dedup |
| 2 | ThreadKey update preserves status | Keeps Read/Archived status when new events group |
| 3 | Scope default = ActorOnly | Follow relationships typically Actor-scoped |
| 4 | IEntityGovernancePolicy required | Enables private/VIP entity protection |
| 5 | IdempotencyKey for requests | Prevents duplicate follow requests |
