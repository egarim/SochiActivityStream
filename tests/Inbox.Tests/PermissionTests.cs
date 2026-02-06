using ActivityStream.Abstractions;
using ActivityStream.Core;
using Inbox.Abstractions;
using Inbox.Core;
using Inbox.Store.InMemory;
using RelationshipService.Abstractions;
using RelationshipService.Core;
using RelationshipService.Store.InMemory;

namespace Inbox.Tests;

/// <summary>
/// Tests for permission checks: Hidden/Denied recipients are skipped.
/// </summary>
public class PermissionTests
{
    private readonly InboxNotificationService _service;
    private readonly InMemoryInboxStore _inboxStore;
    private readonly RelationshipServiceImpl _relationshipService;
    private readonly InMemoryRelationshipStore _relationshipStore;
    private readonly TestGovernancePolicy _policy;

    public PermissionTests()
    {
        _inboxStore = new InMemoryInboxStore();
        var requestStore = new InMemoryFollowRequestStore();
        _relationshipStore = new InMemoryRelationshipStore();
        _relationshipService = new RelationshipServiceImpl(_relationshipStore, new UlidIdGenerator());
        _policy = new TestGovernancePolicy();

        _service = new InboxNotificationService(
            _inboxStore,
            requestStore,
            _relationshipService,
            _policy,
            new UlidIdGenerator());
    }

    private static EntityRefDto CreateProfile(string id) => new()
    {
        Kind = "identity",
        Type = "Profile",
        Id = id
    };

    private static ActivityDto CreateActivity(string actorId) => new()
    {
        Id = $"act_{Guid.NewGuid():N}",
        TenantId = "acme",
        TypeKey = "status.posted",
        OccurredAt = DateTimeOffset.UtcNow,
        Actor = CreateProfile(actorId),
        Visibility = ActivityVisibility.Internal,
        Payload = new { }
    };

    [Fact]
    public async Task Blocked_follower_does_not_receive_notifications()
    {
        var actor = CreateProfile("p_actor");
        var follower = CreateProfile("p_follower");

        // Follower follows actor
        await _relationshipService.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = follower,
            To = actor,
            Kind = RelationshipKind.Follow
        });

        // But follower also blocks actor
        await _relationshipService.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = follower,
            To = actor,
            Kind = RelationshipKind.Block
        });

        var activity = CreateActivity("p_actor");
        await _service.OnActivityPublishedAsync(activity);

        // Follower's inbox should be empty (blocked)
        var result = await _service.QueryInboxAsync(new InboxQuery
        {
            TenantId = "acme",
            Recipients = new List<EntityRefDto> { follower }
        });

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task Muted_follower_does_not_receive_notifications()
    {
        var actor = CreateProfile("p_actor");
        var follower = CreateProfile("p_follower");

        // Follower follows actor
        await _relationshipService.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = follower,
            To = actor,
            Kind = RelationshipKind.Follow
        });

        // Follower mutes actor
        await _relationshipService.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = follower,
            To = actor,
            Kind = RelationshipKind.Mute
        });

        var activity = CreateActivity("p_actor");
        await _service.OnActivityPublishedAsync(activity);

        // Follower's inbox should be empty (muted = Hidden)
        var result = await _service.QueryInboxAsync(new InboxQuery
        {
            TenantId = "acme",
            Recipients = new List<EntityRefDto> { follower }
        });

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task Deny_rule_skips_notification()
    {
        var actor = CreateProfile("p_actor");
        var follower = CreateProfile("p_follower");

        // Follower follows actor
        await _relationshipService.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = follower,
            To = actor,
            Kind = RelationshipKind.Follow
        });

        // Follower has Deny rule for status.*
        await _relationshipService.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = follower,
            To = actor,
            Kind = RelationshipKind.Deny,
            Filter = new RelationshipFilterDto
            {
                TypeKeyPrefixes = new List<string> { "status." }
            }
        });

        var activity = CreateActivity("p_actor");
        activity.TypeKey = "status.posted";
        await _service.OnActivityPublishedAsync(activity);

        // Follower's inbox should be empty (denied)
        var result = await _service.QueryInboxAsync(new InboxQuery
        {
            TenantId = "acme",
            Recipients = new List<EntityRefDto> { follower }
        });

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task Private_activity_not_visible_to_non_participants()
    {
        var actor = CreateProfile("p_actor");
        var follower = CreateProfile("p_follower");

        // Follower follows actor
        await _relationshipService.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = follower,
            To = actor,
            Kind = RelationshipKind.Follow
        });

        // Private activity
        var activity = CreateActivity("p_actor");
        activity.Visibility = ActivityVisibility.Private;
        await _service.OnActivityPublishedAsync(activity);

        // Follower's inbox should be empty (private not visible)
        var result = await _service.QueryInboxAsync(new InboxQuery
        {
            TenantId = "acme",
            Recipients = new List<EntityRefDto> { follower }
        });

        Assert.Empty(result.Items);
    }
}
