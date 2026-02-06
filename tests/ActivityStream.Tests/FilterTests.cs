using ActivityStream.Abstractions;
using ActivityStream.Core;
using ActivityStream.Store.InMemory;

namespace ActivityStream.Tests;

/// <summary>
/// Tests for query filtering per Section 7.5 of the plan.
/// </summary>
public class FilterTests
{
    private readonly ActivityStreamService _service;
    private readonly InMemoryActivityStore _store;

    public FilterTests()
    {
        _store = new InMemoryActivityStore();
        _service = new ActivityStreamService(
            _store,
            new UlidIdGenerator(),
            new DefaultActivityValidator());
    }

    private static ActivityDto CreateActivity(
        string tenantId,
        string typeKey,
        EntityRefDto actor,
        DateTimeOffset occurredAt,
        List<EntityRefDto>? targets = null) => new()
        {
            TenantId = tenantId,
            TypeKey = typeKey,
            OccurredAt = occurredAt,
            Actor = actor,
            Targets = targets ?? new(),
            Payload = new { }
        };

    [Fact]
    public async Task Filter_by_TypeKey()
    {
        var baseTime = DateTimeOffset.UtcNow;
        var actor = new EntityRefDto { Kind = "user", Type = "User", Id = "u_1" };

        await _service.PublishAsync(CreateActivity("acme", "invoice.created", actor, baseTime));
        await _service.PublishAsync(CreateActivity("acme", "invoice.paid", actor, baseTime.AddMinutes(-1)));
        await _service.PublishAsync(CreateActivity("acme", "invoice.created", actor, baseTime.AddMinutes(-2)));
        await _service.PublishAsync(CreateActivity("acme", "order.placed", actor, baseTime.AddMinutes(-3)));

        var result = await _service.QueryAsync(new ActivityQuery
        {
            TenantId = "acme",
            TypeKey = "invoice.created",
            Limit = 10
        });

        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, item => Assert.Equal("invoice.created", item.TypeKey));
    }

    [Fact]
    public async Task Filter_by_Actor_exact_ref()
    {
        var baseTime = DateTimeOffset.UtcNow;
        var actor1 = new EntityRefDto { Kind = "user", Type = "User", Id = "u_1" };
        var actor2 = new EntityRefDto { Kind = "user", Type = "User", Id = "u_2" };
        var actor3 = new EntityRefDto { Kind = "service", Type = "Service", Id = "svc_1" };

        await _service.PublishAsync(CreateActivity("acme", "test", actor1, baseTime));
        await _service.PublishAsync(CreateActivity("acme", "test", actor2, baseTime.AddMinutes(-1)));
        await _service.PublishAsync(CreateActivity("acme", "test", actor1, baseTime.AddMinutes(-2)));
        await _service.PublishAsync(CreateActivity("acme", "test", actor3, baseTime.AddMinutes(-3)));

        var result = await _service.QueryAsync(new ActivityQuery
        {
            TenantId = "acme",
            Actor = new EntityRefDto { Kind = "user", Type = "User", Id = "u_1" },
            Limit = 10
        });

        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, item =>
        {
            Assert.Equal("user", item.Actor.Kind);
            Assert.Equal("User", item.Actor.Type);
            Assert.Equal("u_1", item.Actor.Id);
        });
    }

    [Fact]
    public async Task Actor_filter_is_case_sensitive()
    {
        var baseTime = DateTimeOffset.UtcNow;
        var actor1 = new EntityRefDto { Kind = "user", Type = "User", Id = "u_1" };
        var actor2 = new EntityRefDto { Kind = "User", Type = "user", Id = "u_1" };

        await _service.PublishAsync(CreateActivity("acme", "test", actor1, baseTime));
        await _service.PublishAsync(CreateActivity("acme", "test", actor2, baseTime.AddMinutes(-1)));

        var result = await _service.QueryAsync(new ActivityQuery
        {
            TenantId = "acme",
            Actor = new EntityRefDto { Kind = "user", Type = "User", Id = "u_1" },
            Limit = 10
        });

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task Filter_by_Target_exact_ref()
    {
        var baseTime = DateTimeOffset.UtcNow;
        var actor = new EntityRefDto { Kind = "user", Type = "User", Id = "u_1" };
        var target1 = new EntityRefDto { Kind = "object", Type = "Invoice", Id = "inv_1" };
        var target2 = new EntityRefDto { Kind = "object", Type = "Invoice", Id = "inv_2" };

        await _service.PublishAsync(CreateActivity("acme", "test", actor, baseTime, new List<EntityRefDto> { target1 }));
        await _service.PublishAsync(CreateActivity("acme", "test", actor, baseTime.AddMinutes(-1), new List<EntityRefDto> { target2 }));
        await _service.PublishAsync(CreateActivity("acme", "test", actor, baseTime.AddMinutes(-2), new List<EntityRefDto> { target1, target2 }));
        await _service.PublishAsync(CreateActivity("acme", "test", actor, baseTime.AddMinutes(-3), new List<EntityRefDto>()));

        var result = await _service.QueryAsync(new ActivityQuery
        {
            TenantId = "acme",
            Target = new EntityRefDto { Kind = "object", Type = "Invoice", Id = "inv_1" },
            Limit = 10
        });

        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, item =>
            Assert.Contains(item.Targets, t => t.Kind == "object" && t.Type == "Invoice" && t.Id == "inv_1"));
    }

    [Fact]
    public async Task Target_filter_is_case_sensitive()
    {
        var baseTime = DateTimeOffset.UtcNow;
        var actor = new EntityRefDto { Kind = "user", Type = "User", Id = "u_1" };
        var target1 = new EntityRefDto { Kind = "object", Type = "Invoice", Id = "inv_1" };
        var target2 = new EntityRefDto { Kind = "Object", Type = "invoice", Id = "inv_1" };

        await _service.PublishAsync(CreateActivity("acme", "test", actor, baseTime, new List<EntityRefDto> { target1 }));
        await _service.PublishAsync(CreateActivity("acme", "test", actor, baseTime.AddMinutes(-1), new List<EntityRefDto> { target2 }));

        var result = await _service.QueryAsync(new ActivityQuery
        {
            TenantId = "acme",
            Target = new EntityRefDto { Kind = "object", Type = "Invoice", Id = "inv_1" },
            Limit = 10
        });

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task Filter_by_From_inclusive()
    {
        var baseTime = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var actor = new EntityRefDto { Kind = "user", Type = "User", Id = "u_1" };

        await _service.PublishAsync(CreateActivity("acme", "test", actor, baseTime.AddHours(-2)));
        await _service.PublishAsync(CreateActivity("acme", "test", actor, baseTime.AddHours(-1)));
        await _service.PublishAsync(CreateActivity("acme", "test", actor, baseTime));
        await _service.PublishAsync(CreateActivity("acme", "test", actor, baseTime.AddHours(1)));
        await _service.PublishAsync(CreateActivity("acme", "test", actor, baseTime.AddHours(2)));

        var result = await _service.QueryAsync(new ActivityQuery
        {
            TenantId = "acme",
            From = baseTime,
            Limit = 10
        });

        Assert.Equal(3, result.Items.Count);
        Assert.All(result.Items, item => Assert.True(item.OccurredAt >= baseTime));
    }

    [Fact]
    public async Task Filter_by_To_exclusive()
    {
        var baseTime = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var actor = new EntityRefDto { Kind = "user", Type = "User", Id = "u_1" };

        await _service.PublishAsync(CreateActivity("acme", "test", actor, baseTime.AddHours(-2)));
        await _service.PublishAsync(CreateActivity("acme", "test", actor, baseTime.AddHours(-1)));
        await _service.PublishAsync(CreateActivity("acme", "test", actor, baseTime));
        await _service.PublishAsync(CreateActivity("acme", "test", actor, baseTime.AddHours(1)));
        await _service.PublishAsync(CreateActivity("acme", "test", actor, baseTime.AddHours(2)));

        var result = await _service.QueryAsync(new ActivityQuery
        {
            TenantId = "acme",
            To = baseTime,
            Limit = 10
        });

        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, item => Assert.True(item.OccurredAt < baseTime));
    }

    [Fact]
    public async Task Filter_by_From_and_To_range()
    {
        var baseTime = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var actor = new EntityRefDto { Kind = "user", Type = "User", Id = "u_1" };

        await _service.PublishAsync(CreateActivity("acme", "test", actor, baseTime.AddHours(-2)));
        await _service.PublishAsync(CreateActivity("acme", "test", actor, baseTime.AddHours(-1)));
        await _service.PublishAsync(CreateActivity("acme", "test", actor, baseTime));
        await _service.PublishAsync(CreateActivity("acme", "test", actor, baseTime.AddHours(1)));
        await _service.PublishAsync(CreateActivity("acme", "test", actor, baseTime.AddHours(2)));

        var result = await _service.QueryAsync(new ActivityQuery
        {
            TenantId = "acme",
            From = baseTime.AddHours(-1),
            To = baseTime.AddHours(1),
            Limit = 10
        });

        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, item =>
        {
            Assert.True(item.OccurredAt >= baseTime.AddHours(-1));
            Assert.True(item.OccurredAt < baseTime.AddHours(1));
        });
    }

    [Fact]
    public async Task Combined_filters()
    {
        var baseTime = DateTimeOffset.UtcNow;
        var actor1 = new EntityRefDto { Kind = "user", Type = "User", Id = "u_1" };
        var actor2 = new EntityRefDto { Kind = "user", Type = "User", Id = "u_2" };
        var target = new EntityRefDto { Kind = "object", Type = "Invoice", Id = "inv_1" };

        await _service.PublishAsync(CreateActivity("acme", "invoice.paid", actor1, baseTime, new List<EntityRefDto> { target }));
        await _service.PublishAsync(CreateActivity("acme", "invoice.paid", actor2, baseTime.AddMinutes(-1), new List<EntityRefDto> { target }));
        await _service.PublishAsync(CreateActivity("acme", "invoice.created", actor1, baseTime.AddMinutes(-2), new List<EntityRefDto> { target }));
        await _service.PublishAsync(CreateActivity("acme", "invoice.paid", actor1, baseTime.AddMinutes(-3)));

        var result = await _service.QueryAsync(new ActivityQuery
        {
            TenantId = "acme",
            TypeKey = "invoice.paid",
            Actor = actor1,
            Target = target,
            Limit = 10
        });

        Assert.Single(result.Items);
        Assert.Equal("invoice.paid", result.Items[0].TypeKey);
        Assert.Equal("u_1", result.Items[0].Actor.Id);
    }
}
