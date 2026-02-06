# RelationshipService.Store.InMemory

Reference in-memory storage implementation for the RelationshipService library.

## Overview

This library provides a thread-safe, in-memory implementation of `IRelationshipStore` for testing, development, and prototyping.

**NuGet:** `RelationshipService.Store.InMemory`  
**Dependencies:** `RelationshipService.Abstractions`

## Installation

```xml
<PackageReference Include="RelationshipService.Store.InMemory" Version="1.0.0" />
```

## Key Classes

### InMemoryRelationshipStore

Thread-safe in-memory implementation of `IRelationshipStore`:

```csharp
public class InMemoryRelationshipStore : IRelationshipStore
{
    public Task<RelationshipEdgeDto> UpsertAsync(
        RelationshipEdgeDto edge, CancellationToken ct = default);
    
    public Task<bool> DeleteAsync(
        string tenantId, string edgeId, CancellationToken ct = default);
    
    public Task<IReadOnlyList<RelationshipEdgeDto>> QueryAsync(
        RelationshipQuery query, CancellationToken ct = default);
}
```

**Features:**
- Thread-safe using `ReaderWriterLockSlim`
- Enforces uniqueness per `(TenantId, From, To, Kind, Scope)`
- Supports all query operations
- Perfect for unit tests and development

**Limitations:**
- Not persistent - data lost on restart
- No data limits - will consume memory
- Linear query performance (not indexed)
- Not suitable for production

## Usage Example

### Basic Setup

```csharp
using RelationshipService.Abstractions;
using RelationshipService.Core;
using RelationshipService.Store.InMemory;
using ActivityStream.Core;

// Create in-memory store
var store = new InMemoryRelationshipStore();

// Use with service
var service = new RelationshipServiceImpl(
    store: store,
    idGenerator: new UlidIdGenerator()
);
```

### Unit Testing

```csharp
public class RelationshipServiceTests
{
    private readonly InMemoryRelationshipStore _store;
    private readonly RelationshipServiceImpl _service;

    public RelationshipServiceTests()
    {
        _store = new InMemoryRelationshipStore();
        _service = new RelationshipServiceImpl(_store, new UlidIdGenerator());
    }

    [Fact]
    public async Task UpsertAsync_CreatesRelationship()
    {
        // Arrange
        var edge = new RelationshipEdgeDto
        {
            TenantId = "test",
            From = new EntityRefDto { Kind = "user", Type = "User", Id = "u_a" },
            To = new EntityRefDto { Kind = "user", Type = "User", Id = "u_b" },
            Kind = RelationshipKind.Follow,
            Scope = RelationshipScope.ActorOnly
        };

        // Act
        var stored = await _service.UpsertAsync(edge);

        // Assert
        Assert.NotNull(stored.Id);
        var queried = await _service.QueryAsync(new RelationshipQuery
        {
            TenantId = "test",
            From = edge.From,
            To = edge.To,
            Kind = RelationshipKind.Follow
        });
        Assert.Single(queried);
    }

    [Fact]
    public async Task UpsertAsync_EnforcesUniqueness()
    {
        // Arrange
        var edge = new RelationshipEdgeDto
        {
            TenantId = "test",
            From = new EntityRefDto { Kind = "user", Type = "User", Id = "u_a" },
            To = new EntityRefDto { Kind = "user", Type = "User", Id = "u_b" },
            Kind = RelationshipKind.Follow,
            Scope = RelationshipScope.ActorOnly
        };

        // Act
        var first = await _service.UpsertAsync(edge);
        var second = await _service.UpsertAsync(edge);

        // Assert
        Assert.Equal(first.Id, second.Id);
        var all = await _service.QueryAsync(new RelationshipQuery
        {
            TenantId = "test",
            From = edge.From
        });
        Assert.Single(all);
    }

    [Fact]
    public async Task DeleteAsync_RemovesRelationship()
    {
        // Arrange
        var edge = await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "test",
            From = new EntityRefDto { Kind = "user", Type = "User", Id = "u_a" },
            To = new EntityRefDto { Kind = "user", Type = "User", Id = "u_b" },
            Kind = RelationshipKind.Follow,
            Scope = RelationshipScope.ActorOnly
        });

        // Act
        var deleted = await _service.DeleteAsync("test", edge.Id!);

        // Assert
        Assert.True(deleted);
        var queried = await _service.QueryAsync(new RelationshipQuery
        {
            TenantId = "test",
            From = edge.From
        });
        Assert.Empty(queried);
    }
}
```

### Integration Testing

