# Relationship Service (Agnostic) — C# Library Plan for an LLM Agent Programmer

**Goal:** Build an **agnostic Relationship Service** as a C# library (no HTTP, no UI, no framework ties).  
It manages **relationships (“edges”) between entities** represented by `EntityRefDto` and provides deterministic decisions such as:
- follow / subscribe
- block / mute
- allow/deny filters (optional but planned)
- “can viewer see this activity?” evaluation
- feed candidate expansion helpers (optional)

This plan is designed to be handed to an LLM coding agent (e.g., Claude Code) to implement end-to-end.

> **Assumption:** You already have `EntityRefDto`, `ActivityDto`, `ActivityVisibility`, and `IIdGenerator` from the Activity Stream library.
> **Decision:** For v1, `RelationshipService.Abstractions` directly references `ActivityStream.Abstractions`. No separate common package is created.

---

## 0) Definition of Done (v1 / MVP)

Deliverables (projects):

1. **RelationshipService.Abstractions**
   - DTOs + enums for relationships
   - Interfaces (service + store)
   - Decision result types + errors
   - **References `ActivityStream.Abstractions`** for `EntityRefDto`, `ActivityDto`, `ActivityVisibility`, and `IIdGenerator`

2. **RelationshipService.Core**
   - `RelationshipServiceImpl` implementing `IRelationshipService`
   - Normalization + validation
   - Deterministic evaluation engine (`CanSeeAsync`)

3. **RelationshipService.Store.InMemory**
   - Reference store implementing `IRelationshipStore`
   - Correctness > performance

4. **RelationshipService.Tests**
   - Edge create/upsert/remove tests
   - Validation tests
   - Visibility decision priority tests (SelfAuthored > Block > Deny > Visibility > Mute > Allow > Default)
   - Filter matching tests
   - Scope tests

Optional later:
- `RelationshipService.Store.Postgres`
- `Feed.Core` (ties ActivityStream + Relationship to build home feeds)

Success criteria:
- All tests green
- Deterministic decisions and priorities
- Relationship edges are idempotent (upsert semantics)
- Zero dependencies on web/UI frameworks

---

## 1) Core Concepts

### 1.1 Universal edges between entities
Everything is an `EntityRefDto`. Relationships are edges:
- **From**: the owner of the preference (viewer, subscriber)
- **To**: the subject (user, object, service, etc.)

### 1.2 Relationship types (kinds)
- **Follow**: viewer wants to see posts by/related to `To`
- **Subscribe**: viewer wants updates about a specific target/entity timeline
- **Block**: hard deny (strongest)
- **Mute**: soft hide (weaker than Block)
- **Allow / Deny**: filter rules controlling which activities are visible

### 1.3 Scope
A relationship can apply to:
- actor only
- targets only
- owner only
- any (default)

### 1.4 Deterministic evaluation engine
Given:
- `viewer` (EntityRefDto)
- `activity` (ActivityDto)
Return: allowed / denied + reason + (optional) decision kind.

### 1.5 EntityRefDto Equality Contract

Two `EntityRefDto` instances are considered equal when `Kind`, `Type`, and `Id` all match using:
- **Trimming**: leading/trailing whitespace is removed before comparison
- **Case-insensitive**: `OrdinalIgnoreCase` comparison for all three fields

This policy applies to:
- Edge uniqueness checks (upsert deduplication)
- Scope matching (does edge.To match activity.Actor/Target/Owner?)
- Visibility checks (is viewer the Actor/Owner/Target?)

> **Note:** This policy should also be documented in `ActivityStream_PLAN.md` for consistency.

### 1.6 TypeKey and Tag Matching Policy

- **TypeKey exact matches**: case-insensitive (OrdinalIgnoreCase)
- **TypeKey prefix matches**: case-insensitive
- **Tag matches** (RequiredTagsAny, ExcludedTagsAny): case-insensitive

### 1.7 Follow/Subscribe and Visibility

**Important:** `Follow` and `Subscribe` relationship kinds do **NOT** affect `CanSeeAsync` visibility decisions. They are used exclusively for feed candidate expansion (determining which entities' activities to include in a user's feed). Visibility is controlled by Block, Deny, Mute, Allow, and the activity's Visibility property.

---

## 2) DTOs (v1)

> DTOs must be plain and live in `RelationshipService.Abstractions`.

### 2.1 RelationshipKind

```csharp
public enum RelationshipKind
{
    Follow = 0,
    Subscribe = 1,
    Block = 2,
    Mute = 3,
    Allow = 4,
    Deny = 5
}
```

