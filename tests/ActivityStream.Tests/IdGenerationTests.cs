using ActivityStream.Abstractions;
using ActivityStream.Core;
using ActivityStream.Store.InMemory;

namespace ActivityStream.Tests;

/// <summary>
/// Tests for Id and CreatedAt generation per Section 7.2 of the plan.
/// </summary>
public class IdGenerationTests
{
    private readonly ActivityStreamService _service;
    private readonly InMemoryActivityStore _store;

    public IdGenerationTests()
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
    public async Task If_Id_missing_Publish_assigns_Id()
    {
        var activity = CreateMinimalValid();
        Assert.Null(activity.Id);

        var result = await _service.PublishAsync(activity);

        Assert.NotNull(result.Id);
        Assert.NotEmpty(result.Id);
    }

    [Fact]
    public async Task If_Id_is_whitespace_Publish_assigns_new_Id()
    {
        var activity = CreateMinimalValid();
        activity.Id = "   ";

        var result = await _service.PublishAsync(activity);

        Assert.NotNull(result.Id);
        Assert.NotEqual("   ", result.Id);
        Assert.Equal(result.Id, result.Id.Trim());
    }

    [Fact]
    public async Task If_Id_provided_Publish_preserves_Id()
    {
        var activity = CreateMinimalValid();
        activity.Id = "custom-id-123";

        var result = await _service.PublishAsync(activity);

        Assert.Equal("custom-id-123", result.Id);
    }

    [Fact]
    public async Task If_CreatedAt_default_Publish_assigns_CreatedAt()
    {
        var activity = CreateMinimalValid();
        Assert.Equal(default, activity.CreatedAt);

        var beforePublish = DateTimeOffset.UtcNow;
        var result = await _service.PublishAsync(activity);
        var afterPublish = DateTimeOffset.UtcNow;

        Assert.NotEqual(default, result.CreatedAt);
        Assert.InRange(result.CreatedAt, beforePublish, afterPublish);
    }

    [Fact]
    public async Task If_CreatedAt_provided_Publish_preserves_it()
    {
        var activity = CreateMinimalValid();
        var specifiedTime = DateTimeOffset.UtcNow.AddDays(-5);
        activity.CreatedAt = specifiedTime;

        var result = await _service.PublishAsync(activity);

        Assert.Equal(specifiedTime, result.CreatedAt);
    }

    [Fact]
    public async Task Generated_Ids_are_unique()
    {
        var ids = new HashSet<string>();

        for (int i = 0; i < 100; i++)
        {
            var activity = CreateMinimalValid();
            var result = await _service.PublishAsync(activity);
            Assert.True(ids.Add(result.Id!), $"Duplicate Id generated: {result.Id}");
        }

        Assert.Equal(100, ids.Count);
    }

    [Fact]
    public void UlidIdGenerator_produces_26_char_ids()
    {
        var generator = new UlidIdGenerator();

        for (int i = 0; i < 100; i++)
        {
            var id = generator.NewId();
            Assert.Equal(26, id.Length);
        }
    }

    [Fact]
    public void UlidIdGenerator_ids_are_lexicographically_sortable_by_time()
    {
        var generator = new UlidIdGenerator();
        var time1 = DateTimeOffset.UtcNow;
        var time2 = time1.AddSeconds(1);
        var time3 = time1.AddSeconds(2);

        var id1 = generator.NewId(time1);
        var id2 = generator.NewId(time2);
        var id3 = generator.NewId(time3);

        Assert.True(string.Compare(id1, id2, StringComparison.Ordinal) < 0);
        Assert.True(string.Compare(id2, id3, StringComparison.Ordinal) < 0);
    }
}
