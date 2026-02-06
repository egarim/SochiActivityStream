# RelationshipService.Core

Service implementation with deterministic visibility rules for managing relationship graphs.

## Overview

This library provides the core service implementation that orchestrates relationship validation, storage, and visibility evaluation with deterministic decision-making.

**NuGet:** `RelationshipService.Core`  
**Dependencies:** `RelationshipService.Abstractions`, NUlid

## Installation

```xml
<PackageReference Include="RelationshipService.Core" Version="1.0.0" />
<PackageReference Include="RelationshipService.Abstractions" Version="1.0.0" />
```

## Key Classes

### RelationshipServiceImpl

Main service implementation of `IRelationshipService`:

```csharp
public class RelationshipServiceImpl : IRelationshipService
{
    public RelationshipServiceImpl(
        IRelationshipStore store,
        IIdGenerator idGenerator)
    {
        // ...
    }

    Task<RelationshipEdgeDto> UpsertAsync(RelationshipEdgeDto edge, CancellationToken ct = default);
    Task<bool> DeleteAsync(string tenantId, string edgeId, CancellationToken ct = default);
    Task<IReadOnlyList<RelationshipEdgeDto>> QueryAsync(RelationshipQuery query, CancellationToken ct = default);
    Task<RelationshipDecision> CanSeeAsync(string tenantId, EntityRefDto viewer, ActivityDto activity, CancellationToken ct = default);
}
```

**Features:**
- Validates edges before storage
- Normalizes entity references
- Generates IDs if not provided
- Deterministic visibility evaluation
- Enforces uniqueness per From+To+Kind+Scope
- Supports relationship expiration

### RelationshipEdgeValidator

Validates relationship edges:

```csharp
public class RelationshipEdgeValidator
{
    public IReadOnlyList<RelationshipValidationError> Validate(RelationshipEdgeDto edge);
}
```

**Validation Rules:**
- `TenantId` - Required, non-empty
- `From` - Required with valid Kind, Type, Id
- `To` - Required with valid Kind, Type, Id
- `Kind` - Must be valid enum value
- `Scope` - Must be valid enum value
- `Filter.TypeKeyPrefixes` - Each max 200 characters
- `Filter.Tags` - Max 50 items, each max 100 characters
- `ExpiresAt` - If set, must be in future

### RelationshipEdgeNormalizer

Normalizes entity references for consistent comparison:

```csharp
public static class RelationshipEdgeNormalizer
{
    public static RelationshipEdgeDto Normalize(RelationshipEdgeDto edge);
    public static EntityRefDto Normalize(EntityRefDto entityRef);
}
```

**Normalization:**
- Trims whitespace from strings
- Case-insensitive comparison for Kind, Type, Id
- Ensures consistent entity equality

### EntityRefEqualityHelper

Provides entity reference equality:

```csharp
public static class EntityRefEqualityHelper
{
    public static bool Equals(EntityRefDto? a, EntityRefDto? b);
    public static bool Matches(EntityRefDto entity, EntityRefDto? actor, 
        EntityRefDto? owner, List<EntityRefDto> targets, RelationshipScope scope);
}
```

### FilterMatcher

Evaluates filter criteria against activities:

```csharp
public static class FilterMatcher
{
    public static bool Matches(RelationshipFilterDto? filter, ActivityDto activity);
}
```

**Matching Logic:**
- TypeKeyPrefixes - Activity.TypeKey starts with any prefix
- Tags - Activity contains all filter tags
- Visibility - Activity visibility matches filter

## Usage Example

### Basic Setup

```csharp
using RelationshipService.Abstractions;
using RelationshipService.Core;
using RelationshipService.Store.InMemory;
using ActivityStream.Core;

// Setup dependencies
var store = new InMemoryRelationshipStore();
var idGenerator = new UlidIdGenerator();

// Create service
var service = new RelationshipServiceImpl(store, idGenerator);
```

### Managing Relationships