### 2.2 RelationshipScope

```csharp
public enum RelationshipScope
{
    Any = 0,
    ActorOnly = 1,
    TargetOnly = 2,
    OwnerOnly = 3
}
```

### 2.3 RelationshipFilterDto (optional but included in v1)
Filter is used mainly by Allow/Deny and optionally by Mute/Follow.

```csharp
public sealed class RelationshipFilterDto
{
    /// <summary>
    /// Match exact type keys (e.g., "invoice.paid").
    /// </summary>
    public List<string>? TypeKeys { get; set; }

    /// <summary>
    /// Match prefixes (e.g., "invoice.", "build.").
    /// </summary>
    public List<string>? TypeKeyPrefixes { get; set; }

    /// <summary>
    /// Require at least one of these tags to be present in activity.Tags.
    /// </summary>
    public List<string>? RequiredTagsAny { get; set; }

    /// <summary>
    /// If any of these tags present in activity.Tags, filter does NOT match.
    /// </summary>
    public List<string>? ExcludedTagsAny { get; set; }

    /// <summary>
    /// Optional visibility constraint.
    /// </summary>
    public List<ActivityVisibility>? AllowedVisibilities { get; set; }
}
```

### 2.4 RelationshipEdgeDto

```csharp
public sealed class RelationshipEdgeDto
{
    public string? Id { get; set; }

    public required string TenantId { get; set; }

    /// <summary>
    /// The owner of the preference (viewer/subscriber).
    /// </summary>
    public required EntityRefDto From { get; set; }

    /// <summary>
    /// The subject (user/object/service/etc).
    /// </summary>
    public required EntityRefDto To { get; set; }

    public required RelationshipKind Kind { get; set; }

    public RelationshipScope Scope { get; set; } = RelationshipScope.Any;

    /// <summary>
    /// Optional filter controlling what the relationship applies to.
    /// For Block, filter typically null (block everything).
    /// For Deny/Allow, filter is often used.
    /// </summary>
    public RelationshipFilterDto? Filter { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = default;
}
```

### 2.5 Decision result types

```csharp
public enum RelationshipDecisionKind
{
    Allowed = 0,
    Denied = 1,
    Hidden = 2 // e.g., mute: not “forbidden” but hidden from feeds
}

public sealed record RelationshipDecision(
    RelationshipDecisionKind Kind,
    bool Allowed,
    string Reason,
    string? MatchedEdgeId = null
);
```

### 2.6 Query DTOs

```csharp
public sealed class RelationshipQuery
{
    public required string TenantId { get; set; }

    public EntityRefDto? From { get; set; }
    public EntityRefDto? To { get; set; }

    public RelationshipKind? Kind { get; set; }
    public RelationshipScope? Scope { get; set; }

    public bool? IsActive { get; set; } = true;

    public int Limit { get; set; } = 200;
    public string? Cursor { get; set; } // optional, can be added later; v1 may omit paging
}
```

---

## 3) Interfaces

### 3.1 IRelationshipService

```csharp
public interface IRelationshipService
{
    Task<RelationshipEdgeDto> UpsertAsync(RelationshipEdgeDto edge, CancellationToken ct = default);

    Task RemoveAsync(string tenantId, string edgeId, CancellationToken ct = default);

    Task<IReadOnlyList<RelationshipEdgeDto>> QueryAsync(RelationshipQuery query, CancellationToken ct = default);

    /// <summary>
    /// Determines whether a viewer can see an activity (visibility + relationship graph).
    /// Deterministic rule priority.
    /// </summary>
    Task<RelationshipDecision> CanSeeAsync(
        string tenantId,
        EntityRefDto viewer,
        ActivityDto activity,
        CancellationToken ct = default);
}
```

### 3.2 IRelationshipStore

```csharp
public interface IRelationshipStore
{
    Task<RelationshipEdgeDto?> GetByIdAsync(string tenantId, string edgeId, CancellationToken ct = default);

    /// <summary>
    /// Find existing edge for upsert uniqueness.
    /// Uniqueness key recommendation: (TenantId, From, To, Kind, Scope).
    /// Filter may be treated as part of uniqueness only if you need multiple rules of same kind.
    /// For v1, keep it simple: one edge per (From,To,Kind,Scope).
    /// </summary>
    Task<RelationshipEdgeDto?> FindAsync(
        string tenantId,
        EntityRefDto from,
        EntityRefDto to,
        RelationshipKind kind,
        RelationshipScope scope,
        CancellationToken ct = default);

    Task UpsertAsync(RelationshipEdgeDto edge, CancellationToken ct = default);

    Task RemoveAsync(string tenantId, string edgeId, CancellationToken ct = default);

    Task<IReadOnlyList<RelationshipEdgeDto>> QueryAsync(RelationshipQuery query, CancellationToken ct = default);
}
```

