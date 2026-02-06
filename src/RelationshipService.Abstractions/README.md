# RelationshipService.Abstractions

Relationship DTOs, interfaces, and decision types for managing entity relationships and visibility.

## Overview

This library provides the foundational types for building relationship graph systems. It enables management of relationships between entities (Follow, Block, Mute, etc.) and deterministic visibility evaluation.

**NuGet:** `RelationshipService.Abstractions`  
**Dependencies:** None (pure .NET 8)

## Installation

```xml
<PackageReference Include="RelationshipService.Abstractions" Version="1.0.0" />
```

## Key Types

### RelationshipEdgeDto

Represents a directed edge from one entity to another:

```csharp
public class RelationshipEdgeDto
{
    public string? Id { get; set; }
    public required string TenantId { get; set; }
    public required EntityRefDto From { get; set; }
    public required EntityRefDto To { get; set; }
    public required RelationshipKind Kind { get; set; }
    public RelationshipScope Scope { get; set; }
    public RelationshipFilterDto? Filter { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
```

**Key Properties:**
- `TenantId` - Multi-tenancy partition key
- `From` - Source entity (e.g., viewer/follower)
- `To` - Target entity (e.g., followed user)
- `Kind` - Relationship type (Follow, Block, Mute, etc.)
- `Scope` - What activities the relationship applies to
- `Filter` - Optional filtering by TypeKey, tags, or visibility
- `ExpiresAt` - Optional expiration (e.g., temporary mutes)

### EntityRefDto

Universal entity reference:

```csharp
public class EntityRefDto
{
    public required string Kind { get; set; }    // "user", "object", "service"
    public required string Type { get; set; }    // "User", "Project", "Team"
    public required string Id { get; set; }      // "u_123", "proj_456"
    public string? Display { get; set; }         // "John Doe", "ACME Project"
}
```

### RelationshipKind

Types of relationships:

```csharp
public enum RelationshipKind
{
    Follow = 1,      // Follow a user/feed
    Subscribe = 2,   // Subscribe to notifications
    Block = 3,       // Hard block (deny visibility)
    Mute = 4,        // Soft hide (hidden but not denied)
    Allow = 5,       // Explicit permission grant
    Deny = 6         // Explicit permission denial
}
```

**Usage:**
- **Follow** - User follows another user's activities
- **Subscribe** - User subscribes to a feed/channel
- **Block** - Hard block preventing visibility
- **Mute** - Soft mute hiding activities (can be unmuted)
- **Allow** - Explicit permission to see private content
- **Deny** - Explicit denial of access

### RelationshipScope

Defines what part of an activity the relationship applies to:

```csharp
public enum RelationshipScope
{
    Any = 0,         // Matches if From or To appears anywhere in activity
    ActorOnly = 1,   // Only matches if entity is the Actor
    TargetOnly = 2,  // Only matches if entity is in Targets
    OwnerOnly = 3    // Only matches if entity is the Owner
}
```

**Examples:**
- **ActorOnly** - Follow only activities where they are the actor
- **TargetOnly** - Follow activities mentioning this entity
- **OwnerOnly** - Follow activities owned by this entity
- **Any** - Follow all activities involving this entity

### RelationshipFilterDto

Optional filtering criteria:

```csharp
public class RelationshipFilterDto
{
    public List<string>? TypeKeyPrefixes { get; set; }
    public List<string>? Tags { get; set; }
    public ActivityVisibility? Visibility { get; set; }
}
```

**Usage:**
```csharp
// Only follow build-related activities
new RelationshipFilterDto
{
    TypeKeyPrefixes = new List<string> { "build" }
}

// Only follow urgent activities
new RelationshipFilterDto
{
    Tags = new List<string> { "urgent" }
}

// Only follow public activities
new RelationshipFilterDto
{
    Visibility = ActivityVisibility.Public
}
```

### RelationshipDecision

Result of visibility evaluation:

```csharp
public class RelationshipDecision
{
    public required RelationshipDecisionKind Kind { get; init; }
    public required bool Allowed { get; init; }
    public required string Reason { get; init; }
    public RelationshipEdgeDto? Edge { get; init; }
}
```

