# Inbox Notification Service (Agnostic) — C# Library Plan for an LLM Agent Programmer

**Goal:** Build an **Inbox Notification Service** as a C# library (no HTTP, no UI, no framework ties).  
It converts **Activity Stream activities** into **inbox items** for recipients and supports **follow/subscribe requests** for entities that require approval.

Key requirements (from our discussion):
- **Any entity can own an inbox** (typically a *Profile* entity rather than a User; users can have many profiles; profiles can be shared by multiple users).
- **Private objects cannot be targeted by activities** (policy must prevent/flag this).
- Follow requests are approved by **owner(s)** or **moderator profile(s)** (profiles are entities, not hard-coded types).
- Inbox supports: unread/read, archive, actions (approve/deny), and **grouping/dedup**.
- Real-time push (SignalR) is **later**; v1 is inbox-only.

> **Assumptions:** You already have `EntityRefDto`, `ActivityDto`, `ActivityVisibility` (Activity Stream) and `RelationshipKind`, `RelationshipScope`, `RelationshipFilterDto` (Relationship Service).

---

## 0) Definition of Done (v1 / MVP)

### 0.1 Project References

```
Inbox.Abstractions
  └── ActivityStream.Abstractions (EntityRefDto, ActivityDto, ActivityVisibility, IIdGenerator)
  └── RelationshipService.Abstractions (RelationshipKind, RelationshipScope, RelationshipFilterDto)

Inbox.Core
  └── Inbox.Abstractions
  └── RelationshipService.Abstractions (IRelationshipService for CanSeeAsync)

Inbox.Store.InMemory
  └── Inbox.Abstractions

Inbox.Tests
  └── All of the above
```

### 0.2 Deliverables (projects)

1. **Inbox.Abstractions**
   - DTOs for inbox items, requests, grouping
   - Interfaces: service + stores + policy hooks
   - Result/Errors types

2. **Inbox.Core**
   - `InboxNotificationService` implementing `IInboxNotificationService`
   - Deterministic notification pipeline:
     - activity → recipient selection → permission check → inbox item creation → grouping/dedup
   - Follow/subscribe request workflow:
     - create request → notify approvers → approve/deny → create relationships → notify requester

3. **Inbox.Store.InMemory**
   - Reference store implementing `IInboxStore` and `IFollowRequestStore`
   - Correctness > performance

4. **Inbox.Tests**
   - Notification generation tests
   - Grouping/dedup tests
   - Request approval workflow tests
   - Policy enforcement tests (private objects cannot be targeted)
   - Read/Archive status tests

Optional later:
- `Inbox.Store.Postgres`
- `Inbox.Realtime.SignalR` (push updates)

Success criteria:
- All tests green
- Deterministic grouping and dedup behavior
- Request workflow produces correct relationship edges
- Policy hooks allow “profiles shared between users” without hardcoding users/profiles into the library

---

## 1) Core Concepts

### 1.1 Inbox owners are entities (not users)
Inbox owner is an `EntityRefDto` (commonly a Profile entity).
- A “User” may view multiple inboxes (their profiles).
- A “Profile” may be shared by multiple users.
The library does **not** model users; it stores items *per inbox owner entity*.
**EntityRef conventions** (aligned with IdentityProfiles §4):
```csharp
// Profile inbox owner (most common)
new EntityRefDto { Kind = "identity", Type = "Profile", Id = profileId }

// User (rarely used as inbox owner; typically internal)
new EntityRefDto { Kind = "identity", Type = "User", Id = userId }
```
### 1.2 Events that create inbox items
- **Activity published** → notification inbox items for recipients
- **Follow request created** → request inbox items for approvers
- **Request approved/denied** → notification to requester

