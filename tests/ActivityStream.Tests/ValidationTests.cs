using ActivityStream.Abstractions;
using ActivityStream.Core;
using ActivityStream.Store.InMemory;

namespace ActivityStream.Tests;

/// <summary>
/// Tests for activity validation per Section 7.1 of the plan.
/// </summary>
public class ValidationTests
{
    private readonly ActivityStreamService _service;
    private readonly InMemoryActivityStore _store;

    public ValidationTests()
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
    public async Task Minimal_valid_activity_publishes_successfully()
    {
        var activity = CreateMinimalValid();

        var result = await _service.PublishAsync(activity);

        Assert.NotNull(result);
        Assert.NotNull(result.Id);
        Assert.NotEqual(default, result.CreatedAt);
    }

    [Fact]
    public async Task Missing_TenantId_fails_validation()
    {
        var activity = new ActivityDto
        {
            TenantId = "",
            TypeKey = "test.event",
            OccurredAt = DateTimeOffset.UtcNow,
            Actor = new EntityRefDto { Kind = "user", Type = "User", Id = "u_123" },
            Payload = new { }
        };

        var ex = await Assert.ThrowsAsync<ActivityValidationException>(
            () => _service.PublishAsync(activity));

        Assert.Contains(ex.Errors, e => e.Path == "TenantId");
    }

    [Fact]
    public async Task Whitespace_only_TenantId_fails_validation()
    {
        var activity = new ActivityDto
        {
            TenantId = "   ",
            TypeKey = "test.event",
            OccurredAt = DateTimeOffset.UtcNow,
            Actor = new EntityRefDto { Kind = "user", Type = "User", Id = "u_123" },
            Payload = new { }
        };

        var ex = await Assert.ThrowsAsync<ActivityValidationException>(
            () => _service.PublishAsync(activity));

        Assert.Contains(ex.Errors, e => e.Path == "TenantId");
    }

    [Fact]
    public async Task Missing_TypeKey_fails_validation()
    {
        var activity = new ActivityDto
        {
            TenantId = "acme",
            TypeKey = "",
            OccurredAt = DateTimeOffset.UtcNow,
            Actor = new EntityRefDto { Kind = "user", Type = "User", Id = "u_123" },
            Payload = new { }
        };

        var ex = await Assert.ThrowsAsync<ActivityValidationException>(
            () => _service.PublishAsync(activity));

        Assert.Contains(ex.Errors, e => e.Path == "TypeKey");
    }

    [Fact]
    public async Task Default_OccurredAt_fails_validation()
    {
        var activity = new ActivityDto
        {
            TenantId = "acme",
            TypeKey = "test.event",
            OccurredAt = default,
            Actor = new EntityRefDto { Kind = "user", Type = "User", Id = "u_123" },
            Payload = new { }
        };

        var ex = await Assert.ThrowsAsync<ActivityValidationException>(
            () => _service.PublishAsync(activity));

        Assert.Contains(ex.Errors, e => e.Path == "OccurredAt");
    }

    [Fact]
    public async Task Actor_missing_Kind_fails_validation()
    {
        var activity = new ActivityDto
        {
            TenantId = "acme",
            TypeKey = "test.event",
            OccurredAt = DateTimeOffset.UtcNow,
            Actor = new EntityRefDto { Kind = "", Type = "User", Id = "u_123" },
            Payload = new { }
        };

        var ex = await Assert.ThrowsAsync<ActivityValidationException>(
            () => _service.PublishAsync(activity));

        Assert.Contains(ex.Errors, e => e.Path == "Actor.Kind");
    }

    [Fact]
    public async Task Actor_missing_Type_fails_validation()
    {
        var activity = new ActivityDto
        {
            TenantId = "acme",
            TypeKey = "test.event",
            OccurredAt = DateTimeOffset.UtcNow,
            Actor = new EntityRefDto { Kind = "user", Type = "", Id = "u_123" },
            Payload = new { }
        };

        var ex = await Assert.ThrowsAsync<ActivityValidationException>(
            () => _service.PublishAsync(activity));

        Assert.Contains(ex.Errors, e => e.Path == "Actor.Type");
    }

    [Fact]
    public async Task Actor_missing_Id_fails_validation()
    {
        var activity = new ActivityDto
        {
            TenantId = "acme",
            TypeKey = "test.event",
            OccurredAt = DateTimeOffset.UtcNow,
            Actor = new EntityRefDto { Kind = "user", Type = "User", Id = "" },
            Payload = new { }
        };

        var ex = await Assert.ThrowsAsync<ActivityValidationException>(
            () => _service.PublishAsync(activity));

        Assert.Contains(ex.Errors, e => e.Path == "Actor.Id");
    }

    [Fact]
    public async Task Target_missing_Kind_fails_validation()
    {
        var activity = CreateMinimalValid();
        activity.Targets.Add(new EntityRefDto { Kind = "", Type = "Invoice", Id = "inv_1" });

        var ex = await Assert.ThrowsAsync<ActivityValidationException>(
            () => _service.PublishAsync(activity));

        Assert.Contains(ex.Errors, e => e.Path == "Targets[0].Kind");
    }

    [Fact]
    public async Task Target_missing_Type_fails_validation()
    {
        var activity = CreateMinimalValid();
        activity.Targets.Add(new EntityRefDto { Kind = "object", Type = "", Id = "inv_1" });

        var ex = await Assert.ThrowsAsync<ActivityValidationException>(
            () => _service.PublishAsync(activity));

        Assert.Contains(ex.Errors, e => e.Path == "Targets[0].Type");
    }

    [Fact]
    public async Task Target_missing_Id_fails_validation()
    {
        var activity = CreateMinimalValid();
        activity.Targets.Add(new EntityRefDto { Kind = "object", Type = "Invoice", Id = "" });

        var ex = await Assert.ThrowsAsync<ActivityValidationException>(
            () => _service.PublishAsync(activity));

        Assert.Contains(ex.Errors, e => e.Path == "Targets[0].Id");
    }

    [Fact]
    public async Task Summary_exceeding_max_length_fails_validation()
    {
        var activity = CreateMinimalValid();
        activity.Summary = new string('x', DefaultActivityValidator.MaxSummaryLength + 1);

        var ex = await Assert.ThrowsAsync<ActivityValidationException>(
            () => _service.PublishAsync(activity));

        Assert.Contains(ex.Errors, e => e.Path == "Summary" && e.Code == "MAX_LENGTH");
    }

    [Fact]
    public async Task TypeKey_exceeding_max_length_fails_validation()
    {
        var activity = CreateMinimalValid();
        activity.TypeKey = new string('x', DefaultActivityValidator.MaxTypeKeyLength + 1);

        var ex = await Assert.ThrowsAsync<ActivityValidationException>(
            () => _service.PublishAsync(activity));

        Assert.Contains(ex.Errors, e => e.Path == "TypeKey" && e.Code == "MAX_LENGTH");
    }

    [Fact]
    public async Task Tags_exceeding_max_count_fails_validation()
    {
        var activity = CreateMinimalValid();
        for (int i = 0; i <= DefaultActivityValidator.MaxTagCount; i++)
        {
            activity.Tags.Add($"tag{i}");
        }

        var ex = await Assert.ThrowsAsync<ActivityValidationException>(
            () => _service.PublishAsync(activity));

        Assert.Contains(ex.Errors, e => e.Path == "Tags" && e.Code == "MAX_COUNT");
    }
}