### RelationshipDecisionKind

Why a decision was made:

```csharp
public enum RelationshipDecisionKind
{
    Default = 0,         // No specific rules, allow
    SelfAuthored = 1,    // User sees own activities
    Block = 2,           // Blocked by relationship
    Deny = 3,            // Explicit deny rule
    Visibility = 4,      // Activity visibility rules
    Mute = 5,            // Muted (hidden)
    Allow = 6            // Explicit allow rule
}
```

**Priority Order (Highest to Lowest):**
1. **SelfAuthored** - Always allowed
2. **Block** - Always denied
3. **Deny** - Explicitly denied
4. **Visibility** - Based on activity visibility
5. **Mute** - Hidden (not denied)
6. **Allow** - Explicitly allowed
7. **Default** - Allowed

### RelationshipQuery

Query parameters for finding relationships:

```csharp
public class RelationshipQuery
{
    public required string TenantId { get; set; }
    public EntityRefDto? From { get; set; }
    public EntityRefDto? To { get; set; }
    public RelationshipKind? Kind { get; set; }
    public RelationshipScope? Scope { get; set; }
    public int Limit { get; set; } = 100;
}
```

## Interfaces

### IRelationshipService

Main service interface:

```csharp
public interface IRelationshipService
{
    Task<RelationshipEdgeDto> UpsertAsync(
        RelationshipEdgeDto edge, CancellationToken ct = default);
    
    Task<bool> DeleteAsync(
        string tenantId, string edgeId, CancellationToken ct = default);
    
    Task<IReadOnlyList<RelationshipEdgeDto>> QueryAsync(
        RelationshipQuery query, CancellationToken ct = default);
    
    Task<RelationshipDecision> CanSeeAsync(
        string tenantId, EntityRefDto viewer, ActivityDto activity, 
        CancellationToken ct = default);
}
```

### IRelationshipStore

Storage abstraction:

```csharp
public interface IRelationshipStore
{
    Task<RelationshipEdgeDto> UpsertAsync(
        RelationshipEdgeDto edge, CancellationToken ct = default);
    
    Task<bool> DeleteAsync(
        string tenantId, string edgeId, CancellationToken ct = default);
    
    Task<IReadOnlyList<RelationshipEdgeDto>> QueryAsync(
        RelationshipQuery query, CancellationToken ct = default);
}
```

## Usage Examples

### Creating Relationships

```csharp
using RelationshipService.Abstractions;

// User A follows User B
var followEdge = new RelationshipEdgeDto
{
    TenantId = "acme",
    From = new EntityRefDto { Kind = "user", Type = "User", Id = "u_a" },
    To = new EntityRefDto { Kind = "user", Type = "User", Id = "u_b" },
    Kind = RelationshipKind.Follow,
    Scope = RelationshipScope.ActorOnly
};

// User A blocks User C
var blockEdge = new RelationshipEdgeDto
{
    TenantId = "acme",
    From = new EntityRefDto { Kind = "user", Type = "User", Id = "u_a" },
    To = new EntityRefDto { Kind = "user", Type = "User", Id = "u_c" },
    Kind = RelationshipKind.Block,
    Scope = RelationshipScope.Any
};

// User A mutes build notifications
var muteEdge = new RelationshipEdgeDto
{
    TenantId = "acme",
    From = new EntityRefDto { Kind = "user", Type = "User", Id = "u_a" },
    To = new EntityRefDto { Kind = "object", Type = "Project", Id = "proj_1" },
    Kind = RelationshipKind.Mute,
    Scope = RelationshipScope.Any,
    Filter = new RelationshipFilterDto
    {
        TypeKeyPrefixes = new List<string> { "build" }
    }
};
```

### Visibility Evaluation

```csharp
// Check if viewer can see an activity
var decision = await service.CanSeeAsync(
    tenantId: "acme",
    viewer: new EntityRefDto { Kind = "user", Type = "User", Id = "u_a" },
    activity: activityDto
);

if (decision.Allowed)
{
    if (decision.Kind == RelationshipDecisionKind.Mute)
    {
        // Activity is hidden but not denied - user can unhide
        ShowHiddenActivity(activityDto);
    }
    else
    {
        // Show activity normally
        ShowActivity(activityDto);
    }
}
else
{
    // Don't show activity
    // decision.Reason explains why
}
```

