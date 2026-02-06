using ActivityStream.Abstractions;
using ActivityStream.Core;
using ActivityStream.Store.InMemory;

namespace ActivityStream.Tests;

/// <summary>
/// Tests for idempotency behavior per Section 7.3 of the plan.
/// </summary>
public class IdempotencyTests
{
    private readonly ActivityStreamService _service;
    private readonly InMemoryActivityStore _store;

    public IdempotencyTests()
    {
        _store = new InMemoryActivityStore();
        _service = new ActivityStreamService(
            _store,
            new UlidIdGenerator(),
            new DefaultActivityValidator());
    }

    private static ActivityDto CreateMinimalValid() => new()
    {
        TenantId = "acme",
        TypeKey = "test.event",
        OccurredAt = DateTimeOffset.UtcNow,
        Actor = new EntityRefDto { Kind = "user", Type = "User", Id = "u_123" },
        Payload = new { }
    };

    [Fact]
    public async Task Same_tenant_system_and_key_returns_same_activity()
    {
        var activity1 = CreateMinimalValid();
        activity1.Source = new ActivitySourceDto
        {
            System = "erp",
            IdempotencyKey = "unique-key-1"
        };

        var result1 = await _service.PublishAsync(activity1);

        var activity2 = CreateMinimalValid();
        activity2.Source = new ActivitySourceDto
        {
            System = "erp",
            IdempotencyKey = "unique-key-1"
        };

        var result2 = await _service.PublishAsync(activity2);

        Assert.Equal(result1.Id, result2.Id);
        Assert.Equal(1, _store.GetCount("acme"));
    }

    [Fact]
    public async Task Different_tenant_does_not_dedupe()
    {
        var activity1 = CreateMinimalValid();
        activity1.TenantId = "acme";
        activity1.Source = new ActivitySourceDto
        {
            System = "erp",
            IdempotencyKey = "same-key"
        };

        var result1 = await _service.PublishAsync(activity1);

        var activity2 = CreateMinimalValid();
        activity2.TenantId = "other-tenant";
        activity2.Source = new ActivitySourceDto
        {
            System = "erp",
            IdempotencyKey = "same-key"
        };

        var result2 = await _service.PublishAsync(activity2);

        Assert.NotEqual(result1.Id, result2.Id);
        Assert.Equal(1, _store.GetCount("acme"));
        Assert.Equal(1, _store.GetCount("other-tenant"));
    }

    [Fact]
    public async Task Same_key_but_different_system_does_not_dedupe()
    {
        var activity1 = CreateMinimalValid();
        activity1.Source = new ActivitySourceDto
        {
            System = "erp",
            IdempotencyKey = "same-key"
        };

        var result1 = await _service.PublishAsync(activity1);

        var activity2 = CreateMinimalValid();
        activity2.Source = new ActivitySourceDto
        {
            System = "ci",
            IdempotencyKey = "same-key"
        };

        var result2 = await _service.PublishAsync(activity2);

        Assert.NotEqual(result1.Id, result2.Id);
        Assert.Equal(2, _store.GetCount("acme"));
    }

    [Fact]
    public async Task If_system_missing_no_dedupe()
    {
        var activity1 = CreateMinimalValid();
        activity1.Source = new ActivitySourceDto
        {
            System = null,
            IdempotencyKey = "same-key"
        };

        var result1 = await _service.PublishAsync(activity1);

        var activity2 = CreateMinimalValid();
        activity2.Source = new ActivitySourceDto
        {
            System = null,
            IdempotencyKey = "same-key"
        };

        var result2 = await _service.PublishAsync(activity2);

        Assert.NotEqual(result1.Id, result2.Id);
        Assert.Equal(2, _store.GetCount("acme"));
    }

    [Fact]
    public async Task If_key_missing_no_dedupe()
    {
        var activity1 = CreateMinimalValid();
        activity1.Source = new ActivitySourceDto
        {
            System = "erp",
            IdempotencyKey = null
        };

        var result1 = await _service.PublishAsync(activity1);

        var activity2 = CreateMinimalValid();
        activity2.Source = new ActivitySourceDto
        {
            System = "erp",
            IdempotencyKey = null
        };

        var result2 = await _service.PublishAsync(activity2);

        Assert.NotEqual(result1.Id, result2.Id);
        Assert.Equal(2, _store.GetCount("acme"));
    }

    [Fact]
    public async Task If_source_null_no_dedupe()
    {
        var activity1 = CreateMinimalValid();
        activity1.Source = null;

        var result1 = await _service.PublishAsync(activity1);

        var activity2 = CreateMinimalValid();
        activity2.Source = null;

        var result2 = await _service.PublishAsync(activity2);

        Assert.NotEqual(result1.Id, result2.Id);
        Assert.Equal(2, _store.GetCount("acme"));
    }

    [Fact]
    public async Task Whitespace_system_or_key_treated_as_missing()
    {
        var activity1 = CreateMinimalValid();
        activity1.Source = new ActivitySourceDto
        {
            System = "   ",
            IdempotencyKey = "key1"
        };

        var result1 = await _service.PublishAsync(activity1);

        var activity2 = CreateMinimalValid();
        activity2.Source = new ActivitySourceDto
        {
            System = "   ",
            IdempotencyKey = "key1"
        };

        var result2 = await _service.PublishAsync(activity2);

        // Whitespace is trimmed to empty, so no dedupe
        Assert.NotEqual(result1.Id, result2.Id);
        Assert.Equal(2, _store.GetCount("acme"));
    }
}