---

## 4) Validation & Normalization

### 4.1 Required validation
- TenantId non-empty
- From and To are not null and have non-empty Kind/Type/Id
- Kind is valid enum
- Scope is valid enum
- If Filter present:
  - trim strings in TypeKeys/TypeKeyPrefixes/Tags
  - remove empty entries
  - dedupe case-insensitively

### 4.2 Normalization rules
Service must:
- Trim all string fields on DTOs (TenantId, Kind, Type, Id, Display, etc.)
- Ensure CreatedAt is set to `UtcNow` if default
- Generate Id if missing (reuse `IIdGenerator` or simple GUID generator inside Relationship.Core)
- Ensure IsActive defaults to true

### 4.3 Errors
Use a simple validation error record:

```csharp
public sealed record RelationshipValidationError(string Code, string Message, string? Path = null);
```

If validation fails, throw `RelationshipValidationException` containing errors.

---

## 5) Deterministic Visibility Algorithm (CanSeeAsync)

### 5.1 Matching rules
An edge “matches” an activity if:
- Edge is active
- Edge scope matches:
  - Any: matches if To matches either Actor OR any Target OR Owner
  - ActorOnly: To matches activity.Actor
  - TargetOnly: To matches any activity.Targets
  - OwnerOnly: To matches activity.Owner (if present)
- Filter matches (if filter is null => match-all)
  - TypeKeys exact match OR TypeKeyPrefixes prefix match (if provided)
  - RequiredTagsAny: activity.Tags contains at least one
  - ExcludedTagsAny: activity.Tags contains none
  - AllowedVisibilities includes activity.Visibility (if provided)

EntityRef equality:
- Match on Kind + Type + Id. **Policy:** normalize Kind/Type/Id by trimming; compare using `OrdinalIgnoreCase` for all three (documented).

### 5.2 Rule priority
**Priority order for decisions:**

0) **Self-authored** (highest priority)
   - If viewer == activity.Actor => Allowed (Reason: SelfAuthored)
   - Users always see their own activities regardless of any edges.

1) **Block** (hard deny)
   - If viewer has Block edge matching Actor/Target/Owner => Denied (Reason: Block)

2) **Deny** (rule-based deny)
   - If Deny edge matches => Denied (Reason: DenyRule)

3) **Visibility base policy**
   - If activity.Visibility == **Private**:
     - Allowed only if viewer equals:
       - activity.Actor (already handled in step 0), OR
       - activity.Owner (if present), OR
       - any entity in activity.Targets
     - Otherwise Denied (Reason: PrivateVisibility)
   - If activity.Visibility == **Internal**:
     - Allowed for all viewers within the same tenant (no additional check for v1)
   - If activity.Visibility == **Public**:
     - Allowed for all viewers

4) **Mute** (soft hide)
   - If Mute edge matches => Hidden (Allowed=false) with DecisionKind.Hidden (Reason: Mute)

5) **Allow** (explicit allow)
   - If Allow edge matches => Allowed (Reason: AllowRule)
   - Allow does NOT override Block/Deny.

6) **Default**
   - Allowed (Reason: Default)

### 5.3 Implementation approach
- Fetch viewer’s relevant edges with a single store query: `From = viewer, IsActive = true`.
- Partition edges by kind for fast evaluation.
- Evaluate kinds in priority order and return first match.

---

## 6) InMemory Store Requirements

Implement `RelationshipService.Store.InMemory` with:
- Dictionary by edgeId
- Secondary index for uniqueness key: `${tenant}|${fromKey}|${toKey}|${kind}|${scope}` -> edgeId
- Query scans and filters (correctness > perf)

EntityRef key function:
- `${kind}|${type}|${id}` with normalization (trim + lower)

Must support:
- Upsert by uniqueness key
- Remove by edgeId
- Query by tenant, from, to, kind, scope, isActive

---

## 7) Tests (Required)

### 7.1 Edge upsert/remove tests
- Upsert new edge creates it with Id and CreatedAt
- Upsert same (tenant,from,to,kind,scope) updates existing (keeps same Id)
- Remove removes it (hard delete for v1 InMemory)

