using ActivityStream.Abstractions;
using ActivityStream.Core;
using RelationshipService.Abstractions;
using RelationshipService.Core;
using RelationshipService.Store.InMemory;

namespace RelationshipService.Tests;

/// <summary>
/// Tests for mutual relationship methods (friendship support).
/// </summary>
public class MutualRelationshipTests
{
    private readonly RelationshipServiceImpl _service;
    private readonly InMemoryRelationshipStore _store;

    public MutualRelationshipTests()
    {
        _store = new InMemoryRelationshipStore();
        _service = new RelationshipServiceImpl(_store, new UlidIdGenerator());
    }

    private static EntityRefDto CreateUser(string id, string? name = null) => new()
    {
        Kind = "user",
        Type = "Profile",
        Id = id,
        Display = name ?? $"User {id}"
    };

    // ─────────────────────────────────────────────────────────────────
    // GetEdgeAsync Tests
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetEdge_ReturnsEdge_WhenExists()
    {
        var user1 = CreateUser("u1");
        var user2 = CreateUser("u2");

        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "tenant1",
            From = user1,
            To = user2,
            Kind = RelationshipKind.Follow
        });

        var edge = await _service.GetEdgeAsync("tenant1", user1, user2, RelationshipKind.Follow);

        Assert.NotNull(edge);
        Assert.Equal(RelationshipKind.Follow, edge!.Kind);
    }

    [Fact]
    public async Task GetEdge_ReturnsNull_WhenNotExists()
    {
        var user1 = CreateUser("u1");
        var user2 = CreateUser("u2");

        var edge = await _service.GetEdgeAsync("tenant1", user1, user2, RelationshipKind.Follow);

        Assert.Null(edge);
    }

    [Fact]
    public async Task GetEdge_ReturnsNull_ForWrongKind()
    {
        var user1 = CreateUser("u1");
        var user2 = CreateUser("u2");

        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "tenant1",
            From = user1,
            To = user2,
            Kind = RelationshipKind.Follow
        });

        var edge = await _service.GetEdgeAsync("tenant1", user1, user2, RelationshipKind.Block);

        Assert.Null(edge);
    }

    // ─────────────────────────────────────────────────────────────────
    // AreMutualAsync Tests
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AreMutual_ReturnsTrue_WhenBothDirectionsExist()
    {
        var user1 = CreateUser("u1");
        var user2 = CreateUser("u2");

        // Create mutual follows
        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "tenant1",
            From = user1,
            To = user2,
            Kind = RelationshipKind.Follow
        });
        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "tenant1",
            From = user2,
            To = user1,
            Kind = RelationshipKind.Follow
        });

        var areMutual = await _service.AreMutualAsync("tenant1", user1, user2, RelationshipKind.Follow);

        Assert.True(areMutual);
    }

    [Fact]
    public async Task AreMutual_ReturnsFalse_WhenOnlyOneDirection()
    {
        var user1 = CreateUser("u1");
        var user2 = CreateUser("u2");

        // Only u1 follows u2
        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "tenant1",
            From = user1,
            To = user2,
            Kind = RelationshipKind.Follow
        });

        var areMutual = await _service.AreMutualAsync("tenant1", user1, user2, RelationshipKind.Follow);

        Assert.False(areMutual);
    }

    [Fact]
    public async Task AreMutual_ReturnsFalse_WhenNoRelationship()
    {
        var user1 = CreateUser("u1");
        var user2 = CreateUser("u2");

        var areMutual = await _service.AreMutualAsync("tenant1", user1, user2, RelationshipKind.Follow);

        Assert.False(areMutual);
    }

    [Fact]
    public async Task AreMutual_ReturnsFalse_WhenOneEdgeIsInactive()
    {
        var user1 = CreateUser("u1");
        var user2 = CreateUser("u2");

        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "tenant1",
            From = user1,
            To = user2,
            Kind = RelationshipKind.Follow,
            IsActive = true
        });
        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "tenant1",
            From = user2,
            To = user1,
            Kind = RelationshipKind.Follow,
            IsActive = false // Inactive
        });

        var areMutual = await _service.AreMutualAsync("tenant1", user1, user2, RelationshipKind.Follow);

        Assert.False(areMutual);
    }

    // ─────────────────────────────────────────────────────────────────
    // GetMutualRelationshipsAsync Tests (Mutual Friends)
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMutualRelationships_ReturnsCommonFollows()
    {
        var user1 = CreateUser("u1", "Alice");
        var user2 = CreateUser("u2", "Bob");
        var common1 = CreateUser("c1", "Common1");
        var common2 = CreateUser("c2", "Common2");
        var onlyUser1 = CreateUser("o1", "OnlyUser1");
        var onlyUser2 = CreateUser("o2", "OnlyUser2");

        // User1 follows: common1, common2, onlyUser1
        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "tenant1",
            From = user1,
            To = common1,
            Kind = RelationshipKind.Follow
        });
        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "tenant1",
            From = user1,
            To = common2,
            Kind = RelationshipKind.Follow
        });
        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "tenant1",
            From = user1,
            To = onlyUser1,
            Kind = RelationshipKind.Follow
        });

        // User2 follows: common1, common2, onlyUser2
        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "tenant1",
            From = user2,
            To = common1,
            Kind = RelationshipKind.Follow
        });
        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "tenant1",
            From = user2,
            To = common2,
            Kind = RelationshipKind.Follow
        });
        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "tenant1",
            From = user2,
            To = onlyUser2,
            Kind = RelationshipKind.Follow
        });

        var mutuals = await _service.GetMutualRelationshipsAsync(
            "tenant1", user1, user2, RelationshipKind.Follow);

        Assert.Equal(2, mutuals.Count);
        Assert.Contains(mutuals, m => m.Id == "c1");
        Assert.Contains(mutuals, m => m.Id == "c2");
    }

    [Fact]
    public async Task GetMutualRelationships_ReturnsEmpty_WhenNoCommon()
    {
        var user1 = CreateUser("u1");
        var user2 = CreateUser("u2");
        var onlyUser1 = CreateUser("o1");
        var onlyUser2 = CreateUser("o2");

        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "tenant1",
            From = user1,
            To = onlyUser1,
            Kind = RelationshipKind.Follow
        });
        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "tenant1",
            From = user2,
            To = onlyUser2,
            Kind = RelationshipKind.Follow
        });

        var mutuals = await _service.GetMutualRelationshipsAsync(
            "tenant1", user1, user2, RelationshipKind.Follow);

        Assert.Empty(mutuals);
    }

    [Fact]
    public async Task GetMutualRelationships_RespectsLimit()
    {
        var user1 = CreateUser("u1");
        var user2 = CreateUser("u2");

        // Create 10 common follows
        for (int i = 0; i < 10; i++)
        {
            var common = CreateUser($"common{i}");
            await _service.UpsertAsync(new RelationshipEdgeDto
            {
                TenantId = "tenant1",
                From = user1,
                To = common,
                Kind = RelationshipKind.Follow
            });
            await _service.UpsertAsync(new RelationshipEdgeDto
            {
                TenantId = "tenant1",
                From = user2,
                To = common,
                Kind = RelationshipKind.Follow
            });
        }

        var mutuals = await _service.GetMutualRelationshipsAsync(
            "tenant1", user1, user2, RelationshipKind.Follow, limit: 3);

        Assert.Equal(3, mutuals.Count);
    }

    // ─────────────────────────────────────────────────────────────────
    // CountMutualRelationshipsAsync Tests
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CountMutualRelationships_ReturnsCorrectCount()
    {
        var user1 = CreateUser("u1");
        var user2 = CreateUser("u2");

        // Create 5 common follows
        for (int i = 0; i < 5; i++)
        {
            var common = CreateUser($"common{i}");
            await _service.UpsertAsync(new RelationshipEdgeDto
            {
                TenantId = "tenant1",
                From = user1,
                To = common,
                Kind = RelationshipKind.Follow
            });
            await _service.UpsertAsync(new RelationshipEdgeDto
            {
                TenantId = "tenant1",
                From = user2,
                To = common,
                Kind = RelationshipKind.Follow
            });
        }

        // User1 has 2 additional non-common follows
        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "tenant1",
            From = user1,
            To = CreateUser("only1"),
            Kind = RelationshipKind.Follow
        });
        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "tenant1",
            From = user1,
            To = CreateUser("only2"),
            Kind = RelationshipKind.Follow
        });

        var count = await _service.CountMutualRelationshipsAsync(
            "tenant1", user1, user2, RelationshipKind.Follow);

        Assert.Equal(5, count);
    }

    [Fact]
    public async Task CountMutualRelationships_ReturnsZero_WhenNoCommon()
    {
        var user1 = CreateUser("u1");
        var user2 = CreateUser("u2");

        var count = await _service.CountMutualRelationshipsAsync(
            "tenant1", user1, user2, RelationshipKind.Follow);

        Assert.Equal(0, count);
    }

    // ─────────────────────────────────────────────────────────────────
    // Tenant Isolation Tests
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AreMutual_IsolatedByTenant()
    {
        var user1 = CreateUser("u1");
        var user2 = CreateUser("u2");

        // Create mutual in tenant1
        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "tenant1",
            From = user1,
            To = user2,
            Kind = RelationshipKind.Follow
        });
        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "tenant1",
            From = user2,
            To = user1,
            Kind = RelationshipKind.Follow
        });

        // Should be mutual in tenant1
        Assert.True(await _service.AreMutualAsync("tenant1", user1, user2, RelationshipKind.Follow));

        // Should NOT be mutual in tenant2
        Assert.False(await _service.AreMutualAsync("tenant2", user1, user2, RelationshipKind.Follow));
    }

    [Fact]
    public async Task GetMutualRelationships_DifferentKinds_AreIndependent()
    {
        var user1 = CreateUser("u1");
        var user2 = CreateUser("u2");
        var common = CreateUser("common");

        // Both follow common
        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "tenant1",
            From = user1,
            To = common,
            Kind = RelationshipKind.Follow
        });
        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "tenant1",
            From = user2,
            To = common,
            Kind = RelationshipKind.Follow
        });

        // Only user1 subscribes to common
        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "tenant1",
            From = user1,
            To = common,
            Kind = RelationshipKind.Subscribe
        });

        var followMutuals = await _service.GetMutualRelationshipsAsync(
            "tenant1", user1, user2, RelationshipKind.Follow);
        var subscribeMutuals = await _service.GetMutualRelationshipsAsync(
            "tenant1", user1, user2, RelationshipKind.Subscribe);

        Assert.Single(followMutuals);
        Assert.Empty(subscribeMutuals);
    }
}
