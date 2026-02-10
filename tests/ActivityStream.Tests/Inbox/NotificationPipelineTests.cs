using ActivityStream.Abstractions;
using ActivityStream.Core;
using Inbox.Abstractions;
using Inbox.Core;
using Inbox.Store.InMemory;
using RelationshipService.Abstractions;
using RelationshipService.Core;
using RelationshipService.Store.InMemory;

namespace ActivityStream.Tests.Inbox;

/// <summary>
/// Tests for the notification pipeline: activity → inbox items for followers/subscribers.
/// </summary>
public class NotificationPipelineTests
{
    private readonly InboxNotificationService _service;
    private readonly InMemoryInboxStore _inboxStore;
    private readonly InMemoryFollowRequestStore _requestStore;
    private readonly RelationshipServiceImpl _relationshipService;
    private readonly InMemoryRelationshipStore _relationshipStore;
    private readonly TestGovernancePolicy _policy;

    public NotificationPipelineTests()
    {
        _inboxStore = new InMemoryInboxStore();
        _requestStore = new InMemoryFollowRequestStore();
        _relationshipStore = new InMemoryRelationshipStore();
        _relationshipService = new RelationshipServiceImpl(_relationshipStore, new UlidIdGenerator());
        _policy = new TestGovernancePolicy();

        _service = new InboxNotificationService(
            _inboxStore,
            _requestStore,
            _relationshipService,
            _policy,
            new UlidIdGenerator());
    }

    private static EntityRefDto CreateProfile(string id) => new()
    {
        Kind = "identity",
        Type = "Profile",
        Id = id,
        Display = id
    };

    private static EntityRefDto CreateObject(string type, string id) => new()
    {
        Kind = "object",
        Type = type,
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
        Payload = new { },
        Summary = "Activity summary"
    };

    [Fact]
    public async Task Activity_creates_inbox_items_for_followers()
    {
        var actor = CreateProfile("p_actor");
        var follower = CreateProfile("p_follower");

        // Create Follow edge: follower follows actor
        await _relationshipService.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = follower,
            To = actor,
            Kind = RelationshipKind.Follow,
            Scope = RelationshipScope.ActorOnly
        });

        // Publish activity
        var activity = CreateActivity("p_actor");
        await _service.OnActivityPublishedAsync(activity);

        // Query follower's inbox
        var result = await _service.QueryInboxAsync(new InboxQuery
        {
            TenantId = "acme",
            Recipients = new List<EntityRefDto> { follower }
        });

        Assert.Single(result.Items);
        Assert.Equal(InboxItemKind.Notification, result.Items[0].Kind);
        Assert.Equal(activity.Id, result.Items[0].Event.Id);
    }

    [Fact]
    public async Task Activity_creates_inbox_items_for_subscribers_of_targets()
    {
        var actor = CreateProfile("p_actor");
        var target = CreateObject("Invoice", "inv_123");
        var subscriber = CreateProfile("p_subscriber");

        // Create Subscribe edge: subscriber subscribes to target
        await _relationshipService.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = subscriber,
            To = target,
            Kind = RelationshipKind.Subscribe,
            Scope = RelationshipScope.TargetOnly
        });

        // Publish activity with target
        var activity = CreateActivity("p_actor");
        activity.Targets = new List<EntityRefDto> { target };
        await _service.OnActivityPublishedAsync(activity);

        // Query subscriber's inbox
        var result = await _service.QueryInboxAsync(new InboxQuery
        {
            TenantId = "acme",
            Recipients = new List<EntityRefDto> { subscriber }
        });

        Assert.Single(result.Items);
        Assert.Equal("activity", result.Items[0].Event.Kind);
    }

    [Fact]
    public async Task Activity_does_not_notify_profiles_without_relationships()
    {
        var actor = CreateProfile("p_actor");
        var unrelated = CreateProfile("p_unrelated");

        // Publish activity (no relationships exist)
        var activity = CreateActivity("p_actor");
        await _service.OnActivityPublishedAsync(activity);

        // Query unrelated's inbox - should be empty
        var result = await _service.QueryInboxAsync(new InboxQuery
        {
            TenantId = "acme",
            Recipients = new List<EntityRefDto> { unrelated }
        });

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task Activity_notifies_multiple_followers()
    {
        var actor = CreateProfile("p_actor");
        var follower1 = CreateProfile("p_f1");
        var follower2 = CreateProfile("p_f2");

        // Both follow actor
        await _relationshipService.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = follower1,
            To = actor,
            Kind = RelationshipKind.Follow
        });
        await _relationshipService.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "acme",
            From = follower2,
            To = actor,
            Kind = RelationshipKind.Follow
        });

        var activity = CreateActivity("p_actor");
        await _service.OnActivityPublishedAsync(activity);

        // Check both inboxes
        var result1 = await _service.QueryInboxAsync(new InboxQuery
        {
            TenantId = "acme",
            Recipients = new List<EntityRefDto> { follower1 }
        });
        var result2 = await _service.QueryInboxAsync(new InboxQuery
        {
            TenantId = "acme",
            Recipients = new List<EntityRefDto> { follower2 }
        });

        Assert.Single(result1.Items);
        Assert.Single(result2.Items);
    }
}
