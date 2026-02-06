using ActivityStream.Abstractions;
using ActivityStream.Core;
using RelationshipService.Abstractions;
using RelationshipService.Core;
using RelationshipService.Store.InMemory;

namespace RelationshipService.Tests;

/// <summary>
/// Tests for decision priority in CanSeeAsync per Section 7.3 of the plan.
/// Priority: SelfAuthored > Block > Deny > PrivateVisibility > Mute > Allow > Default
/// </summary>
public class DecisionPriorityTests
{
    private readonly RelationshipServiceImpl _service;
    private readonly InMemoryRelationshipStore _store;

    public DecisionPriorityTests()
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

    private static ActivityDto CreateActivity(EntityRefDto actor, ActivityVisibility visibility = ActivityVisibility.Internal) => new()
    {
        Id = "act_1",
        TenantId = "acme",
        TypeKey = "status.posted",
        OccurredAt = DateTimeOffset.UtcNow,
        Actor = actor,
        Visibility = visibility,
        Payload = new { }
    };

    [Fact]
    public async Task SelfAuthored_returns_Allowed()
    {
        var viewer = CreateUser("u_1");
        var activity = CreateActivity(viewer);

        var decision = await _service.CanSeeAsync("acme", viewer, activity);

        Assert.True(decision.Allowed);
        Assert.Equal("SelfAuthored", decision.Reason);
        Assert.Equal(RelationshipDecisionKind.Allowed, decision.Kind);
    }

