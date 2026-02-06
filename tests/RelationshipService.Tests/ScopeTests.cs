using ActivityStream.Abstractions;
using ActivityStream.Core;
using RelationshipService.Abstractions;
using RelationshipService.Core;
using RelationshipService.Store.InMemory;

namespace RelationshipService.Tests;

/// <summary>
/// Tests for scope matching per Section 7.5 of the plan.
/// </summary>
public class ScopeTests
{
    private readonly RelationshipServiceImpl _service;
    private readonly InMemoryRelationshipStore _store;

    public ScopeTests()
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

    private static EntityRefDto CreateObject(string type, string id) => new()
    {
        Kind = "object",
        Type = type,
        Id = id
    };

    private static ActivityDto CreateActivity(EntityRefDto actor) => new()
    {
        Id = "act_1",
        TenantId = "acme",
        TypeKey = "status.posted",
        OccurredAt = DateTimeOffset.UtcNow,
        Actor = actor,
        Visibility = ActivityVisibility.Internal,
        Payload = new { }
    };

    [Fact]
    public async Task ActorOnly_triggers_only_on_actor_match()
    {
        var viewer = CreateUser("u_1");
        var actor = CreateUser("u_2");
        var target = CreateObject("Invoice", "inv_1");

        var activity = CreateActivity(actor);
        activity.Targets = new List<EntityRefDto> { target };

        // Mute on target with ActorOnly scope - should NOT trigger
        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = viewer,
            To = target,
            Kind = RelationshipKind.Mute,
            Scope = RelationshipScope.ActorOnly
        });

        var decision = await _service.CanSeeAsync("acme", viewer, activity);
        Assert.True(decision.Allowed); // Target is not the actor

        // Mute on actor with ActorOnly scope - SHOULD trigger
        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = viewer,
            To = actor,
            Kind = RelationshipKind.Mute,
            Scope = RelationshipScope.ActorOnly
        });

        decision = await _service.CanSeeAsync("acme", viewer, activity);
        Assert.False(decision.Allowed);
        Assert.Equal("Mute", decision.Reason);
    }

    [Fact]
    public async Task TargetOnly_triggers_only_when_To_matches_any_target()
    {
        var viewer = CreateUser("u_1");
        var actor = CreateUser("u_2");
        var target1 = CreateObject("Invoice", "inv_1");
        var target2 = CreateObject("Invoice", "inv_2");

        var activity = CreateActivity(actor);
        activity.Targets = new List<EntityRefDto> { target1 };

        // Mute on target2 with TargetOnly scope - should NOT trigger
        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = viewer,
            To = target2,
            Kind = RelationshipKind.Mute,
            Scope = RelationshipScope.TargetOnly
        });

        var decision = await _service.CanSeeAsync("acme", viewer, activity);
        Assert.True(decision.Allowed);

        // Mute on target1 with TargetOnly scope - SHOULD trigger
        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = viewer,
            To = target1,
            Kind = RelationshipKind.Mute,
            Scope = RelationshipScope.TargetOnly
        });

        decision = await _service.CanSeeAsync("acme", viewer, activity);
        Assert.False(decision.Allowed);
        Assert.Equal("Mute", decision.Reason);
    }

    [Fact]
    public async Task TargetOnly_does_not_trigger_on_actor()
    {
        var viewer = CreateUser("u_1");
        var actor = CreateUser("u_2");

        var activity = CreateActivity(actor);
        activity.Targets = new List<EntityRefDto> { CreateObject("Invoice", "inv_1") };

        // Mute on actor with TargetOnly scope - should NOT trigger
        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = viewer,
            To = actor,
            Kind = RelationshipKind.Mute,
            Scope = RelationshipScope.TargetOnly
        });

        var decision = await _service.CanSeeAsync("acme", viewer, activity);
        Assert.True(decision.Allowed);
    }

    [Fact]
    public async Task OwnerOnly_triggers_only_when_Owner_matches()
    {
        var viewer = CreateUser("u_1");
        var actor = CreateUser("u_2");
        var owner = CreateUser("u_3");
        var nonOwner = CreateUser("u_4");

        var activity = CreateActivity(actor);
        activity.Owner = owner;

        // Mute on nonOwner with OwnerOnly scope - should NOT trigger
        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = viewer,
            To = nonOwner,
            Kind = RelationshipKind.Mute,
            Scope = RelationshipScope.OwnerOnly
        });

        var decision = await _service.CanSeeAsync("acme", viewer, activity);
        Assert.True(decision.Allowed);

        // Mute on owner with OwnerOnly scope - SHOULD trigger
        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = viewer,
            To = owner,
            Kind = RelationshipKind.Mute,
            Scope = RelationshipScope.OwnerOnly
        });

        decision = await _service.CanSeeAsync("acme", viewer, activity);
        Assert.False(decision.Allowed);
        Assert.Equal("Mute", decision.Reason);
    }

    [Fact]
    public async Task OwnerOnly_does_not_trigger_when_activity_has_no_owner()
    {
        var viewer = CreateUser("u_1");
        var actor = CreateUser("u_2");

        var activity = CreateActivity(actor);
        activity.Owner = null;

        // Mute on actor with OwnerOnly scope - should NOT trigger (no owner)
        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = viewer,
            To = actor,
            Kind = RelationshipKind.Mute,
            Scope = RelationshipScope.OwnerOnly
        });

        var decision = await _service.CanSeeAsync("acme", viewer, activity);
        Assert.True(decision.Allowed);
    }

    [Fact]
    public async Task Any_triggers_when_To_matches_actor()
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
            Scope = RelationshipScope.Any
        });

        var decision = await _service.CanSeeAsync("acme", viewer, activity);
        Assert.False(decision.Allowed);
        Assert.Equal("Mute", decision.Reason);
    }

    [Fact]
    public async Task Any_triggers_when_To_matches_any_target()
    {
        var viewer = CreateUser("u_1");
        var actor = CreateUser("u_2");
        var target = CreateObject("Invoice", "inv_1");

        var activity = CreateActivity(actor);
        activity.Targets = new List<EntityRefDto> { target };

        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = viewer,
            To = target,
            Kind = RelationshipKind.Mute,
            Scope = RelationshipScope.Any
        });

        var decision = await _service.CanSeeAsync("acme", viewer, activity);
        Assert.False(decision.Allowed);
        Assert.Equal("Mute", decision.Reason);
    }

    [Fact]
    public async Task Any_triggers_when_To_matches_owner()
    {
        var viewer = CreateUser("u_1");
        var actor = CreateUser("u_2");
        var owner = CreateUser("u_3");

        var activity = CreateActivity(actor);
        activity.Owner = owner;

        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = viewer,
            To = owner,
            Kind = RelationshipKind.Mute,
            Scope = RelationshipScope.Any
        });

        var decision = await _service.CanSeeAsync("acme", viewer, activity);
        Assert.False(decision.Allowed);
        Assert.Equal("Mute", decision.Reason);
    }

    [Fact]
    public async Task EntityRef_matching_is_case_insensitive()
    {
        var viewer = CreateUser("u_1");
        var actor = new EntityRefDto { Kind = "USER", Type = "USER", Id = "U_2" };

        var activity = CreateActivity(actor);

        // Create edge with lowercase version
        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = viewer,
            To = new EntityRefDto { Kind = "user", Type = "user", Id = "u_2" },
            Kind = RelationshipKind.Mute,
            Scope = RelationshipScope.ActorOnly
        });

        var decision = await _service.CanSeeAsync("acme", viewer, activity);
        Assert.False(decision.Allowed);
        Assert.Equal("Mute", decision.Reason);
    }

    [Fact]
    public async Task Inactive_edge_does_not_trigger()
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
            Scope = RelationshipScope.ActorOnly,
            IsActive = false
        });

        var decision = await _service.CanSeeAsync("acme", viewer, activity);
        Assert.True(decision.Allowed);
    }
}