```csharp
// User A follows User B
var edge = await service.UpsertAsync(new RelationshipEdgeDto
{
    TenantId = "acme",
    From = new EntityRefDto { Kind = "user", Type = "User", Id = "u_a", Display = "Alice" },
    To = new EntityRefDto { Kind = "user", Type = "User", Id = "u_b", Display = "Bob" },
    Kind = RelationshipKind.Follow,
    Scope = RelationshipScope.ActorOnly
});

Console.WriteLine($"Created relationship: {edge.Id}");

// User A blocks User C
await service.UpsertAsync(new RelationshipEdgeDto
{
    TenantId = "acme",
    From = new EntityRefDto { Kind = "user", Type = "User", Id = "u_a" },
    To = new EntityRefDto { Kind = "user", Type = "User", Id = "u_c" },
    Kind = RelationshipKind.Block,
    Scope = RelationshipScope.Any
});

// User A mutes project notifications for 24 hours
await service.UpsertAsync(new RelationshipEdgeDto
{
    TenantId = "acme",
    From = new EntityRefDto { Kind = "user", Type = "User", Id = "u_a" },
    To = new EntityRefDto { Kind = "object", Type = "Project", Id = "proj_1" },
    Kind = RelationshipKind.Mute,
    Scope = RelationshipScope.Any,
    ExpiresAt = DateTimeOffset.UtcNow.AddHours(24)
});
```

### Visibility Evaluation

```csharp
// Check if viewer can see activity
var activity = new ActivityDto
{
    TenantId = "acme",
    TypeKey = "status.posted",
    OccurredAt = DateTimeOffset.UtcNow,
    Actor = new EntityRefDto { Kind = "user", Type = "User", Id = "u_b" },
    Payload = new { message = "Hello world" },
    Visibility = ActivityVisibility.Public
};

var viewer = new EntityRefDto { Kind = "user", Type = "User", Id = "u_a" };
var decision = await service.CanSeeAsync("acme", viewer, activity);

Console.WriteLine($"Can see: {decision.Allowed}");
Console.WriteLine($"Reason: {decision.Reason}");
Console.WriteLine($"Kind: {decision.Kind}");

if (decision.Allowed)
{
    if (decision.Kind == RelationshipDecisionKind.Mute)
    {
        Console.WriteLine("Activity is muted (hidden but not blocked)");
    }
    else
    {
        Console.WriteLine("Show activity normally");
    }
}
```

### Building Personalized Feeds

```csharp
public async Task<List<ActivityDto>> BuildFeedAsync(
    string tenantId,
    EntityRefDto viewer,
    IActivityStreamService activityService,
    IRelationshipService relationshipService)
{
    // Get all recent activities
    var allActivities = await activityService.QueryAsync(new ActivityQuery
    {
        TenantId = tenantId,
        Limit = 100
    });

    // Filter by visibility rules
    var visibleActivities = new List<ActivityDto>();
    foreach (var activity in allActivities.Items)
    {
        var decision = await relationshipService.CanSeeAsync(tenantId, viewer, activity);
        
        // Include if allowed and not muted
        if (decision.Allowed && decision.Kind != RelationshipDecisionKind.Mute)
        {
            visibleActivities.Add(activity);
        }
    }

    return visibleActivities;
}
```

### Querying Relationships

```csharp
// Get all users that Alice follows
var following = await service.QueryAsync(new RelationshipQuery
{
    TenantId = "acme",
    From = new EntityRefDto { Kind = "user", Type = "User", Id = "u_a" },
    Kind = RelationshipKind.Follow
});

Console.WriteLine($"Alice follows {following.Count} users:");
foreach (var edge in following)
{
    Console.WriteLine($"  - {edge.To.Display} ({edge.Scope})");
}

// Get Bob's followers
var followers = await service.QueryAsync(new RelationshipQuery
{
    TenantId = "acme",
    To = new EntityRefDto { Kind = "user", Type = "User", Id = "u_b" },
    Kind = RelationshipKind.Follow
});

Console.WriteLine($"Bob has {followers.Count} followers");

// Get all blocks
var blocks = await service.QueryAsync(new RelationshipQuery
{
    TenantId = "acme",
    From = new EntityRefDto { Kind = "user", Type = "User", Id = "u_a" },
    Kind = RelationshipKind.Block
});

Console.WriteLine($"Alice has blocked {blocks.Count} users");
```

