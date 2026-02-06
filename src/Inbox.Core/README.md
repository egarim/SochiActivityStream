# Inbox.Core

Core implementation of the inbox notification and follow/subscribe request service.

## Overview

This library provides the core service implementation for:
- Activity fan-out to followers and subscribers
- Inbox item management with grouping and deduplication
- Follow/subscribe request workflow with approval support
- Permission checking via RelationshipService

**NuGet:** `Inbox.Core`  
**Dependencies:** `Inbox.Abstractions`, `RelationshipService.Abstractions`

## Installation

```xml
<PackageReference Include="Inbox.Core" Version="1.0.0" />
```

## Key Components

### InboxNotificationService

The main service implementing `IInboxNotificationService`:

```csharp
var service = new InboxNotificationService(
    inboxStore,           // IInboxStore
    followRequestStore,   // IFollowRequestStore
    relationshipService,  // IRelationshipService
    governancePolicy,     // IEntityGovernancePolicy
    idGenerator,          // IIdGenerator (from ActivityStream.Abstractions)
    recipientExpansion    // IRecipientExpansionPolicy (optional)
);
```

### Activity Fan-out Pipeline

When `OnActivityPublishedAsync` is called:

1. **Policy Enforcement** - Checks Actor, Targets, and Owner are targetable
2. **Recipient Selection** - Gets followers of Actor and subscribers of Targets
3. **Permission Filtering** - For each recipient:
   - Check visibility permissions via RelationshipService
   - Skip Hidden (Muted) or Denied recipients
4. **Inbox Item Creation** - Create items with proper DedupKey and ThreadKey

### DefaultRecipientExpansionPolicy

Returns recipients unchanged (v1 default). Override for team expansion:

```csharp
public class TeamExpansionPolicy : IRecipientExpansionPolicy
{
    public async Task<IReadOnlyList<EntityRefDto>> ExpandRecipientsAsync(
        string tenantId, EntityRefDto recipient, CancellationToken ct)
    {
        if (recipient.Type == "Team")
            return await GetTeamMembers(tenantId, recipient.Id);
        return new[] { recipient };
    }
}
```

### Helpers

**CursorHelper** - Base64Url cursor encoding:

```csharp
// Cursor format: "{createdAt:O}|{id}"
var cursor = CursorHelper.Encode(item.CreatedAt, item.Id);
var (createdAt, id) = CursorHelper.Decode(cursor);
```

**EntityRefKeyHelper** - Consistent key generation:

```csharp
var key = EntityRefKeyHelper.ToKey(entity); // "identity|profile|p_123"
var dedupKey = EntityRefKeyHelper.BuildActivityDedupKey(activity);
var threadKey = EntityRefKeyHelper.BuildThreadKey(typeKey, target);
```

**InboxNormalizer** - Consistent normalization:

```csharp
InboxNormalizer.Normalize(inboxItem);  // Trims and lowercases
InboxNormalizer.Normalize(request);    // For FollowRequestDto
```

**InboxValidator** - Validation:

```csharp
var errors = InboxValidator.ValidateInboxItem(item);
var errors = InboxValidator.ValidateFollowRequest(request);
```

## Request Workflow

### Auto-Approval (Default)

When `RequiresApprovalToFollowAsync` returns `false`:

1. Create relationship edge immediately
2. Mark request as Approved
3. Notify requester

### Manual Approval

When `RequiresApprovalToFollowAsync` returns `true`:

1. Store request as Pending
2. Notify approvers (from `GetApproversAsync`)
3. Wait for `ApproveRequestAsync` or `DenyRequestAsync`
4. Create edge on approval, notify requester

## Usage

### Basic Setup

```csharp
// Create stores (use InMemory for dev, real stores for prod)
var inboxStore = new InMemoryInboxStore();
var requestStore = new InMemoryFollowRequestStore();
var relationshipStore = new InMemoryRelationshipStore();

// Create services
var relationshipService = new RelationshipServiceImpl(relationshipStore, new UlidIdGenerator());
var governancePolicy = new MyGovernancePolicy(); // Implement IEntityGovernancePolicy

var inboxService = new InboxNotificationService(
    inboxStore,
    requestStore,
    relationshipService,
    governancePolicy,
    new UlidIdGenerator());
```

### Processing Activities

```csharp
// When an activity is published in your system:
await inboxService.OnActivityPublishedAsync(activity);
```

### Grouping Behavior

**DedupKey** prevents duplicates:
- Same `DedupKey` for same recipient → returns existing item

**ThreadKey** groups items:
- Same `ThreadKey` → increments `ThreadCount` on existing item
- Last event replaces `Event` reference
- Status (Read/Archived) is preserved

## Thread Safety

The service is thread-safe when used with thread-safe stores. `InMemoryInboxStore` and `InMemoryFollowRequestStore` use `ConcurrentDictionary` with locks for compound operations.

## Testing

```csharp
public class InboxTests
{
    private readonly InboxNotificationService _service;
    
    public InboxTests()
    {
        _service = new InboxNotificationService(
            new InMemoryInboxStore(),
            new InMemoryFollowRequestStore(),
            new RelationshipServiceImpl(new InMemoryRelationshipStore(), new UlidIdGenerator()),
            new TestGovernancePolicy(),
            new UlidIdGenerator());
    }
    
    [Fact]
    public async Task Activity_creates_inbox_items_for_followers()
    {
        // Setup follow relationship
        // Publish activity
        // Assert inbox item created
    }
}
```