### 1.3 Grouping and dedup
- **DedupKey** prevents duplicate items (e.g., retries)
- **ThreadKey** groups multiple events into one thread (e.g., “5 comments on Invoice #332”)

### 1.4 Policy-driven privacy / governance
Because “private objects cannot be targeted” and approvers vary by domain, the service relies on **policy hooks** implemented by the host application.

---

## 2) DTOs (v1)

### 2.1 InboxItemKind, InboxItemStatus

```csharp
public enum InboxItemKind
{
    Notification = 0,
    Request = 1
}

public enum InboxItemStatus
{
    Unread = 0,
    Read = 1,
    Archived = 2
}
```

### 2.2 InboxEventRefDto

```csharp
public sealed class InboxEventRefDto
{
    /// <summary>
    /// "activity" | "follow-request" | etc
    /// </summary>
    public required string Kind { get; set; }

    /// <summary>
    /// Id of the referenced event (activityId, requestId).
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Convenience: activity.TypeKey when Kind == "activity".
    /// </summary>
    public string? TypeKey { get; set; }

    public DateTimeOffset? OccurredAt { get; set; }
}
```

### 2.3 InboxItemDto

```csharp
public sealed class InboxItemDto
{
    public string? Id { get; set; }
    public required string TenantId { get; set; }

    /// <summary>
    /// Inbox owner entity (typically a Profile).
    /// </summary>
    public required EntityRefDto Recipient { get; set; }

    public required InboxItemKind Kind { get; set; }
    public required InboxEventRefDto Event { get; set; }

    public string? Title { get; set; }
    public string? Body { get; set; }

    /// <summary>
    /// Related entities for “open” actions (invoice, project, thread, actor, etc).
    /// </summary>
    public List<EntityRefDto> Targets { get; set; } = new();

    /// <summary>
    /// Additional structured context (optional).
    /// </summary>
    public Dictionary<string, object?>? Data { get; set; }

    public InboxItemStatus Status { get; set; } = InboxItemStatus.Unread;

    /// <summary>
    /// Used for deduping repeated generation.
    /// Example: "activity:{activityId}:recipient:{recipientKey}"
    /// </summary>
    public string? DedupKey { get; set; }

    /// <summary>
    /// Used for grouping threads.
    /// Example: "target:Invoice:inv_332:type:comment"
    /// </summary>
    public string? ThreadKey { get; set; }

    public int ThreadCount { get; set; } = 1;

    public DateTimeOffset CreatedAt { get; set; } = default;
    public DateTimeOffset? UpdatedAt { get; set; }
}
```

### 2.4 Inbox queries (read model)

```csharp
public sealed class InboxQuery
{
    public required string TenantId { get; set; }

    /// <summary>
    /// Query a single inbox owner (Profile) OR multiple owners (user has many profiles).
    /// </summary>
    public List<EntityRefDto> Recipients { get; set; } = new();

    public InboxItemStatus? Status { get; set; } // e.g. Unread only
    public InboxItemKind? Kind { get; set; }

    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }

    public int Limit { get; set; } = 50;
    public string? Cursor { get; set; }
}

public sealed class InboxPageResult
{
    public List<InboxItemDto> Items { get; set; } = new();
    public string? NextCursor { get; set; }
}
```

### 2.5 FollowRequestDto (approval workflow)

```csharp
public enum FollowRequestStatus
{
    Pending = 0,
    Approved = 1,
    Denied = 2,
    Cancelled = 3
}

public sealed class FollowRequestDto
{
    public string? Id { get; set; }
    public required string TenantId { get; set; }

    /// <summary>
    /// Requester (typically a Profile, not necessarily a User).
    /// </summary>
    public required EntityRefDto Requester { get; set; }

    /// <summary>
    /// The entity to follow/subscribe (may be private).
    /// </summary>
    public required EntityRefDto Target { get; set; }

    public required RelationshipKind RequestedKind { get; set; } // Follow or Subscribe

    /// <summary>
    /// Scope for the resulting relationship edge.
    /// Default: ActorOnly for Follow, TargetOnly for Subscribe.
    /// </summary>
    public RelationshipScope Scope { get; set; } = RelationshipScope.ActorOnly;
    public RelationshipFilterDto? Filter { get; set; }

    public FollowRequestStatus Status { get; set; } = FollowRequestStatus.Pending;

    public EntityRefDto? DecidedBy { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public string? DecisionReason { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = default;

    /// <summary>
    /// Dedup for repeated UI submissions.
    /// Unique per (tenant, requester, target, requestedKind, scope).
    /// </summary>
    public string? IdempotencyKey { get; set; }
}
```

---

## 3) Interfaces

### 3.1 IInboxNotificationService

**Constructor dependencies:**
```csharp
public InboxNotificationService(
    IInboxStore inboxStore,
    IFollowRequestStore requestStore,
    IRelationshipService relationshipService,  // for CanSeeAsync + edge creation
    IEntityGovernancePolicy governancePolicy,
    IIdGenerator idGenerator,
    IRecipientExpansionPolicy? recipientExpansion = null);
```

```csharp
public interface IInboxNotificationService
{
    // Activity → notifications
    Task OnActivityPublishedAsync(ActivityDto activity, CancellationToken ct = default);

    // Inbox operations
    Task<InboxItemDto> AddAsync(InboxItemDto item, CancellationToken ct = default);
    Task<InboxPageResult> QueryInboxAsync(InboxQuery query, CancellationToken ct = default);
    Task MarkReadAsync(string tenantId, string inboxItemId, CancellationToken ct = default);
    Task ArchiveAsync(string tenantId, string inboxItemId, CancellationToken ct = default);

    // Follow/subscribe request workflow
    Task<FollowRequestDto> CreateFollowRequestAsync(FollowRequestDto request, CancellationToken ct = default);
    Task<FollowRequestDto> ApproveRequestAsync(
        string tenantId, string requestId, EntityRefDto decidedBy, string? reason, CancellationToken ct = default);
    Task<FollowRequestDto> DenyRequestAsync(
        string tenantId, string requestId, EntityRefDto decidedBy, string? reason, CancellationToken ct = default);
}
```

### 3.2 Stores

```csharp
public interface IInboxStore
{
    Task<InboxItemDto?> GetByIdAsync(string tenantId, string id, CancellationToken ct = default);

    Task<InboxItemDto?> FindByDedupKeyAsync(string tenantId, EntityRefDto recipient, string dedupKey, CancellationToken ct = default);

    /// <summary>
    /// If ThreadKey is used, this allows group-updating a thread item.
    /// </summary>
    Task<InboxItemDto?> FindByThreadKeyAsync(string tenantId, EntityRefDto recipient, string threadKey, CancellationToken ct = default);

    Task UpsertAsync(InboxItemDto item, CancellationToken ct = default);

    Task<InboxPageResult> QueryAsync(InboxQuery query, CancellationToken ct = default);

    Task UpdateStatusAsync(string tenantId, string id, InboxItemStatus status, CancellationToken ct = default);
}

public interface IFollowRequestStore
{
    Task<FollowRequestDto?> GetByIdAsync(string tenantId, string requestId, CancellationToken ct = default);

    Task<FollowRequestDto?> FindByIdempotencyAsync(
        string tenantId, string idempotencyKey, CancellationToken ct = default);

    Task UpsertAsync(FollowRequestDto request, CancellationToken ct = default);

    Task<IReadOnlyList<FollowRequestDto>> QueryPendingForTargetAsync(
        string tenantId, EntityRefDto target, CancellationToken ct = default);
}
```

---

## 4) Policy Hooks (critical to keep library agnostic)

### 4.1 IEntityGovernancePolicy

```csharp
public interface IEntityGovernancePolicy
{
    /// <summary>
    /// Private objects cannot be targeted by activities (your rule).
    /// Return true if this entity is allowed to appear in ActivityDto.Targets or Owner.
    /// </summary>
    Task<bool> IsTargetableAsync(string tenantId, EntityRefDto entity, CancellationToken ct = default);

    /// <summary>
    /// Should following/subscribing require approval?
    /// </summary>
    Task<bool> RequiresApprovalToFollowAsync(
        string tenantId, EntityRefDto requester, EntityRefDto target,
        RelationshipKind requestedKind, CancellationToken ct = default);

    /// <summary>
    /// Returns the approver inbox owners (profiles) for this target.
    /// Usually: owner profile(s) and/or moderator profile(s).
    /// </summary>
    Task<IReadOnlyList<EntityRefDto>> GetApproversAsync(
        string tenantId, EntityRefDto target, CancellationToken ct = default);
}
```

### 4.2 IRecipientExpansionPolicy (optional)
Default implementation returns `[recipient]`.

```csharp
public interface IRecipientExpansionPolicy
{
    Task<IReadOnlyList<EntityRefDto>> ExpandRecipientsAsync(
        string tenantId, EntityRefDto recipient, CancellationToken ct = default);
}
```

---

## 5) Notification Pipeline (Activity → Inbox Items)

### 5.1 Policy enforcement: “private objects cannot be targeted”
Before generating notifications:
- For `activity.Actor`, each `activity.Targets`, and `activity.Owner` (if present):
  - call `IsTargetableAsync`
  - if any returns false:
    - **v1 recommended behavior:** throw `InboxPolicyViolationException`
    - test: no inbox items created if exception occurs

> **Note:** Actor is also checked because a private service/bot posting publicly would violate governance.

### 5.2 Recipient selection strategy (v1)
Recipients are derived from RelationshipService edges via `IRelationshipService.QueryAsync`:

**Query pattern for followers of Actor:**
```csharp
var followers = await relationshipService.QueryAsync(new RelationshipQuery
{
    TenantId = activity.TenantId,
    To = activity.Actor,           // edges pointing TO the actor
    Kind = RelationshipKind.Follow,
    IsActive = true
});
// Recipients = followers.Select(e => e.From)
```

**Query pattern for subscribers of Targets:**
```csharp
foreach (var target in activity.Targets)
{
    var subscribers = await relationshipService.QueryAsync(new RelationshipQuery
    {
        TenantId = activity.TenantId,
        To = target,
        Kind = RelationshipKind.Subscribe,
        IsActive = true
    });
    // Recipients += subscribers.Select(e => e.From)
}
```

- Optional: Subscribers of `activity.Owner` (same pattern)

Then optionally apply `IRecipientExpansionPolicy` to each recipient.

> **v1 default:** `IRecipientExpansionPolicy` returns `[recipient]` unchanged. Team→members expansion can be added later.

### 5.3 Permission check
For each candidate recipient:
- call `RelationshipService.CanSeeAsync(tenantId, recipient, activity)`
- if Allowed: proceed
- if Hidden/Denied: skip

### 5.4 Grouping/dedup
Default keys:
- `DedupKey = "activity:{activity.Id}:recipient:{recipientKey}"`
- `ThreadKey` recommended default: group by **(first target if any)** + **typeKey prefix**
  - prefix = substring up to first '.' (e.g., `invoice.` from `invoice.paid`)
  - `ThreadKey = "target:{Type}:{Id}:type:{prefix}"`
If no targets, use actor grouping:
- `ThreadKey = "actor:{Actor.Type}:{Actor.Id}:type:{prefix}"`

Algorithm:
1. If ThreadKey present and existing thread item found:
   - increment ThreadCount
   - set UpdatedAt = UtcNow
   - optionally set Status = Unread (configurable; see follow-up)
   - return
2. Else if DedupKey exists:
   - do nothing
3. Else insert new item

---

## 6) Follow/Subscribe Request Workflow

### 6.1 Create request
1. Normalize + validate request
2. If `RequiresApprovalToFollowAsync` == false:
   - create relationship edge immediately (via RelationshipService.UpsertAsync)
   - notify requester (Notification item: “follow enabled”)
3. Else:
   - store request as Pending (idempotent via IdempotencyKey)
   - `GetApproversAsync` → recipients (profiles/entities)
   - create Request inbox items for approvers
   - optional: notify requester “request sent”

### 6.2 Approve request
1. Load request; ensure Pending
2. Mark Approved + DecidedBy/DecidedAt + reason
3. Create relationship edge:
   - From = Requester, To = Target, Kind = RequestedKind, Scope/Filter copied
4. Notify requester: “Approved”
5. Optionally update/close approver request item thread

### 6.3 Deny request
- Mark Denied + reason
- Notify requester: “Denied”

---

## 7) Cursor pagination (Inbox)
Order by:
- `CreatedAt DESC`, tie-breaker `Id DESC`
Cursor:
- Base64Url encode of `"{createdAt:O}|{id}"`

---

## 8) InMemory Store Requirements
### 8.1 Inbox
- store items by tenant+recipientKey
- index dedupKey and threadKey per recipient
- query across **multiple recipients**:
  - merge + sort + page

### 8.2 Follow Requests
- store by id
- index by idempotencyKey
- query pending for target

---

## 9) Tests (Required)
- Activity triggers inbox notifications for followers/subscribers
- Dedup prevents duplicates
- ThreadKey grouping increments ThreadCount
- RelationshipService Hidden/Denied recipients do not receive items
- Policy violation when activity targets non-targetable entity
- Create request sends to approvers
- Approve creates relationship edge and notifies requester
- Deny notifies requester
- MarkRead / Archive changes status
- Query across multiple recipients returns merged paging correctly

---

## 10) Milestones
- M0 Scaffold
- M1 Abstractions
- M2 Core pipeline + request workflow
- M3 InMemory stores
- M4 Tests
- M5 README

---

## 11) Decisions (locked for v1)

| # | Decision | Choice | Rationale |
|---|----------|--------|----------|
| 1 | **Threading strategy** | Group by first target + typeKey prefix; fallback actor + prefix when no targets | Natural grouping (e.g., "5 comments on Invoice #332") |
| 2 | **Unread bump behavior** | Stay Read, move to top (UpdatedAt changes) | Less noisy; user already saw thread. Position change is sufficient signal. |
| 3 | **Multiple approvers model** | Any single approver can approve/deny | Simplest for v1; avoids UX complexity of majority/unanimous |
| 4 | **Edge creation on approval** | Create only Follow/Subscribe edge | Keep simple; Allow edges can be added explicitly by app layer if needed |
| 5 | **Recipient expansion** | Default `[recipient]` (no expansion in v1) | Ship MVP; expansion via `IRecipientExpansionPolicy` can be added later |