```csharp
public class VisibilityIntegrationTests
{
    [Fact]
    public async Task BlockedUser_CannotSeeActivities()
    {
        // Arrange
        var relationshipStore = new InMemoryRelationshipStore();
        var relationshipService = new RelationshipServiceImpl(
            relationshipStore, new UlidIdGenerator());

        var viewer = new EntityRefDto { Kind = "user", Type = "User", Id = "u_a" };
        var blocked = new EntityRefDto { Kind = "user", Type = "User", Id = "u_b" };

        // Block user
        await relationshipService.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "test",
            From = viewer,
            To = blocked,
            Kind = RelationshipKind.Block,
            Scope = RelationshipScope.Any
        });

        // Create activity from blocked user
        var activity = new ActivityDto
        {
            TenantId = "test",
            TypeKey = "status.posted",
            OccurredAt = DateTimeOffset.UtcNow,
            Actor = blocked,
            Payload = new { message = "Hello" }
        };

        // Act
        var decision = await relationshipService.CanSeeAsync("test", viewer, activity);

        // Assert
        Assert.False(decision.Allowed);
        Assert.Equal(RelationshipDecisionKind.Block, decision.Kind);
    }

    [Fact]
    public async Task MutedUser_ActivitiesAreHidden()
    {
        // Arrange
        var relationshipStore = new InMemoryRelationshipStore();
        var relationshipService = new RelationshipServiceImpl(
            relationshipStore, new UlidIdGenerator());

        var viewer = new EntityRefDto { Kind = "user", Type = "User", Id = "u_a" };
        var muted = new EntityRefDto { Kind = "user", Type = "User", Id = "u_b" };

        // Mute user
        await relationshipService.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "test",
            From = viewer,
            To = muted,
            Kind = RelationshipKind.Mute,
            Scope = RelationshipScope.ActorOnly
        });

        // Create activity from muted user
        var activity = new ActivityDto
        {
            TenantId = "test",
            TypeKey = "status.posted",
            OccurredAt = DateTimeOffset.UtcNow,
            Actor = muted,
            Payload = new { message = "Hello" }
        };

        // Act
        var decision = await relationshipService.CanSeeAsync("test", viewer, activity);

        // Assert
        Assert.True(decision.Allowed);
        Assert.Equal(RelationshipDecisionKind.Mute, decision.Kind);
    }

    [Fact]
    public async Task FilteredFollow_OnlyMatchingActivities()
    {
        // Arrange
        var relationshipStore = new InMemoryRelationshipStore();
        var relationshipService = new RelationshipServiceImpl(
            relationshipStore, new UlidIdGenerator());

        var viewer = new EntityRefDto { Kind = "user", Type = "User", Id = "u_a" };
        var followed = new EntityRefDto { Kind = "user", Type = "User", Id = "u_b" };

        // Follow only build activities
        await relationshipService.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "test",
            From = viewer,
            To = followed,
            Kind = RelationshipKind.Follow,
            Scope = RelationshipScope.ActorOnly,
            Filter = new RelationshipFilterDto
            {
                TypeKeyPrefixes = new List<string> { "build" }
            }
        });

        // Build activity (should match)
        var buildActivity = new ActivityDto
        {
            TenantId = "test",
            TypeKey = "build.completed",
            OccurredAt = DateTimeOffset.UtcNow,
            Actor = followed,
            Payload = new { }
        };

        // Status activity (should not match filter, but still visible)
        var statusActivity = new ActivityDto
        {
            TenantId = "test",
            TypeKey = "status.posted",
            OccurredAt = DateTimeOffset.UtcNow,
            Actor = followed,
            Payload = new { }
        };

        // Act & Assert
        var buildDecision = await relationshipService.CanSeeAsync("test", viewer, buildActivity);
        var statusDecision = await relationshipService.CanSeeAsync("test", viewer, statusActivity);

        Assert.True(buildDecision.Allowed);
        Assert.True(statusDecision.Allowed);
        // Note: Filters affect relationship matching, not visibility denial
    }
}
```

### Building Complete Feed System

