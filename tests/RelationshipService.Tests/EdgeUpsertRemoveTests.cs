using ActivityStream.Abstractions;
using ActivityStream.Core;
using RelationshipService.Abstractions;
using RelationshipService.Core;
using RelationshipService.Store.InMemory;

namespace RelationshipService.Tests;

/// <summary>
/// Tests for edge upsert/remove operations per Section 7.1 of the plan.
/// </summary>
public class EdgeUpsertRemoveTests
{
    private readonly RelationshipServiceImpl _service;
    private readonly InMemoryRelationshipStore _store;

    public EdgeUpsertRemoveTests()
    {
        _store = new InMemoryRelationshipStore();
        _service = new RelationshipServiceImpl(_store, new UlidIdGenerator());
    }

    private static EntityRefDto CreateUser(string id) => new()
    {
        Kind = "user",
        Type = "User",
        Id = id
    };

    private static RelationshipEdgeDto CreateEdge(EntityRefDto from, EntityRefDto to) => new()
    {
        TenantId = "acme",
        From = from,
        To = to,
        Kind = RelationshipKind.Follow,
        Scope = RelationshipScope.ActorOnly
    };

    [Fact]
    public async Task Upsert_new_edge_creates_it_with_Id_and_CreatedAt()
    {
        var edge = CreateEdge(CreateUser("u_1"), CreateUser("u_2"));

        var result = await _service.UpsertAsync(edge);

        Assert.NotNull(result.Id);
        Assert.NotEqual(default, result.CreatedAt);
    }

    [Fact]
    public async Task Upsert_same_uniqueness_key_updates_existing_and_keeps_same_Id()
    {
        var from = CreateUser("u_1");
        var to = CreateUser("u_2");
        var edge1 = CreateEdge(from, to);
        edge1.IsActive = true;

        var result1 = await _service.UpsertAsync(edge1);
        var originalId = result1.Id;
        var originalCreatedAt = result1.CreatedAt;

        // Create new edge with same uniqueness key but different IsActive
        var edge2 = CreateEdge(from, to);
        edge2.IsActive = false;

        var result2 = await _service.UpsertAsync(edge2);

        Assert.Equal(originalId, result2.Id);
        Assert.Equal(originalCreatedAt, result2.CreatedAt);
        Assert.False(result2.IsActive);
    }

    [Fact]
    public async Task Upsert_different_Kind_creates_new_edge()
    {
        var from = CreateUser("u_1");
        var to = CreateUser("u_2");

        var followEdge = new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = from,
            To = to,
            Kind = RelationshipKind.Follow,
            Scope = RelationshipScope.ActorOnly
        };

        var blockEdge = new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = from,
            To = to,
            Kind = RelationshipKind.Block,
            Scope = RelationshipScope.ActorOnly
        };

        var result1 = await _service.UpsertAsync(followEdge);
        var result2 = await _service.UpsertAsync(blockEdge);

        Assert.NotEqual(result1.Id, result2.Id);
    }

    [Fact]
    public async Task Upsert_different_Scope_creates_new_edge()
    {
        var from = CreateUser("u_1");
        var to = CreateUser("u_2");

        var actorEdge = new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = from,
            To = to,
            Kind = RelationshipKind.Block,
            Scope = RelationshipScope.ActorOnly
        };

        var targetEdge = new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = from,
            To = to,
            Kind = RelationshipKind.Block,
            Scope = RelationshipScope.TargetOnly
        };

        var result1 = await _service.UpsertAsync(actorEdge);
        var result2 = await _service.UpsertAsync(targetEdge);

        Assert.NotEqual(result1.Id, result2.Id);
    }

    [Fact]
    public async Task Remove_removes_edge()
    {
        var edge = CreateEdge(CreateUser("u_1"), CreateUser("u_2"));
        var result = await _service.UpsertAsync(edge);

        await _service.RemoveAsync("acme", result.Id!);

        var query = await _service.QueryAsync(new RelationshipQuery
        {
            TenantId = "acme",
            From = edge.From
        });

        Assert.Empty(query);
    }

    [Fact]
    public async Task Remove_nonexistent_edge_does_not_throw()
    {
        // Should not throw
        await _service.RemoveAsync("acme", "nonexistent_id");
    }

    [Fact]
    public async Task Query_returns_edges_by_From()
    {
        var user1 = CreateUser("u_1");
        var user2 = CreateUser("u_2");
        var user3 = CreateUser("u_3");

        await _service.UpsertAsync(CreateEdge(user1, user2));
        await _service.UpsertAsync(CreateEdge(user1, user3));
        await _service.UpsertAsync(CreateEdge(user2, user3));

        var results = await _service.QueryAsync(new RelationshipQuery
        {
            TenantId = "acme",
            From = user1
        });

        Assert.Equal(2, results.Count);
        Assert.All(results, e => Assert.Equal("u_1", e.From.Id));
    }

    [Fact]
    public async Task Query_returns_edges_by_To()
    {
        var user1 = CreateUser("u_1");
        var user2 = CreateUser("u_2");
        var user3 = CreateUser("u_3");

        await _service.UpsertAsync(CreateEdge(user1, user2));
        await _service.UpsertAsync(CreateEdge(user1, user3));
        await _service.UpsertAsync(CreateEdge(user2, user3));

        var results = await _service.QueryAsync(new RelationshipQuery
        {
            TenantId = "acme",
            To = user3
        });

        Assert.Equal(2, results.Count);
        Assert.All(results, e => Assert.Equal("u_3", e.To.Id));
    }

    [Fact]
    public async Task Query_respects_IsActive_filter()
    {
        var edge = CreateEdge(CreateUser("u_1"), CreateUser("u_2"));
        edge.IsActive = false;
        await _service.UpsertAsync(edge);

        var activeResults = await _service.QueryAsync(new RelationshipQuery
        {
            TenantId = "acme",
            IsActive = true
        });

        var inactiveResults = await _service.QueryAsync(new RelationshipQuery
        {
            TenantId = "acme",
            IsActive = false
        });

        Assert.Empty(activeResults);
        Assert.Single(inactiveResults);
    }
}