    [Fact]
    public async Task SelfAuthored_bypasses_Block()
    {
        var viewer = CreateUser("u_1");
        var activity = CreateActivity(viewer);

        // Block the viewer from themselves (weird but tests priority)
        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = viewer,
            To = viewer,
            Kind = RelationshipKind.Block,
            Scope = RelationshipScope.ActorOnly
        });

        var decision = await _service.CanSeeAsync("acme", viewer, activity);

        Assert.True(decision.Allowed);
        Assert.Equal("SelfAuthored", decision.Reason);
    }

    [Fact]
    public async Task Block_returns_Denied()
    {
        var viewer = CreateUser("u_1");
        var actor = CreateUser("u_2");
        var activity = CreateActivity(actor);

        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = viewer,
            To = actor,
            Kind = RelationshipKind.Block,
            Scope = RelationshipScope.ActorOnly
        });

        var decision = await _service.CanSeeAsync("acme", viewer, activity);

        Assert.False(decision.Allowed);
        Assert.Equal("Block", decision.Reason);
        Assert.Equal(RelationshipDecisionKind.Denied, decision.Kind);
    }

    [Fact]
    public async Task Block_overrides_Allow()
    {
        var viewer = CreateUser("u_1");
        var actor = CreateUser("u_2");
        var activity = CreateActivity(actor);

        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = viewer,
            To = actor,
            Kind = RelationshipKind.Block,
            Scope = RelationshipScope.ActorOnly
        });

        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = viewer,
            To = actor,
            Kind = RelationshipKind.Allow,
            Scope = RelationshipScope.ActorOnly
        });

        var decision = await _service.CanSeeAsync("acme", viewer, activity);

        Assert.False(decision.Allowed);
        Assert.Equal("Block", decision.Reason);
    }

    [Fact]
    public async Task Deny_returns_Denied()
    {
        var viewer = CreateUser("u_1");
        var actor = CreateUser("u_2");
        var activity = CreateActivity(actor);
        activity.TypeKey = "invoice.paid";

        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = viewer,
            To = actor,
            Kind = RelationshipKind.Deny,
            Scope = RelationshipScope.ActorOnly,
            Filter = new RelationshipFilterDto
            {
                TypeKeyPrefixes = new List<string> { "invoice." }
            }
        });

        var decision = await _service.CanSeeAsync("acme", viewer, activity);

        Assert.False(decision.Allowed);
        Assert.Equal("DenyRule", decision.Reason);
        Assert.Equal(RelationshipDecisionKind.Denied, decision.Kind);
    }

    [Fact]
    public async Task Deny_overrides_Allow()
    {
        var viewer = CreateUser("u_1");
        var actor = CreateUser("u_2");
        var activity = CreateActivity(actor);
        activity.TypeKey = "invoice.paid";

        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = viewer,
            To = actor,
            Kind = RelationshipKind.Deny,
            Scope = RelationshipScope.ActorOnly,
            Filter = new RelationshipFilterDto
            {
                TypeKeyPrefixes = new List<string> { "invoice." }
            }
        });

        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = viewer,
            To = actor,
            Kind = RelationshipKind.Allow,
            Scope = RelationshipScope.ActorOnly
        });

        var decision = await _service.CanSeeAsync("acme", viewer, activity);

        Assert.False(decision.Allowed);
        Assert.Equal("DenyRule", decision.Reason);
    }

    [Fact]
    public async Task Private_visibility_denied_for_non_actor_non_owner_non_target()
    {
        var viewer = CreateUser("u_1");
        var actor = CreateUser("u_2");
        var activity = CreateActivity(actor, ActivityVisibility.Private);

        var decision = await _service.CanSeeAsync("acme", viewer, activity);

        Assert.False(decision.Allowed);
        Assert.Equal("PrivateVisibility", decision.Reason);
        Assert.Equal(RelationshipDecisionKind.Denied, decision.Kind);
    }

    [Fact]
    public async Task Private_visibility_allowed_for_Owner()
    {
        var viewer = CreateUser("u_1");
        var actor = CreateUser("u_2");
        var activity = CreateActivity(actor, ActivityVisibility.Private);
        activity.Owner = viewer;

        var decision = await _service.CanSeeAsync("acme", viewer, activity);

        Assert.True(decision.Allowed);
    }

    [Fact]
    public async Task Private_visibility_allowed_for_Target()
    {
        var viewer = CreateUser("u_1");
        var actor = CreateUser("u_2");
        var activity = CreateActivity(actor, ActivityVisibility.Private);
        activity.Targets = new List<EntityRefDto> { viewer };

        var decision = await _service.CanSeeAsync("acme", viewer, activity);

        Assert.True(decision.Allowed);
    }

    [Fact]
    public async Task Mute_returns_Hidden()
    {
        var viewer = CreateUser("u_1");
        var actor = CreateUser("u_2");
        var activity = CreateActivity(actor);

        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = viewer,
            To = actor,
            Kind = RelationshipKind.Mute,
            Scope = RelationshipScope.ActorOnly
        });

        var decision = await _service.CanSeeAsync("acme", viewer, activity);

        Assert.False(decision.Allowed);
        Assert.Equal("Mute", decision.Reason);
        Assert.Equal(RelationshipDecisionKind.Hidden, decision.Kind);
    }

    [Fact]
    public async Task Allow_returns_Allowed()
    {
        var viewer = CreateUser("u_1");
        var actor = CreateUser("u_2");
        var activity = CreateActivity(actor);

        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = viewer,
            To = actor,
            Kind = RelationshipKind.Allow,
            Scope = RelationshipScope.ActorOnly
        });

        var decision = await _service.CanSeeAsync("acme", viewer, activity);

        Assert.True(decision.Allowed);
        Assert.Equal("AllowRule", decision.Reason);
        Assert.Equal(RelationshipDecisionKind.Allowed, decision.Kind);
    }

    [Fact]
    public async Task No_edges_returns_Default_Allowed()
    {
        var viewer = CreateUser("u_1");
        var actor = CreateUser("u_2");
        var activity = CreateActivity(actor);

        var decision = await _service.CanSeeAsync("acme", viewer, activity);

        Assert.True(decision.Allowed);
        Assert.Equal("Default", decision.Reason);
    }

    [Fact]
    public async Task Public_visibility_allowed_by_default()
    {
        var viewer = CreateUser("u_1");
        var actor = CreateUser("u_2");
        var activity = CreateActivity(actor, ActivityVisibility.Public);

        var decision = await _service.CanSeeAsync("acme", viewer, activity);

        Assert.True(decision.Allowed);
    }

    [Fact]
    public async Task Internal_visibility_allowed_by_default()
    {
        var viewer = CreateUser("u_1");
        var actor = CreateUser("u_2");
        var activity = CreateActivity(actor, ActivityVisibility.Internal);

        var decision = await _service.CanSeeAsync("acme", viewer, activity);

        Assert.True(decision.Allowed);
    }
}
