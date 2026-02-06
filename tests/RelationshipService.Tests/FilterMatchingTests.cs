using ActivityStream.Abstractions;
using ActivityStream.Core;
using RelationshipService.Abstractions;
using RelationshipService.Core;
using RelationshipService.Store.InMemory;

namespace RelationshipService.Tests;

/// <summary>
/// Tests for filter matching per Section 7.4 of the plan.
/// </summary>
public class FilterMatchingTests
{
    private readonly RelationshipServiceImpl _service;
    private readonly InMemoryRelationshipStore _store;

    public FilterMatchingTests()
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

    private static ActivityDto CreateActivity(EntityRefDto actor, string typeKey) => new()
    {
        Id = "act_1",
        TenantId = "acme",
        TypeKey = typeKey,
        OccurredAt = DateTimeOffset.UtcNow,
        Actor = actor,
        Visibility = ActivityVisibility.Internal,
        Payload = new { }
    };

    [Fact]
    public async Task TypeKey_exact_match_triggers_edge()
    {
        var viewer = CreateUser("u_1");
        var actor = CreateUser("u_2");
        var activity = CreateActivity(actor, "invoice.paid");

        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = viewer,
            To = actor,
            Kind = RelationshipKind.Mute,
            Scope = RelationshipScope.ActorOnly,
            Filter = new RelationshipFilterDto
            {
                TypeKeys = new List<string> { "invoice.paid" }
            }
        });

        var decision = await _service.CanSeeAsync("acme", viewer, activity);

        Assert.False(decision.Allowed);
        Assert.Equal("Mute", decision.Reason);
    }

    [Fact]
    public async Task TypeKey_exact_match_is_case_insensitive()
    {
        var viewer = CreateUser("u_1");
        var actor = CreateUser("u_2");
        var activity = CreateActivity(actor, "INVOICE.PAID");

        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = viewer,
            To = actor,
            Kind = RelationshipKind.Mute,
            Scope = RelationshipScope.ActorOnly,
            Filter = new RelationshipFilterDto
            {
                TypeKeys = new List<string> { "invoice.paid" }
            }
        });

        var decision = await _service.CanSeeAsync("acme", viewer, activity);

        Assert.False(decision.Allowed);
        Assert.Equal("Mute", decision.Reason);
    }

    [Fact]
    public async Task TypeKey_no_match_does_not_trigger_edge()
    {
        var viewer = CreateUser("u_1");
        var actor = CreateUser("u_2");
        var activity = CreateActivity(actor, "build.completed");

        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = viewer,
            To = actor,
            Kind = RelationshipKind.Mute,
            Scope = RelationshipScope.ActorOnly,
            Filter = new RelationshipFilterDto
            {
                TypeKeys = new List<string> { "invoice.paid" }
            }
        });

        var decision = await _service.CanSeeAsync("acme", viewer, activity);

        Assert.True(decision.Allowed);
    }

    [Fact]
    public async Task TypeKeyPrefix_match_triggers_edge()
    {
        var viewer = CreateUser("u_1");
        var actor = CreateUser("u_2");
        var activity = CreateActivity(actor, "invoice.paid");

        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = viewer,
            To = actor,
            Kind = RelationshipKind.Mute,
            Scope = RelationshipScope.ActorOnly,
            Filter = new RelationshipFilterDto
            {
                TypeKeyPrefixes = new List<string> { "invoice." }
            }
        });

        var decision = await _service.CanSeeAsync("acme", viewer, activity);

        Assert.False(decision.Allowed);
        Assert.Equal("Mute", decision.Reason);
    }

    [Fact]
    public async Task TypeKeyPrefix_match_is_case_insensitive()
    {
        var viewer = CreateUser("u_1");
        var actor = CreateUser("u_2");
        var activity = CreateActivity(actor, "INVOICE.PAID");

        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = viewer,
            To = actor,
            Kind = RelationshipKind.Mute,
            Scope = RelationshipScope.ActorOnly,
            Filter = new RelationshipFilterDto
            {
                TypeKeyPrefixes = new List<string> { "invoice." }
            }
        });

        var decision = await _service.CanSeeAsync("acme", viewer, activity);

        Assert.False(decision.Allowed);
    }

    [Fact]
    public async Task RequiredTagsAny_match_triggers_edge()
    {
        var viewer = CreateUser("u_1");
        var actor = CreateUser("u_2");
        var activity = CreateActivity(actor, "status.posted");
        activity.Tags = new List<string> { "urgent", "finance" };

        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = viewer,
            To = actor,
            Kind = RelationshipKind.Mute,
            Scope = RelationshipScope.ActorOnly,
            Filter = new RelationshipFilterDto
            {
                RequiredTagsAny = new List<string> { "urgent" }
            }
        });

        var decision = await _service.CanSeeAsync("acme", viewer, activity);

        Assert.False(decision.Allowed);
        Assert.Equal("Mute", decision.Reason);
    }

    [Fact]
    public async Task RequiredTagsAny_no_match_does_not_trigger_edge()
    {
        var viewer = CreateUser("u_1");
        var actor = CreateUser("u_2");
        var activity = CreateActivity(actor, "status.posted");
        activity.Tags = new List<string> { "general" };

        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = viewer,
            To = actor,
            Kind = RelationshipKind.Mute,
            Scope = RelationshipScope.ActorOnly,
            Filter = new RelationshipFilterDto
            {
                RequiredTagsAny = new List<string> { "urgent" }
            }
        });

        var decision = await _service.CanSeeAsync("acme", viewer, activity);

        Assert.True(decision.Allowed);
    }

    [Fact]
    public async Task ExcludedTagsAny_blocks_match()
    {
        var viewer = CreateUser("u_1");
        var actor = CreateUser("u_2");
        var activity = CreateActivity(actor, "status.posted");
        activity.Tags = new List<string> { "spam" };

        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = viewer,
            To = actor,
            Kind = RelationshipKind.Mute,
            Scope = RelationshipScope.ActorOnly,
            Filter = new RelationshipFilterDto
            {
                ExcludedTagsAny = new List<string> { "spam" }
            }
        });

        // Edge does NOT match because activity has excluded tag
        var decision = await _service.CanSeeAsync("acme", viewer, activity);

        Assert.True(decision.Allowed); // Edge didn't match due to excluded tag
    }

    [Fact]
    public async Task ExcludedTagsAny_allows_when_no_excluded_tags_present()
    {
        var viewer = CreateUser("u_1");
        var actor = CreateUser("u_2");
        var activity = CreateActivity(actor, "status.posted");
        activity.Tags = new List<string> { "general" };

        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = viewer,
            To = actor,
            Kind = RelationshipKind.Mute,
            Scope = RelationshipScope.ActorOnly,
            Filter = new RelationshipFilterDto
            {
                ExcludedTagsAny = new List<string> { "spam" }
            }
        });

        var decision = await _service.CanSeeAsync("acme", viewer, activity);

        Assert.False(decision.Allowed); // Edge matches, activity is muted
        Assert.Equal("Mute", decision.Reason);
    }

    [Fact]
    public async Task AllowedVisibilities_filters_activity_visibility()
    {
        var viewer = CreateUser("u_1");
        var actor = CreateUser("u_2");
        var activity = CreateActivity(actor, "status.posted");
        activity.Visibility = ActivityVisibility.Public;

        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = viewer,
            To = actor,
            Kind = RelationshipKind.Mute,
            Scope = RelationshipScope.ActorOnly,
            Filter = new RelationshipFilterDto
            {
                AllowedVisibilities = new List<ActivityVisibility> { ActivityVisibility.Internal }
            }
        });

        // Filter doesn't match because visibility is Public, not Internal
        var decision = await _service.CanSeeAsync("acme", viewer, activity);

        Assert.True(decision.Allowed);
    }

    [Fact]
    public async Task AllowedVisibilities_matches_when_activity_visibility_in_list()
    {
        var viewer = CreateUser("u_1");
        var actor = CreateUser("u_2");
        var activity = CreateActivity(actor, "status.posted");
        activity.Visibility = ActivityVisibility.Public;

        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = viewer,
            To = actor,
            Kind = RelationshipKind.Mute,
            Scope = RelationshipScope.ActorOnly,
            Filter = new RelationshipFilterDto
            {
                AllowedVisibilities = new List<ActivityVisibility> 
                { 
                    ActivityVisibility.Public, 
                    ActivityVisibility.Internal 
                }
            }
        });

        var decision = await _service.CanSeeAsync("acme", viewer, activity);

        Assert.False(decision.Allowed);
        Assert.Equal("Mute", decision.Reason);
    }

    [Fact]
    public async Task Null_filter_matches_everything()
    {
        var viewer = CreateUser("u_1");
        var actor = CreateUser("u_2");
        var activity = CreateActivity(actor, "any.type");
        activity.Tags = new List<string> { "random" };

        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = viewer,
            To = actor,
            Kind = RelationshipKind.Mute,
            Scope = RelationshipScope.ActorOnly,
            Filter = null
        });

        var decision = await _service.CanSeeAsync("acme", viewer, activity);

        Assert.False(decision.Allowed);
        Assert.Equal("Mute", decision.Reason);
    }

    [Fact]
    public async Task Combined_filter_criteria_require_all_to_match()
    {
        var viewer = CreateUser("u_1");
        var actor = CreateUser("u_2");
        var activity = CreateActivity(actor, "invoice.paid");
        activity.Tags = new List<string> { "urgent" };

        await _service.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = viewer,
            To = actor,
            Kind = RelationshipKind.Mute,
            Scope = RelationshipScope.ActorOnly,
            Filter = new RelationshipFilterDto
            {
                TypeKeyPrefixes = new List<string> { "invoice." },
                RequiredTagsAny = new List<string> { "urgent" }
            }
        });

        var decision = await _service.CanSeeAsync("acme", viewer, activity);

        Assert.False(decision.Allowed);
        Assert.Equal("Mute", decision.Reason);

        // Now test with wrong tag
        activity.Tags = new List<string> { "normal" };
        decision = await _service.CanSeeAsync("acme", viewer, activity);

        Assert.True(decision.Allowed); // Filter doesn't match due to missing required tag
    }
}