### Querying Relationships

```csharp
// Get all users that U_A follows
var following = await service.QueryAsync(new RelationshipQuery
{
    TenantId = "acme",
    From = new EntityRefDto { Kind = "user", Type = "User", Id = "u_a" },
    Kind = RelationshipKind.Follow
});

// Get all followers of U_B
var followers = await service.QueryAsync(new RelationshipQuery
{
    TenantId = "acme",
    To = new EntityRefDto { Kind = "user", Type = "User", Id = "u_b" },
    Kind = RelationshipKind.Follow
});

// Get all blocks
var blocks = await service.QueryAsync(new RelationshipQuery
{
    TenantId = "acme",
    From = new EntityRefDto { Kind = "user", Type = "User", Id = "u_a" },
    Kind = RelationshipKind.Block
});
```

## Best Practices

### Relationship Modeling

**Follow Relationships:**
```csharp
// Follow a user's own activities
new RelationshipEdgeDto
{
    From = follower,
    To = followedUser,
    Kind = RelationshipKind.Follow,
    Scope = RelationshipScope.ActorOnly  // Only when they're the actor
}

// Follow activities mentioning a user
new RelationshipEdgeDto
{
    From = follower,
    To = followedUser,
    Kind = RelationshipKind.Follow,
    Scope = RelationshipScope.TargetOnly  // When they're mentioned
}
```

**Block Relationships:**
```csharp
// Block all activities involving a user
new RelationshipEdgeDto
{
    From = blocker,
    To = blockedUser,
    Kind = RelationshipKind.Block,
    Scope = RelationshipScope.Any  // Actor, target, or owner
}
```

**Filtered Relationships:**
```csharp
// Only follow urgent activities
new RelationshipEdgeDto
{
    From = user,
    To = project,
    Kind = RelationshipKind.Follow,
    Scope = RelationshipScope.Any,
    Filter = new RelationshipFilterDto
    {
        Tags = new List<string> { "urgent" }
    }
}
```

### Scope Selection

- **ActorOnly** - Most common for follows (user's own actions)
- **TargetOnly** - For monitoring entities (mentions, tags)
- **OwnerOnly** - For team/project timelines
- **Any** - For blocks and comprehensive relationships

### Temporary Relationships

```csharp
// Mute for 24 hours
new RelationshipEdgeDto
{
    From = user,
    To = chatChannel,
    Kind = RelationshipKind.Mute,
    Scope = RelationshipScope.Any,
    ExpiresAt = DateTimeOffset.UtcNow.AddHours(24)
}
```

## Integration with ActivityStream

```csharp
// Build a personalized feed
var viewer = new EntityRefDto { Kind = "user", Type = "User", Id = "u_123" };

// Get all activities
var allActivities = await activityService.QueryAsync(new ActivityQuery
{
    TenantId = "acme",
    Limit = 100
});

// Filter by visibility
var visibleActivities = new List<ActivityDto>();
foreach (var activity in allActivities.Items)
{
    var decision = await relationshipService.CanSeeAsync("acme", viewer, activity);
    if (decision.Allowed && decision.Kind != RelationshipDecisionKind.Mute)
    {
        visibleActivities.Add(activity);
    }
}
```

## Error Handling

```csharp
try
{
    await service.UpsertAsync(edge);
}
catch (RelationshipValidationException ex)
{
    foreach (var error in ex.Errors)
    {
        Console.WriteLine($"{error.Field}: {error.Message}");
    }
}
```

## See Also

- [RelationshipService.Core](../RelationshipService.Core/README.md) - Service implementation
- [RelationshipService.Store.InMemory](../RelationshipService.Store.InMemory/README.md) - Reference storage
- [ActivityStream.Abstractions](../ActivityStream.Abstractions/README.md) - Activity types
- [Main README](../../README.md) - All libraries overview