### Filtered Relationships

```csharp
// Follow only build-related activities
await service.UpsertAsync(new RelationshipEdgeDto
{
    TenantId = "acme",
    From = new EntityRefDto { Kind = "user", Type = "User", Id = "u_a" },
    To = new EntityRefDto { Kind = "object", Type = "Project", Id = "proj_1" },
    Kind = RelationshipKind.Follow,
    Scope = RelationshipScope.Any,
    Filter = new RelationshipFilterDto
    {
        TypeKeyPrefixes = new List<string> { "build" }
    }
});

// Follow only urgent activities
await service.UpsertAsync(new RelationshipEdgeDto
{
    TenantId = "acme",
    From = new EntityRefDto { Kind = "user", Type = "User", Id = "u_a" },
    To = new EntityRefDto { Kind = "object", Type = "Team", Id = "team_1" },
    Kind = RelationshipKind.Follow,
    Scope = RelationshipScope.Any,
    Filter = new RelationshipFilterDto
    {
        Tags = new List<string> { "urgent" }
    }
});

// Subscribe only to public announcements
await service.UpsertAsync(new RelationshipEdgeDto
{
    TenantId = "acme",
    From = new EntityRefDto { Kind = "user", Type = "User", Id = "u_a" },
    To = new EntityRefDto { Kind = "object", Type = "Channel", Id = "announcements" },
    Kind = RelationshipKind.Subscribe,
    Scope = RelationshipScope.Any,
    Filter = new RelationshipFilterDto
    {
        Visibility = ActivityVisibility.Public
    }
});
```

### Deleting Relationships

```csharp
// Unfollow
var edges = await service.QueryAsync(new RelationshipQuery
{
    TenantId = "acme",
    From = new EntityRefDto { Kind = "user", Type = "User", Id = "u_a" },
    To = new EntityRefDto { Kind = "user", Type = "User", Id = "u_b" },
    Kind = RelationshipKind.Follow
});

foreach (var edge in edges)
{
    var deleted = await service.DeleteAsync("acme", edge.Id!);
    Console.WriteLine($"Deleted: {deleted}");
}
```

## Visibility Decision Algorithm

### Decision Priority (Highest to Lowest)

1. **SelfAuthored** - User always sees their own activities
   ```csharp
   if (viewer equals activity.Actor)
       return Allow(SelfAuthored)
   ```

2. **Block** - Hard block denies all visibility
   ```csharp
   if (Block relationship exists matching activity)
       return Deny(Block)
   ```

3. **Deny** - Explicit deny rule
   ```csharp
   if (Deny relationship exists matching activity)
       return Deny(Deny)
   ```

4. **Visibility** - Activity visibility rules
   ```csharp
   if (activity.Visibility == Private)
       if (viewer not in {Actor, Owner, Targets})
           return Deny(Visibility)
   
   if (activity.Visibility == Internal)
       // Allow within tenant (already filtered)
   ```

5. **Mute** - Soft hide (Allowed = true, but Kind = Mute)
   ```csharp
   if (Mute relationship exists matching activity)
       return Allow(Mute)  // Hidden but not blocked
   ```

6. **Allow** - Explicit permission grant
   ```csharp
   if (Allow relationship exists matching activity)
       return Allow(Allow)
   ```

7. **Default** - No specific rules, allow
   ```csharp
   return Allow(Default)
   ```

### Relationship Matching

A relationship matches an activity if:
1. **Scope matches**: Entity appears in the correct role(s)
2. **Filter matches** (if filter present):
   - TypeKeyPrefixes: Activity TypeKey starts with any prefix
   - Tags: Activity contains all filter tags
   - Visibility: Activity visibility matches filter
3. **Not expired**: ExpiresAt is null or in future

### Example Scenarios

