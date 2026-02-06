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
/// Tests for grouping and deduplication behavior.
/// </summary>
public class GroupingDedupTests
{
    private readonly InboxNotificationService _service;
    private readonly InMemoryInboxStore _inboxStore;
    private readonly InMemoryFollowRequestStore _requestStore;
    private readonly RelationshipServiceImpl _relationshipService;
    private readonly TestGovernancePolicy _policy;

    public GroupingDedupTests()
    {
        _inboxStore = new InMemoryInboxStore();
        _requestStore = new InMemoryFollowRequestStore();
        var relationshipStore = new InMemoryRelationshipStore();
        _relationshipService = new RelationshipServiceImpl(relationshipStore, new UlidIdGenerator());
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
        Id = id
    };

    [Fact]
    public async Task DedupKey_prevents_duplicate_items()
    {
        var recipient = CreateProfile("p_1");

        // Add first item
        var item1 = new InboxItemDto
        {
            TenantId = "acme",
            Recipient = recipient,
            Kind = InboxItemKind.Notification,
            Event = new InboxEventRefDto { Kind = "activity", Id = "act_1" },
            DedupKey = "unique_key_123"
        };
        var result1 = await _service.AddAsync(item1);

        // Add duplicate with same dedup key
        var item2 = new InboxItemDto
        {
            TenantId = "acme",
            Recipient = recipient,
            Kind = InboxItemKind.Notification,
            Event = new InboxEventRefDto { Kind = "activity", Id = "act_2" },
            DedupKey = "unique_key_123"
        };
        var result2 = await _service.AddAsync(item2);

        // Should return the same item
        Assert.Equal(result1.Id, result2.Id);

        // Query should have only one item
        var query = await _service.QueryInboxAsync(new InboxQuery
        {
            TenantId = "acme",
            Recipients = new List<EntityRefDto> { recipient }
        });
        Assert.Single(query.Items);
    }

    [Fact]
    public async Task ThreadKey_increments_thread_count()
    {
        var recipient = CreateProfile("p_1");

        // Add first item with thread key
        var item1 = new InboxItemDto
        {
            TenantId = "acme",
            Recipient = recipient,
            Kind = InboxItemKind.Notification,
            Event = new InboxEventRefDto { Kind = "activity", Id = "act_1" },
            ThreadKey = "target:Invoice:inv_332:type:comment",
            DedupKey = "dedup_1"
        };
        await _service.AddAsync(item1);

        // Add second item with same thread key but different dedup key
        var item2 = new InboxItemDto
        {
            TenantId = "acme",
            Recipient = recipient,
            Kind = InboxItemKind.Notification,
            Event = new InboxEventRefDto { Kind = "activity", Id = "act_2" },
            ThreadKey = "target:Invoice:inv_332:type:comment",
            DedupKey = "dedup_2"
        };
        var result = await _service.AddAsync(item2);

        // Thread count should be incremented
        Assert.Equal(2, result.ThreadCount);

        // Query should still have only one item (grouped)
        var query = await _service.QueryInboxAsync(new InboxQuery
        {
            TenantId = "acme",
            Recipients = new List<EntityRefDto> { recipient }
        });
        Assert.Single(query.Items);
        Assert.Equal(2, query.Items[0].ThreadCount);
    }

    [Fact]
    public async Task Thread_update_preserves_read_status()
    {
        var recipient = CreateProfile("p_1");

        // Add and mark as read
        var item1 = new InboxItemDto
        {
            TenantId = "acme",
            Recipient = recipient,
            Kind = InboxItemKind.Notification,
            Event = new InboxEventRefDto { Kind = "activity", Id = "act_1" },
            ThreadKey = "thread_key_1",
            DedupKey = "dedup_1"
        };
        var result1 = await _service.AddAsync(item1);
        await _service.MarkReadAsync("acme", result1.Id!);

        // Add to thread
        var item2 = new InboxItemDto
        {
            TenantId = "acme",
            Recipient = recipient,
            Kind = InboxItemKind.Notification,
            Event = new InboxEventRefDto { Kind = "activity", Id = "act_2" },
            ThreadKey = "thread_key_1",
            DedupKey = "dedup_2"
        };
        await _service.AddAsync(item2);

        // Query and verify status stayed Read (per decision #2)
        var query = await _service.QueryInboxAsync(new InboxQuery
        {
            TenantId = "acme",
            Recipients = new List<EntityRefDto> { recipient }
        });
        Assert.Single(query.Items);
        Assert.Equal(InboxItemStatus.Read, query.Items[0].Status);
    }

    [Fact]
    public async Task Different_thread_keys_create_separate_items()
    {
        var recipient = CreateProfile("p_1");

        await _service.AddAsync(new InboxItemDto
        {
            TenantId = "acme",
            Recipient = recipient,
            Kind = InboxItemKind.Notification,
            Event = new InboxEventRefDto { Kind = "activity", Id = "act_1" },
            ThreadKey = "thread_A",
            DedupKey = "dedup_1"
        });

        await _service.AddAsync(new InboxItemDto
        {
            TenantId = "acme",
            Recipient = recipient,
            Kind = InboxItemKind.Notification,
            Event = new InboxEventRefDto { Kind = "activity", Id = "act_2" },
            ThreadKey = "thread_B",
            DedupKey = "dedup_2"
        });

        var query = await _service.QueryInboxAsync(new InboxQuery
        {
            TenantId = "acme",
            Recipients = new List<EntityRefDto> { recipient }
        });
        Assert.Equal(2, query.Items.Count);
    }
}
