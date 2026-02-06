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
/// Tests for pagination: multi-recipient merge + cursor correctness.
/// </summary>
public class PaginationTests
{
    private readonly InboxNotificationService _service;
    private readonly InMemoryInboxStore _inboxStore;

    public PaginationTests()
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
    public async Task Query_with_limit_returns_correct_count()
    {
        var recipient = CreateProfile("p_1");

        // Add 5 items
        for (int i = 0; i < 5; i++)
        {
            await _service.AddAsync(new InboxItemDto
            {
                TenantId = "acme",
                Recipient = recipient,
                Kind = InboxItemKind.Notification,
                Event = new InboxEventRefDto { Kind = "activity", Id = $"act_{i}" },
                DedupKey = $"key_{i}"
            });
        }

        var result = await _service.QueryInboxAsync(new InboxQuery
        {
            TenantId = "acme",
            Recipients = new List<EntityRefDto> { recipient },
            Limit = 3
        });

        Assert.Equal(3, result.Items.Count);
        Assert.NotNull(result.NextCursor);
    }

    [Fact]
    public async Task Query_cursor_paginates_correctly()
    {
        var recipient = CreateProfile("p_1");

        // Add 5 items
        for (int i = 0; i < 5; i++)
        {
            await _service.AddAsync(new InboxItemDto
            {
                TenantId = "acme",
                Recipient = recipient,
                Kind = InboxItemKind.Notification,
                Event = new InboxEventRefDto { Kind = "activity", Id = $"act_{i}" },
                DedupKey = $"key_{i}"
            });
        }

        // First page
        var page1 = await _service.QueryInboxAsync(new InboxQuery
        {
            TenantId = "acme",
            Recipients = new List<EntityRefDto> { recipient },
            Limit = 3
        });

        Assert.Equal(3, page1.Items.Count);
        Assert.NotNull(page1.NextCursor);

        // Second page
        var page2 = await _service.QueryInboxAsync(new InboxQuery
        {
            TenantId = "acme",
            Recipients = new List<EntityRefDto> { recipient },
            Limit = 3,
            Cursor = page1.NextCursor
        });

        Assert.Equal(2, page2.Items.Count);
        Assert.Null(page2.NextCursor);

        // No overlapping items
        var page1Ids = page1.Items.Select(x => x.Id).ToHashSet();
        var page2Ids = page2.Items.Select(x => x.Id).ToHashSet();
        Assert.Empty(page1Ids.Intersect(page2Ids));
    }

    [Fact]
    public async Task Query_multi_recipient_merges_results()
    {
        var recipient1 = CreateProfile("p_1");
        var recipient2 = CreateProfile("p_2");

        await _service.AddAsync(new InboxItemDto
        {
            TenantId = "acme",
            Recipient = recipient1,
            Kind = InboxItemKind.Notification,
            Event = new InboxEventRefDto { Kind = "activity", Id = "act_1" },
            DedupKey = "key_1"
        });

        await _service.AddAsync(new InboxItemDto
        {
            TenantId = "acme",
            Recipient = recipient2,
            Kind = InboxItemKind.Notification,
            Event = new InboxEventRefDto { Kind = "activity", Id = "act_2" },
            DedupKey = "key_2"
        });

        // Query both recipients
        var result = await _service.QueryInboxAsync(new InboxQuery
        {
            TenantId = "acme",
            Recipients = new List<EntityRefDto> { recipient1, recipient2 }
        });

        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task Query_sorted_by_createdAt_descending()
    {
        var recipient = CreateProfile("p_1");

        await _service.AddAsync(new InboxItemDto
        {
            TenantId = "acme",
            Recipient = recipient,
            Kind = InboxItemKind.Notification,
            Event = new InboxEventRefDto { Kind = "activity", Id = "act_1" },
            DedupKey = "key_1"
        });

        await Task.Delay(10); // Ensure different timestamps

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
        Assert.True(result.Items[0].CreatedAt >= result.Items[1].CreatedAt);
    }

    [Fact]
    public async Task Query_empty_recipients_returns_empty()
    {
        var result = await _service.QueryInboxAsync(new InboxQuery
        {
            TenantId = "acme",
            Recipients = new List<EntityRefDto>()
        });

        Assert.Empty(result.Items);
        Assert.Null(result.NextCursor);
    }
}