```csharp
public class FeedBuilder
{
    private readonly IActivityStreamService _activityService;
    private readonly IRelationshipService _relationshipService;

    public FeedBuilder(
        IActivityStreamService activityService,
        IRelationshipService relationshipService)
    {
        _activityService = activityService;
        _relationshipService = relationshipService;
    }

    public async Task<List<ActivityDto>> GetPersonalizedFeedAsync(
        string tenantId, EntityRefDto viewer, int limit = 50)
    {
        // Get all recent activities
        var allActivities = await _activityService.QueryAsync(new ActivityQuery
        {
            TenantId = tenantId,
            Limit = limit * 2  // Fetch extra to account for filtering
        });

        // Filter by visibility
        var visibleActivities = new List<ActivityDto>();
        foreach (var activity in allActivities.Items)
        {
            var decision = await _relationshipService.CanSeeAsync(
                tenantId, viewer, activity);

            if (decision.Allowed && decision.Kind != RelationshipDecisionKind.Mute)
            {
                visibleActivities.Add(activity);
                if (visibleActivities.Count >= limit)
                    break;
            }
        }

        return visibleActivities;
    }

    public async Task<List<ActivityDto>> GetFollowingFeedAsync(
        string tenantId, EntityRefDto viewer, int limit = 50)
    {
        // Get users viewer follows
        var following = await _relationshipService.QueryAsync(new RelationshipQuery
        {
            TenantId = tenantId,
            From = viewer,
            Kind = RelationshipKind.Follow
        });

        if (!following.Any())
            return new List<ActivityDto>();

        // Get activities from followed users
        var allActivities = await _activityService.QueryAsync(new ActivityQuery
        {
            TenantId = tenantId,
            Limit = limit * 2
        });

        // Filter to followed users and check visibility
        var feed = new List<ActivityDto>();
        foreach (var activity in allActivities.Items)
        {
            // Check if activity is from a followed user
            var isFollowed = following.Any(edge =>
                edge.Scope == RelationshipScope.ActorOnly &&
                EntityRefEquals(edge.To, activity.Actor));

            if (!isFollowed)
                continue;

            // Check visibility
            var decision = await _relationshipService.CanSeeAsync(
                tenantId, viewer, activity);

            if (decision.Allowed && decision.Kind != RelationshipDecisionKind.Mute)
            {
                feed.Add(activity);
                if (feed.Count >= limit)
                    break;
            }
        }

        return feed;
    }

    private bool EntityRefEquals(EntityRefDto a, EntityRefDto b)
    {
        return string.Equals(a.Kind, b.Kind, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(a.Type, b.Type, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(a.Id, b.Id, StringComparison.OrdinalIgnoreCase);
    }
}

// Usage
var activityStore = new InMemoryActivityStore();
var activityService = new ActivityStreamService(
    activityStore, new UlidIdGenerator(), new DefaultActivityValidator());

var relationshipStore = new InMemoryRelationshipStore();
var relationshipService = new RelationshipServiceImpl(
    relationshipStore, new UlidIdGenerator());

var feedBuilder = new FeedBuilder(activityService, relationshipService);

var viewer = new EntityRefDto { Kind = "user", Type = "User", Id = "u_123" };
var feed = await feedBuilder.GetPersonalizedFeedAsync("acme", viewer);
```

## Thread Safety

The store is thread-safe and can be used concurrently:

```csharp
var store = new InMemoryRelationshipStore();
var service = new RelationshipServiceImpl(store, new UlidIdGenerator());

// Safe to call from multiple threads
var tasks = Enumerable.Range(0, 100).Select(async i =>
{
    await service.UpsertAsync(new RelationshipEdgeDto
    {
        TenantId = "test",
        From = new EntityRefDto { Kind = "user", Type = "User", Id = $"u_{i}" },
        To = new EntityRefDto { Kind = "user", Type = "User", Id = $"u_{(i + 1) % 100}" },
        Kind = RelationshipKind.Follow,
        Scope = RelationshipScope.ActorOnly
    });
});

await Task.WhenAll(tasks);
```

## Query Performance

The in-memory store uses linear scans for queries:
- **UpsertAsync**: O(n) - scans to check uniqueness
- **DeleteAsync**: O(n) - scans to find edge
- **QueryAsync**: O(n) - scans all edges

For production, implement `IRelationshipStore` with a real database that supports indexing.

## When to Use

**✅ Good For:**
- Unit tests
- Integration tests
- Development
- Prototyping
- Demos
- Small datasets (<1,000 relationships)

**❌ Not For:**
- Production environments
- Large datasets
- Persistent storage requirements
- Performance-critical scenarios
- Multi-process scenarios

## Migration to Production Store

When moving to production, replace the in-memory store:

```csharp
// Development
var store = new InMemoryRelationshipStore();

// Production - implement for your database
var store = new PostgresRelationshipStore(connectionString);
// or
var store = new SqlServerRelationshipStore(connectionString);
// or
var store = new MongoRelationshipStore(connectionString);

// Service usage remains the same
var service = new RelationshipServiceImpl(store, idGenerator);
```

## See Also

- [RelationshipService.Abstractions](../RelationshipService.Abstractions/README.md) - Core DTOs and interfaces
- [RelationshipService.Core](../RelationshipService.Core/README.md) - Service implementation
- [ActivityStream.Store.InMemory](../ActivityStream.Store.InMemory/README.md) - Activity storage
- [Main README](../../README.md) - All libraries overview