```csharp
// Scenario 1: Self-authored
Viewer: u_a
Activity.Actor: u_a
Decision: Allow(SelfAuthored) - Always allowed

// Scenario 2: Block
Viewer: u_a
Activity.Actor: u_b
Relationship: u_a -(Block)-> u_b
Decision: Deny(Block) - Blocked

// Scenario 3: Private activity
Viewer: u_a
Activity.Actor: u_b
Activity.Visibility: Private
No relationships
Decision: Deny(Visibility) - Not actor/owner/target

// Scenario 4: Mute
Viewer: u_a
Activity.Actor: u_b
Relationship: u_a -(Mute)-> u_b
Decision: Allow(Mute) - Can see but marked as muted

// Scenario 5: Follow
Viewer: u_a
Activity.Actor: u_b
Activity.Visibility: Public
Relationship: u_a -(Follow)-> u_b
Decision: Allow(Default) - No specific rule, default allow
```

## Error Handling

### Validation Errors

```csharp
try
{
    await service.UpsertAsync(edge);
}
catch (RelationshipValidationException ex)
{
    Console.WriteLine("Validation failed:");
    foreach (var error in ex.Errors)
    {
        Console.WriteLine($"  {error.Field}: {error.Message}");
    }
}
```

### Common Validation Issues

- Empty TenantId
- Invalid From/To entity references
- Invalid Kind or Scope enum values
- TypeKey prefix too long (>200 chars)
- Too many tags (>50)
- ExpiresAt in the past

## Performance Considerations

### Visibility Evaluation

- Queries all relationships for viewer
- Evaluates each relationship against activity
- O(R) where R = number of viewer's relationships
- Cache relationship edges for frequent viewers

### Optimization Tips

```csharp
// Cache viewer's relationships
private readonly Dictionary<string, List<RelationshipEdgeDto>> _cache = new();

public async Task<RelationshipDecision> CanSeeOptimizedAsync(
    string tenantId, EntityRefDto viewer, ActivityDto activity)
{
    var cacheKey = $"{tenantId}:{viewer.Kind}:{viewer.Type}:{viewer.Id}";
    
    if (!_cache.TryGetValue(cacheKey, out var edges))
    {
        edges = (await service.QueryAsync(new RelationshipQuery
        {
            TenantId = tenantId,
            From = viewer
        })).ToList();
        
        _cache[cacheKey] = edges;
    }
    
    // Use cached edges for decision
    return await service.CanSeeAsync(tenantId, viewer, activity);
}
```

### Indexing Recommendations

For optimal query performance:
- Index on `(TenantId, From.Id, Kind, Scope)`
- Index on `(TenantId, To.Id, Kind, Scope)`
- Index on `(TenantId, ExpiresAt)` for cleanup
- Consider unique constraint on `(TenantId, From, To, Kind, Scope)`

## Testing

```csharp
public class RelationshipServiceTests
{
    private readonly IRelationshipService _service;

    public RelationshipServiceTests()
    {
        var store = new InMemoryRelationshipStore();
        _service = new RelationshipServiceImpl(store, new UlidIdGenerator());
    }

    [Fact]
    public async Task Block_PreventsVisibility()
    {
        // Arrange
        var viewer = new EntityRefDto { Kind = "user", Type = "User", Id = "u_a" };
        var blocked = new EntityRefDto { Kind = "user", Type = "User", Id = "u_b" };
        
        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "test",
            From = viewer,
            To = blocked,
            Kind = RelationshipKind.Block,
            Scope = RelationshipScope.Any
        });

        var activity = new ActivityDto
        {
            TenantId = "test",
            TypeKey = "test.action",
            OccurredAt = DateTimeOffset.UtcNow,
            Actor = blocked,
            Payload = new { }
        };

        // Act
        var decision = await _service.CanSeeAsync("test", viewer, activity);

        // Assert
        Assert.False(decision.Allowed);
        Assert.Equal(RelationshipDecisionKind.Block, decision.Kind);
    }
}
```

## See Also

- [RelationshipService.Abstractions](../RelationshipService.Abstractions/README.md) - Core DTOs and interfaces
- [RelationshipService.Store.InMemory](../RelationshipService.Store.InMemory/README.md) - Reference storage
- [ActivityStream.Core](../ActivityStream.Core/README.md) - Activity stream service
- [Main README](../../README.md) - All libraries overview
