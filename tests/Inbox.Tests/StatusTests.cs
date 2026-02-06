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
/// Tests for inbox item status changes: MarkRead/Archive.
/// </summary>
public class StatusTests
{
    private readonly InboxNotificationService _service;
    private readonly InMemoryInboxStore _inboxStore;

    public StatusTests()
    {
        _inboxStore = new InMemoryInboxStore();
        var requestStore = new InMemoryFollowRequestStore();
        var relationshipStore = new InMemoryRelationshipStore();
        var relationshipService = new RelationshipServiceImpl(relationshipStore, new UlidIdGenerator());
        var policy = new TestGovernancePolicy();

        _service = new InboxNotificationService(
            _inboxStore,
            requestStore,
            relationshipService,
            policy,
            new UlidIdGenerator());
    }

    private static EntityRefDto CreateProfile(string id) => new()
    {
        Kind = "identity",
        Type = "Profile",
        Id = id
    };

    [Fact]
    public async Task New_item_has_Unread_status()
    {
        var recipient = CreateProfile("p_1");

        var item = await _service.AddAsync(new InboxItemDto
        {
            TenantId = "acme",
            Recipient = recipient,
            Kind = InboxItemKind.Notification,
            Event = new InboxEventRefDto { Kind = "activity", Id = "act_1" },
            DedupKey = "key_1"
        });

        Assert.Equal(InboxItemStatus.Unread, item.Status);
    }

    [Fact]
    public async Task MarkReadAsync_changes_status_to_Read()
    {
        var recipient = CreateProfile("p_1");

        var item = await _service.AddAsync(new InboxItemDto
        {
            TenantId = "acme",
            Recipient = recipient,
            Kind = InboxItemKind.Notification,
            Event = new InboxEventRefDto { Kind = "activity", Id = "act_1" },
            DedupKey = "key_1"
        });

        await _service.MarkReadAsync("acme", item.Id!);

        var result = await _service.QueryInboxAsync(new InboxQuery
        {
            TenantId = "acme",
            Recipients = new List<EntityRefDto> { recipient }
        });

        Assert.Single(result.Items);
        Assert.Equal(InboxItemStatus.Read, result.Items[0].Status);
    }

    [Fact]
    public async Task ArchiveAsync_changes_status_to_Archived()
    {
        var recipient = CreateProfile("p_1");

        var item = await _service.AddAsync(new InboxItemDto
        {
            TenantId = "acme",
            Recipient = recipient,
            Kind = InboxItemKind.Notification,
            Event = new InboxEventRefDto { Kind = "activity", Id = "act_1" },
            DedupKey = "key_1"
        });

        await _service.ArchiveAsync("acme", item.Id!);

        var result = await _service.QueryInboxAsync(new InboxQuery
        {
            TenantId = "acme",
            Recipients = new List<EntityRefDto> { recipient }
        });

        Assert.Single(result.Items);
        Assert.Equal(InboxItemStatus.Archived, result.Items[0].Status);
    }

    [Fact]
    public async Task Query_with_status_filter_returns_only_matching()
    {
        var recipient = CreateProfile("p_1");

        var item1 = await _service.AddAsync(new InboxItemDto
        {
            TenantId = "acme",
            Recipient = recipient,
            Kind = InboxItemKind.Notification,
            Event = new InboxEventRefDto { Kind = "activity", Id = "act_1" },
            DedupKey = "key_1"
        });
        await _service.MarkReadAsync("acme", item1.Id!);

        await _service.AddAsync(new InboxItemDto
        {
            TenantId = "acme",
            Recipient = recipient,
            Kind = InboxItemKind.Notification,
            Event = new InboxEventRefDto { Kind = "activity", Id = "act_2" },
            DedupKey = "key_2"
        });

        // Query only Unread
        var unread = await _service.QueryInboxAsync(new InboxQuery
        {
            TenantId = "acme",
            Recipients = new List<EntityRefDto> { recipient },
            Status = InboxItemStatus.Unread
        });

        Assert.Single(unread.Items);
        Assert.Equal(InboxItemStatus.Unread, unread.Items[0].Status);

        // Query only Read
        var read = await _service.QueryInboxAsync(new InboxQuery
        {
            TenantId = "acme",
            Recipients = new List<EntityRefDto> { recipient },
            Status = InboxItemStatus.Read
        });

        Assert.Single(read.Items);
        Assert.Equal(InboxItemStatus.Read, read.Items[0].Status);
    }

    [Fact]
    public async Task Query_without_status_filter_returns_all()
    {
        var recipient = CreateProfile("p_1");

        var item1 = await _service.AddAsync(new InboxItemDto
        {
            TenantId = "acme",
            Recipient = recipient,
            Kind = InboxItemKind.Notification,
            Event = new InboxEventRefDto { Kind = "activity", Id = "act_1" },
            DedupKey = "key_1"
        });
        await _service.MarkReadAsync("acme", item1.Id!);

        await _service.AddAsync(new InboxItemDto
        {
            TenantId = "acme",
            Recipient = recipient,
            Kind = InboxItemKind.Notification,
            Event = new InboxEventRefDto { Kind = "activity", Id = "act_2" },
            DedupKey = "key_2"
        });

        var result = await _service.QueryInboxAsync(new InboxQuery
        {
            TenantId = "acme",
            Recipients = new List<EntityRefDto> { recipient }
        });

        Assert.Equal(2, result.Items.Count);
    }
}