### 7.2 Validation tests
- Missing TenantId fails
- Missing From/To fields fail
- Empty Kind/Type/Id fails
- Filter trims and removes empty entries

### 7.3 Decision priority tests
- SelfAuthored => Allowed even if Block exists on self
- Block actor => Denied even if Allow exists
- Deny matching prefix => Denied even if Allow matches too
- Private visibility allowed for Actor, Owner, and Targets
- Mute actor => Hidden (not Denied)
- Allow on target with matching filter => Allowed
- No edges => Allowed for Public/Internal

### 7.4 Filter matching tests
- Exact TypeKey match
- Prefix match
- RequiredTagsAny works
- ExcludedTagsAny blocks match
- AllowedVisibilities filters match

### 7.5 Scope tests
- ActorOnly triggers only on actor match
- TargetOnly triggers only when To equals any Targets entry
- OwnerOnly triggers only when Owner equals To
- Any triggers when To matches actor OR any target OR owner

---

## 8) Milestones (Agent Execution Order)

### M0 — Scaffold
- Create solution + projects
- Enable nullable, analyzers
- Add test framework (xUnit)

### M1 — Abstractions
- Implement DTOs and interfaces exactly
- Add error/exception types

### M2 — Core
- Implement validator + normalization
- Implement RelationshipService:
  - UpsertAsync
  - RemoveAsync
  - QueryAsync
  - CanSeeAsync evaluation engine

### M3 — InMemory Store
- Implement IRelationshipStore
- Ensure upsert uniqueness is correct

### M4 — Tests
- Add tests from Section 7
- All green

### M5 — README
- Document decision priority + equality policy
- Provide examples

---

## 9) Usage Examples

### 9.1 Follow a user
```csharp
await relationshipService.UpsertAsync(new RelationshipEdgeDto
{
    TenantId = "acme",
    From = new EntityRefDto { Kind="user", Type="User", Id="u_1" },
    To   = new EntityRefDto { Kind="user", Type="User", Id="u_2" },
    Kind = RelationshipKind.Follow,
    Scope = RelationshipScope.ActorOnly
});
```

### 9.2 Subscribe to an object timeline
```csharp
await relationshipService.UpsertAsync(new RelationshipEdgeDto
{
    TenantId = "acme",
    From = new EntityRefDto { Kind="user", Type="User", Id="u_1" },
    To   = new EntityRefDto { Kind="object", Type="Invoice", Id="inv_332" },
    Kind = RelationshipKind.Subscribe,
    Scope = RelationshipScope.TargetOnly
});
```

### 9.3 Block a user
```csharp
await relationshipService.UpsertAsync(new RelationshipEdgeDto
{
    TenantId = "acme",
    From = new EntityRefDto { Kind="user", Type="User", Id="u_1" },
    To   = new EntityRefDto { Kind="user", Type="User", Id="u_99" },
    Kind = RelationshipKind.Block,
    Scope = RelationshipScope.ActorOnly
});
```

### 9.4 Mute build.* activities from a service
```csharp
await relationshipService.UpsertAsync(new RelationshipEdgeDto
{
    TenantId = "acme",
    From = new EntityRefDto { Kind="user", Type="User", Id="u_1" },
    To   = new EntityRefDto { Kind="service", Type="CI", Id="ci_main" },
    Kind = RelationshipKind.Mute,
    Scope = RelationshipScope.ActorOnly,
    Filter = new RelationshipFilterDto
    {
        TypeKeyPrefixes = new() { "build." }
    }
});
```

### 9.5 Decide visibility
```csharp
var decision = await relationshipService.CanSeeAsync(
    tenantId: "acme",
    viewer: new EntityRefDto { Kind="user", Type="User", Id="u_1" },
    activity: activityDto);

if (decision.Allowed) { /* show */ }
else { /* hide */ }
```

---

## 10) Non-goals for v1
- Mutual block checks (actor blocks viewer)
- Complex audience ACL lists on activities (custom allow/deny per activity)
- Group/role expansion (teams, roles)
- Graph traversal beyond direct edges
- Feed materialization/fan-out
- Persistence beyond InMemory
- Cursor pagination for relationship queries

These can be added later while keeping the edge model stable.

> **Clarification on blocking:** If user A blocks user B, then A will not see B's activities. However, B can still see A's activities unless B also creates a Block edge targeting A. Mutual blocking is not automatically enforced.
