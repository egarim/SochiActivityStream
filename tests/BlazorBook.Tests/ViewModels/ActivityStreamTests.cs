using NUnit.Framework;
using Sochi.Navigation.Navigation;
using SocialKit.Components.ViewModels;
using ActivityStream.Abstractions;

namespace BlazorBook.Tests.ViewModels;

/// <summary>
/// Tests for Activity Stream: Publishing activities, querying timelines, notifications.
/// </summary>
[TestFixture]
public class ActivityStreamTests
{
    private TestFixture _fixture = null!;

    [SetUp]
    public void SetUp()
    {
        _fixture = new TestFixture();
    }

    [TearDown]
    public void TearDown()
    {
        _fixture.Dispose();
    }

    private async Task<(string id, EntityRefDto entity)> CreateUser(string displayName, string handle, string email)
    {
        var vm = _fixture.GetViewModel<SignUpViewModel>();
        vm.DisplayName = displayName;
        vm.Handle = handle;
        vm.Email = email;
        vm.Password = "password123";
        await vm.SignUpCommand.ExecuteAsync(null);
        var id = _fixture.CurrentUser.ProfileId!;
        return (id, new EntityRefDto { Kind = "Profile", Type = "Profile", Id = id });
    }

    private IActivityStreamService GetActivityService() => 
        _fixture.GetService<IActivityStreamService>();

    // ═══════════════════════════════════════════════════════════════════════════
    // PUBLISH ACTIVITY TESTS
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task PublishActivity_AssignsId()
    {
        // Arrange
        var (aliceId, alice) = await CreateUser("Alice", "alice", "alice@test.com");
        var activityService = GetActivityService();
        
        // Act
        var activity = await activityService.PublishAsync(new ActivityDto
        {
            TenantId = "blazorbook",
            TypeKey = "post.created",
            OccurredAt = DateTimeOffset.UtcNow,
            Actor = alice,
            Payload = new { PostId = "post-1", Content = "Hello world!" }
        });
        
        // Assert
        Assert.That(activity.Id, Is.Not.Null);
        Assert.That(activity.CreatedAt, Is.Not.EqualTo(default(DateTimeOffset)));
    }

