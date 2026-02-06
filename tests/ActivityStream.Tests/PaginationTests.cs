using ActivityStream.Abstractions;
using ActivityStream.Core;
using ActivityStream.Store.InMemory;

namespace ActivityStream.Tests;

/// <summary>
/// Tests for pagination correctness per Section 7.4 of the plan.
/// </summary>
public class PaginationTests
{
    private readonly ActivityStreamService _service;
    private readonly InMemoryActivityStore _store;

    public PaginationTests()
    {
        _store = new InMemoryActivityStore();
        _service = new ActivityStreamService(
            _store,
            new UlidIdGenerator(),
            new DefaultActivityValidator());
    }

    private static ActivityDto CreateActivity(string tenantId, DateTimeOffset occurredAt) => new()
    {
        TenantId = tenantId,
        TypeKey = "test.event",
        OccurredAt = occurredAt,
        Actor = new EntityRefDto { Kind = "user", Type = "User", Id = "u_123" },
        Payload = new { }
    };

    [Fact]
    public async Task Query_with_Limit_returns_at_most_Limit_items()
    {
        var baseTime = DateTimeOffset.UtcNow;

        // Publish 10 activities
        for (int i = 0; i < 10; i++)
        {
            await _service.PublishAsync(CreateActivity("acme", baseTime.AddMinutes(-i)));
        }

        var result = await _service.QueryAsync(new ActivityQuery
        {
            TenantId = "acme",
            Limit = 5
        });

        Assert.Equal(5, result.Items.Count);
    }

    [Fact]
    public async Task NextCursor_is_not_null_when_more_exist()
    {
        var baseTime = DateTimeOffset.UtcNow;

        // Publish 10 activities
        for (int i = 0; i < 10; i++)
        {
            await _service.PublishAsync(CreateActivity("acme", baseTime.AddMinutes(-i)));
        }

        var result = await _service.QueryAsync(new ActivityQuery
        {
            TenantId = "acme",
            Limit = 5
        });

        Assert.NotNull(result.NextCursor);
    }

    [Fact]
    public async Task NextCursor_is_null_when_no_more_items()
    {
        var baseTime = DateTimeOffset.UtcNow;

        // Publish 3 activities
        for (int i = 0; i < 3; i++)
        {
            await _service.PublishAsync(CreateActivity("acme", baseTime.AddMinutes(-i)));
        }

        var result = await _service.QueryAsync(new ActivityQuery
        {
            TenantId = "acme",
            Limit = 5
        });

        Assert.Equal(3, result.Items.Count);
        Assert.Null(result.NextCursor);
    }

    [Fact]
    public async Task Next_query_returns_next_items_with_no_overlap()
    {
        var baseTime = DateTimeOffset.UtcNow;

        // Publish 10 activities with distinct times
        for (int i = 0; i < 10; i++)
        {
            await _service.PublishAsync(CreateActivity("acme", baseTime.AddMinutes(-i)));
        }

        // First page
        var page1 = await _service.QueryAsync(new ActivityQuery
        {
            TenantId = "acme",
            Limit = 4
        });

        Assert.Equal(4, page1.Items.Count);
        Assert.NotNull(page1.NextCursor);

        // Second page
        var page2 = await _service.QueryAsync(new ActivityQuery
        {
            TenantId = "acme",
            Limit = 4,
            Cursor = page1.NextCursor
        });

        Assert.Equal(4, page2.Items.Count);
        Assert.NotNull(page2.NextCursor);

        // Third page
        var page3 = await _service.QueryAsync(new ActivityQuery
        {
            TenantId = "acme",
            Limit = 4,
            Cursor = page2.NextCursor
        });

        Assert.Equal(2, page3.Items.Count);
        Assert.Null(page3.NextCursor);

        // Verify no overlaps
        var allIds = page1.Items.Concat(page2.Items).Concat(page3.Items).Select(a => a.Id).ToList();
        Assert.Equal(10, allIds.Distinct().Count());
    }

    [Fact]
    public async Task Pagination_works_when_many_items_share_same_OccurredAt()
    {
        var sameTime = DateTimeOffset.UtcNow;

        // Publish 10 activities all at the same time
        for (int i = 0; i < 10; i++)
        {
            await _service.PublishAsync(CreateActivity("acme", sameTime));
        }

        var allIds = new List<string>();

        // Page through all
        string? cursor = null;
        int pageCount = 0;
        do
        {
            var page = await _service.QueryAsync(new ActivityQuery
            {
                TenantId = "acme",
                Limit = 3,
                Cursor = cursor
            });

            allIds.AddRange(page.Items.Select(a => a.Id!));
            cursor = page.NextCursor;
            pageCount++;

            Assert.True(pageCount <= 5, "Too many pages - potential infinite loop");
        } while (cursor != null);

        // Verify no duplicates and no gaps
        Assert.Equal(10, allIds.Count);
        Assert.Equal(10, allIds.Distinct().Count());
    }

    [Fact]
    public async Task Results_are_ordered_by_OccurredAt_desc_then_Id_desc()
    {
        var baseTime = DateTimeOffset.UtcNow;

        // Publish activities with varying times
        await _service.PublishAsync(CreateActivity("acme", baseTime.AddMinutes(-5)));
        await _service.PublishAsync(CreateActivity("acme", baseTime.AddMinutes(-1)));
        await _service.PublishAsync(CreateActivity("acme", baseTime.AddMinutes(-3)));
        await _service.PublishAsync(CreateActivity("acme", baseTime.AddMinutes(-2)));
        await _service.PublishAsync(CreateActivity("acme", baseTime.AddMinutes(-4)));

        var result = await _service.QueryAsync(new ActivityQuery
        {
            TenantId = "acme",
            Limit = 10
        });

        Assert.Equal(5, result.Items.Count);

        // Verify ordering
        for (int i = 0; i < result.Items.Count - 1; i++)
        {
            var current = result.Items[i];
            var next = result.Items[i + 1];

            Assert.True(
                current.OccurredAt > next.OccurredAt ||
                (current.OccurredAt == next.OccurredAt &&
                 string.Compare(current.Id, next.Id, StringComparison.Ordinal) > 0),
                "Items are not properly ordered");
        }
    }

    [Fact]
    public async Task Limit_is_clamped_to_max_200()
    {
        var baseTime = DateTimeOffset.UtcNow;

        // Publish 250 activities
        for (int i = 0; i < 250; i++)
        {
            await _service.PublishAsync(CreateActivity("acme", baseTime.AddSeconds(-i)));
        }

        var result = await _service.QueryAsync(new ActivityQuery
        {
            TenantId = "acme",
            Limit = 500 // Exceeds max
        });

        Assert.Equal(200, result.Items.Count);
        Assert.NotNull(result.NextCursor);
    }

    [Fact]
    public async Task Limit_below_1_is_clamped_to_1()
    {
        var baseTime = DateTimeOffset.UtcNow;

        await _service.PublishAsync(CreateActivity("acme", baseTime));

        var result = await _service.QueryAsync(new ActivityQuery
        {
            TenantId = "acme",
            Limit = 0
        });

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task Empty_tenant_returns_empty_result()
    {
        var result = await _service.QueryAsync(new ActivityQuery
        {
            TenantId = "nonexistent",
            Limit = 10
        });

        Assert.Empty(result.Items);
        Assert.Null(result.NextCursor);
    }
}