    [Test]
    public async Task PublishActivity_UserFollowed_RecordsEvent()
    {
        // Arrange
        var (aliceId, alice) = await CreateUser("Alice", "alice", "alice@test.com");
        await _fixture.CurrentUser.SignOutAsync();
        var (bobId, bob) = await CreateUser("Bob", "bob", "bob@test.com");
        
        var activityService = GetActivityService();
        
        // Act: Bob follows Alice (activity would be published by service)
        var activity = await activityService.PublishAsync(new ActivityDto
        {
            TenantId = "blazorbook",
            TypeKey = "user.followed",
            OccurredAt = DateTimeOffset.UtcNow,
            Actor = bob,
            Targets = [alice],
            Summary = $"{bob.Id} followed {alice.Id}",
            Payload = new { FollowerId = bobId, FolloweeId = aliceId }
        });
        
        // Assert
        Assert.That(activity.TypeKey, Is.EqualTo("user.followed"));
        Assert.That(activity.Targets, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task PublishActivity_PostLiked_IncludesPostInTargets()
    {
        // Arrange
        var (aliceId, alice) = await CreateUser("Alice", "alice", "alice@test.com");
        
        var postEntity = new EntityRefDto { Kind = "Post", Type = "Post", Id = "post-123" };
        var activityService = GetActivityService();
        
        // Act
        var activity = await activityService.PublishAsync(new ActivityDto
        {
            TenantId = "blazorbook",
            TypeKey = "post.liked",
            OccurredAt = DateTimeOffset.UtcNow,
            Actor = alice,
            Targets = [postEntity],
            Summary = "Alice liked a post",
            Payload = new { PostId = "post-123", UserId = aliceId }
        });
        
        // Assert
        Assert.That(activity.Targets[0].Id, Is.EqualTo("post-123"));
    }

    [Test]
    public async Task PublishBatch_PublishesMultipleActivities()
    {
        // Arrange
        var (aliceId, alice) = await CreateUser("Alice", "alice", "alice@test.com");
        var activityService = GetActivityService();
        
        var activities = new List<ActivityDto>
        {
            new()
            {
                TenantId = "blazorbook",
                TypeKey = "post.created",
                OccurredAt = DateTimeOffset.UtcNow.AddMinutes(-2),
                Actor = alice,
                Payload = new { PostId = "post-1" }
            },
            new()
            {
                TenantId = "blazorbook",
                TypeKey = "post.created",
                OccurredAt = DateTimeOffset.UtcNow.AddMinutes(-1),
                Actor = alice,
                Payload = new { PostId = "post-2" }
            },
            new()
            {
                TenantId = "blazorbook",
                TypeKey = "post.created",
                OccurredAt = DateTimeOffset.UtcNow,
                Actor = alice,
                Payload = new { PostId = "post-3" }
            }
        };
        
        // Act
        var result = await activityService.PublishBatchAsync(activities);
        
        // Assert
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result.All(a => a.Id != null), Is.True);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // QUERY ACTIVITY TESTS
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task QueryActivities_ByActor_ReturnsOnlyActorActivities()
    {
        // Arrange
        var (aliceId, alice) = await CreateUser("Alice", "alice", "alice@test.com");
        await _fixture.CurrentUser.SignOutAsync();
        var (bobId, bob) = await CreateUser("Bob", "bob", "bob@test.com");
        
        var activityService = GetActivityService();
        
        await activityService.PublishAsync(new ActivityDto
        {
            TenantId = "blazorbook",
            TypeKey = "post.created",
            OccurredAt = DateTimeOffset.UtcNow,
            Actor = alice,
            Payload = new { PostId = "alice-post" }
        });
        
        await activityService.PublishAsync(new ActivityDto
        {
            TenantId = "blazorbook",
            TypeKey = "post.created",
            OccurredAt = DateTimeOffset.UtcNow,
            Actor = bob,
            Payload = new { PostId = "bob-post" }
        });
        
        // Act: Query only Alice's activities
        var result = await activityService.QueryAsync(new ActivityQuery
        {
            TenantId = "blazorbook",
            Actor = alice
        });
        
        // Assert
        Assert.That(result.Items.All(a => a.Actor.Id == aliceId), Is.True);
    }

    [Test]
    public async Task QueryActivities_ByTypeKey_FiltersCorrectly()
    {
        // Arrange
        var (aliceId, alice) = await CreateUser("Alice", "alice", "alice@test.com");
        var activityService = GetActivityService();
        
        await activityService.PublishAsync(new ActivityDto
        {
            TenantId = "blazorbook",
            TypeKey = "post.created",
            OccurredAt = DateTimeOffset.UtcNow,
            Actor = alice,
            Payload = new { PostId = "post-1" }
        });
        
        await activityService.PublishAsync(new ActivityDto
        {
            TenantId = "blazorbook",
            TypeKey = "comment.created",
            OccurredAt = DateTimeOffset.UtcNow,
            Actor = alice,
            Payload = new { CommentId = "comment-1" }
        });
        
        // Act: Query only post.created
        var result = await activityService.QueryAsync(new ActivityQuery
        {
            TenantId = "blazorbook",
            TypeKey = "post.created"
        });
        
        // Assert
        Assert.That(result.Items.All(a => a.TypeKey == "post.created"), Is.True);
    }

    [Test]
    public async Task QueryActivities_ByTarget_ReturnsActivitiesTargetingEntity()
    {
        // Arrange
        var (aliceId, alice) = await CreateUser("Alice", "alice", "alice@test.com");
        await _fixture.CurrentUser.SignOutAsync();
        var (bobId, bob) = await CreateUser("Bob", "bob", "bob@test.com");
        
        var activityService = GetActivityService();
        
        // Bob follows Alice (Alice is the target)
        await activityService.PublishAsync(new ActivityDto
        {
            TenantId = "blazorbook",
            TypeKey = "user.followed",
            OccurredAt = DateTimeOffset.UtcNow,
            Actor = bob,
            Targets = [alice],
            Payload = new { }
        });
        
        // Act: Query activities targeting Alice (her notifications)
        var result = await activityService.QueryAsync(new ActivityQuery
        {
            TenantId = "blazorbook",
            Target = alice
        });
        
        // Assert
        Assert.That(result.Items, Has.Count.GreaterThanOrEqualTo(1));
        Assert.That(result.Items.Any(a => a.TypeKey == "user.followed"), Is.True);
    }

    [Test]
    public async Task QueryActivities_WithTimeRange_FiltersCorrectly()
    {
        // Arrange
        var (aliceId, alice) = await CreateUser("Alice", "alice", "alice@test.com");
        var activityService = GetActivityService();
        
        var now = DateTimeOffset.UtcNow;
        
        // Activity from yesterday
        await activityService.PublishAsync(new ActivityDto
        {
            TenantId = "blazorbook",
            TypeKey = "post.created",
            OccurredAt = now.AddDays(-1),
            Actor = alice,
            Payload = new { PostId = "old-post" }
        });
        
        // Activity from today
        await activityService.PublishAsync(new ActivityDto
        {
            TenantId = "blazorbook",
            TypeKey = "post.created",
            OccurredAt = now,
            Actor = alice,
            Payload = new { PostId = "new-post" }
        });
        
        // Act: Query only today's activities
        var result = await activityService.QueryAsync(new ActivityQuery
        {
            TenantId = "blazorbook",
            From = now.AddHours(-1)
        });
        
        // Assert: Should only return today's activity
        Assert.That(result.Items, Has.Count.EqualTo(1));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PAGINATION TESTS
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task QueryActivities_Pagination_ReturnsCorrectPages()
    {
        // Arrange
        var (aliceId, alice) = await CreateUser("Alice", "alice", "alice@test.com");
        var activityService = GetActivityService();
        
        // Create 10 activities
        for (int i = 0; i < 10; i++)
        {
            await activityService.PublishAsync(new ActivityDto
            {
                TenantId = "blazorbook",
                TypeKey = "post.created",
                OccurredAt = DateTimeOffset.UtcNow.AddMinutes(i),
                Actor = alice,
                Payload = new { PostId = $"post-{i}" }
            });
        }
        
        // Act: Get first page of 5
        var page1 = await activityService.QueryAsync(new ActivityQuery
        {
            TenantId = "blazorbook",
            Actor = alice,
            Limit = 5
        });
        
        Assert.That(page1.Items, Has.Count.EqualTo(5));
        Assert.That(page1.NextCursor, Is.Not.Null);
        
        // Get second page
        var page2 = await activityService.QueryAsync(new ActivityQuery
        {
            TenantId = "blazorbook",
            Actor = alice,
            Limit = 5,
            Cursor = page1.NextCursor
        });
        
        // Assert
        Assert.That(page2.Items, Has.Count.EqualTo(5));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ACTIVITY VISIBILITY TESTS
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task PublishActivity_WithPublicVisibility_Stored()
    {
        // Arrange
        var (aliceId, alice) = await CreateUser("Alice", "alice", "alice@test.com");
        var activityService = GetActivityService();
        
        // Act
        var activity = await activityService.PublishAsync(new ActivityDto
        {
            TenantId = "blazorbook",
            TypeKey = "post.created",
            OccurredAt = DateTimeOffset.UtcNow,
            Actor = alice,
            Visibility = ActivityVisibility.Public,
            Payload = new { PostId = "public-post" }
        });
        
        // Assert
        Assert.That(activity.Visibility, Is.EqualTo(ActivityVisibility.Public));
    }

    [Test]
    public async Task PublishActivity_WithTags_Stored()
    {
        // Arrange
        var (aliceId, alice) = await CreateUser("Alice", "alice", "alice@test.com");
        var activityService = GetActivityService();
        
        // Act
        var activity = await activityService.PublishAsync(new ActivityDto
        {
            TenantId = "blazorbook",
            TypeKey = "post.created",
            OccurredAt = DateTimeOffset.UtcNow,
            Actor = alice,
            Tags = ["social", "announcement", "important"],
            Payload = new { PostId = "tagged-post" }
        });
        
        // Assert
        Assert.That(activity.Tags, Has.Count.EqualTo(3));
        Assert.That(activity.Tags, Contains.Item("social"));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SOCIAL NETWORK ACTIVITY SCENARIOS
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task NotificationScenario_FollowerPostsContent()
    {
        // This simulates the full flow of:
        // 1. Bob follows Alice
        // 2. Alice creates a post
        // 3. Bob should see Alice's activity in his feed
        
        // Arrange
        var (aliceId, alice) = await CreateUser("Alice", "alice", "alice@test.com");
        await _fixture.CurrentUser.SignOutAsync();
        var (bobId, bob) = await CreateUser("Bob", "bob", "bob@test.com");
        
        var activityService = GetActivityService();
        
        // Bob follows Alice (creates activity)
        await activityService.PublishAsync(new ActivityDto
        {
            TenantId = "blazorbook",
            TypeKey = "user.followed",
            OccurredAt = DateTimeOffset.UtcNow,
            Actor = bob,
            Targets = [alice],
            Owner = bob, // Activity belongs to Bob's timeline
            Payload = new { FolloweeId = aliceId }
        });
        
        // Alice creates a post
        var postActivity = await activityService.PublishAsync(new ActivityDto
        {
            TenantId = "blazorbook",
            TypeKey = "post.created",
            OccurredAt = DateTimeOffset.UtcNow,
            Actor = alice,
            Visibility = ActivityVisibility.Public,
            Payload = new { PostId = "alice-new-post", Content = "Hello followers!" }
        });
        
        // Act: Query activities by Alice (for Bob's feed)
        var aliceActivities = await activityService.QueryAsync(new ActivityQuery
        {
            TenantId = "blazorbook",
            Actor = alice,
            TypeKey = "post.created"
        });
        
        // Assert
        Assert.That(aliceActivities.Items, Has.Count.EqualTo(1));
        Assert.That(aliceActivities.Items[0].Actor.Id, Is.EqualTo(aliceId));
    }

    [Test]
    public async Task NotificationScenario_CommentOnPost()
    {
        // Alice posts, Bob comments, Alice should get notification
        
        // Arrange
        var (aliceId, alice) = await CreateUser("Alice", "alice", "alice@test.com");
        await _fixture.CurrentUser.SignOutAsync();
        var (bobId, bob) = await CreateUser("Bob", "bob", "bob@test.com");
        
        var activityService = GetActivityService();
        var postEntity = new EntityRefDto { Kind = "Post", Type = "Post", Id = "alice-post" };
        
        // Bob comments on Alice's post
        await activityService.PublishAsync(new ActivityDto
        {
            TenantId = "blazorbook",
            TypeKey = "comment.created",
            OccurredAt = DateTimeOffset.UtcNow,
            Actor = bob,
            Targets = [postEntity, alice], // Target both the post and post author
            Summary = "Bob commented on your post",
            Payload = new { PostId = "alice-post", CommentId = "comment-1", Body = "Great post!" }
        });
        
        // Act: Query Alice's notifications (activities targeting her)
        var notifications = await activityService.QueryAsync(new ActivityQuery
        {
            TenantId = "blazorbook",
            Target = alice
        });
        
        // Assert
        Assert.That(notifications.Items, Has.Count.GreaterThanOrEqualTo(1));
        Assert.That(notifications.Items.Any(a => a.TypeKey == "comment.created"), Is.True);
    }

    [Test]
    public async Task NotificationScenario_MentionInPost()
    {
        // Bob mentions Alice in a post
        
        // Arrange
        var (aliceId, alice) = await CreateUser("Alice", "alice", "alice@test.com");
        await _fixture.CurrentUser.SignOutAsync();
        var (bobId, bob) = await CreateUser("Bob", "bob", "bob@test.com");
        
        var activityService = GetActivityService();
        
        // Bob mentions Alice
        await activityService.PublishAsync(new ActivityDto
        {
            TenantId = "blazorbook",
            TypeKey = "user.mentioned",
            OccurredAt = DateTimeOffset.UtcNow,
            Actor = bob,
            Targets = [alice],
            Summary = "Bob mentioned you in a post",
            Payload = new { PostId = "bob-post", MentionedUserId = aliceId }
        });
        
        // Act
        var mentions = await activityService.QueryAsync(new ActivityQuery
        {
            TenantId = "blazorbook",
            Target = alice,
            TypeKey = "user.mentioned"
        });
        
        // Assert
        Assert.That(mentions.Items, Has.Count.EqualTo(1));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TENANT ISOLATION
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task QueryActivities_TenantIsolation_DoesNotCrossOver()
    {
        // Arrange
        var (aliceId, alice) = await CreateUser("Alice", "alice", "alice@test.com");
        var activityService = GetActivityService();
        
        // Activity in tenant A
        await activityService.PublishAsync(new ActivityDto
        {
            TenantId = "tenant-a",
            TypeKey = "post.created",
            OccurredAt = DateTimeOffset.UtcNow,
            Actor = alice,
            Payload = new { PostId = "post-a" }
        });
        
        // Activity in tenant B
        await activityService.PublishAsync(new ActivityDto
        {
            TenantId = "tenant-b",
            TypeKey = "post.created",
            OccurredAt = DateTimeOffset.UtcNow,
            Actor = alice,
            Payload = new { PostId = "post-b" }
        });
        
        // Act: Query tenant A
        var result = await activityService.QueryAsync(new ActivityQuery
        {
            TenantId = "tenant-a"
        });
        
        // Assert: Only tenant A activity
        Assert.That(result.Items.All(a => a.TenantId == "tenant-a"), Is.True);
    }
}
