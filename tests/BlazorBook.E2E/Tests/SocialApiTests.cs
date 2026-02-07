using BlazorBook.E2E.Infrastructure;
using Microsoft.Playwright;
using System.Text.Json;

namespace BlazorBook.E2E.Tests;

/// <summary>
/// E2E tests for Chat and Relationships API endpoints.
/// Tests follow, unfollow, messaging, and social graph features.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Self)]
public class SocialApiTests : BlazorBookPageTest
{
    private async Task<(string Token, string ProfileId, string UserId)> CreateUserAsync(string prefix)
    {
        var uniqueId = GenerateUniqueId();
        var apiContext = await Page.Context.NewPageAsync();
        
        var response = await apiContext.APIRequest.PostAsync($"{BaseUrl}/api/auth/signup", new()
        {
            DataObject = new
            {
                tenantId = "blazorbook",
                displayName = $"{prefix} User {uniqueId}",
                username = $"{prefix.ToLower()}{uniqueId}",
                email = $"{prefix.ToLower()}{uniqueId}@example.com",
                password = "TestPassword123!"
            }
        });
        
        var json = await response.JsonAsync();
        var token = json?.GetProperty("token").GetString() ?? "";
        var userId = json?.GetProperty("user").GetProperty("id").GetString() ?? "";
        var profileId = json?.GetProperty("profile").GetProperty("id").GetString() ?? "";
        
        await apiContext.CloseAsync();
        return (token, profileId, userId);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // RELATIONSHIPS TESTS
    // ═══════════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task Api_Follow_User_ReturnsSuccess()
    {
        // Arrange - create two users
        var (followerToken, followerProfileId, _) = await CreateUserAsync("Follower");
        var (_, targetProfileId, _) = await CreateUserAsync("Target");
        
        var apiContext = await Page.Context.NewPageAsync();

        // Act - follower follows target
        var response = await apiContext.APIRequest.PostAsync(
            $"{BaseUrl}/api/relationships/follow/{targetProfileId}", new()
        {
            Headers = new Dictionary<string, string> { ["Authorization"] = $"Bearer {followerToken}" }
        });

        // Assert
        Assert.That(response.Status, Is.EqualTo(200), "Follow should return 200 OK");
        
        var json = await response.JsonAsync();
        Assert.That(json?.GetProperty("followingId").GetString(), Is.EqualTo(targetProfileId));
        
        await apiContext.CloseAsync();
    }

    [Test]
    public async Task Api_Unfollow_User_ReturnsNoContent()
    {
        // Arrange - create two users and follow
        var (followerToken, _, _) = await CreateUserAsync("Unfollower");
        var (_, targetProfileId, _) = await CreateUserAsync("UnfollowTarget");
        
        var apiContext = await Page.Context.NewPageAsync();

        // First follow
        await apiContext.APIRequest.PostAsync(
            $"{BaseUrl}/api/relationships/follow/{targetProfileId}", new()
        {
            Headers = new Dictionary<string, string> { ["Authorization"] = $"Bearer {followerToken}" }
        });

        // Act - unfollow
        var response = await apiContext.APIRequest.DeleteAsync(
            $"{BaseUrl}/api/relationships/follow/{targetProfileId}", new()
        {
            Headers = new Dictionary<string, string> { ["Authorization"] = $"Bearer {followerToken}" }
        });

        // Assert
        Assert.That(response.Status, Is.EqualTo(204), "Unfollow should return 204 No Content");
        
        await apiContext.CloseAsync();
    }

    [Test]
    public async Task Api_GetFollowing_ReturnsFollowedUsers()
    {
        // Arrange - create users and follow
        var (userToken, _, _) = await CreateUserAsync("FollowList");
        var (_, target1ProfileId, _) = await CreateUserAsync("FollowedUser1");
        var (_, target2ProfileId, _) = await CreateUserAsync("FollowedUser2");
        
        var apiContext = await Page.Context.NewPageAsync();

        // Follow both targets
        await apiContext.APIRequest.PostAsync(
            $"{BaseUrl}/api/relationships/follow/{target1ProfileId}", new()
        {
            Headers = new Dictionary<string, string> { ["Authorization"] = $"Bearer {userToken}" }
        });
        await apiContext.APIRequest.PostAsync(
            $"{BaseUrl}/api/relationships/follow/{target2ProfileId}", new()
        {
            Headers = new Dictionary<string, string> { ["Authorization"] = $"Bearer {userToken}" }
        });

        // Act - get following list
        var response = await apiContext.APIRequest.GetAsync(
            $"{BaseUrl}/api/relationships/following", new()
        {
            Headers = new Dictionary<string, string> { ["Authorization"] = $"Bearer {userToken}" }
        });

        // Assert
        Assert.That(response.Status, Is.EqualTo(200), "Get following should return 200 OK");
        
        var json = await response.JsonAsync();
        var count = json?.GetProperty("count").GetInt32();
        Assert.That(count, Is.GreaterThanOrEqualTo(2), "Should have at least 2 following");
        
        await apiContext.CloseAsync();
    }

    [Test]
    public async Task Api_CannotFollowSelf()
    {
        // Arrange
        var (userToken, userProfileId, _) = await CreateUserAsync("SelfFollow");
        
        var apiContext = await Page.Context.NewPageAsync();

        // Act - try to follow self
        var response = await apiContext.APIRequest.PostAsync(
            $"{BaseUrl}/api/relationships/follow/{userProfileId}", new()
        {
            Headers = new Dictionary<string, string> { ["Authorization"] = $"Bearer {userToken}" },
            FailOnStatusCode = false
        });

        // Assert
        Assert.That(response.Status, Is.EqualTo(400), "Should return 400 Bad Request");
        
        await apiContext.CloseAsync();
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // CHAT TESTS
    // ═══════════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task Api_CreateDirectConversation_ReturnsConversation()
    {
        // Arrange
        var (user1Token, _, _) = await CreateUserAsync("ChatUser1");
        var (_, user2ProfileId, _) = await CreateUserAsync("ChatUser2");
        
        var apiContext = await Page.Context.NewPageAsync();

        // Act - create direct conversation
        var response = await apiContext.APIRequest.PostAsync(
            $"{BaseUrl}/api/chat/conversations/direct/{user2ProfileId}", new()
        {
            Headers = new Dictionary<string, string> { ["Authorization"] = $"Bearer {user1Token}" }
        });

        // Assert
        Assert.That(response.Status, Is.EqualTo(200), "Create conversation should return 200 OK");
        
        var json = await response.JsonAsync();
        var type = json?.GetProperty("type").GetInt32();
        Assert.That(type, Is.EqualTo(0), "Should be Direct conversation type (0)");
        
        await apiContext.CloseAsync();
    }

    [Test]
    public async Task Api_SendMessage_ReturnsMessage()
    {
        // Arrange
        var (user1Token, _, _) = await CreateUserAsync("Sender");
        var (_, user2ProfileId, _) = await CreateUserAsync("Receiver");
        
        var apiContext = await Page.Context.NewPageAsync();

        // Create conversation first
        var convResponse = await apiContext.APIRequest.PostAsync(
            $"{BaseUrl}/api/chat/conversations/direct/{user2ProfileId}", new()
        {
            Headers = new Dictionary<string, string> { ["Authorization"] = $"Bearer {user1Token}" }
        });
        var convJson = await convResponse.JsonAsync();
        var conversationId = convJson?.GetProperty("id").GetString();

        // Act - send message
        var response = await apiContext.APIRequest.PostAsync(
            $"{BaseUrl}/api/chat/conversations/{conversationId}/messages", new()
        {
            Headers = new Dictionary<string, string> { ["Authorization"] = $"Bearer {user1Token}" },
            DataObject = new { body = "Hello from E2E test!" }
        });

        // Assert
        Assert.That(response.Status, Is.EqualTo(200), "Send message should return 200 OK");
        
        var json = await response.JsonAsync();
        var body = json?.GetProperty("body").GetString();
        Assert.That(body, Is.EqualTo("Hello from E2E test!"));
        
        await apiContext.CloseAsync();
    }

    [Test]
    public async Task Api_GetConversations_ReturnsList()
    {
        // Arrange
        var (userToken, _, _) = await CreateUserAsync("ConvListUser");
        var (_, otherProfileId, _) = await CreateUserAsync("OtherUser");
        
        var apiContext = await Page.Context.NewPageAsync();

        // Create a conversation
        await apiContext.APIRequest.PostAsync(
            $"{BaseUrl}/api/chat/conversations/direct/{otherProfileId}", new()
        {
            Headers = new Dictionary<string, string> { ["Authorization"] = $"Bearer {userToken}" }
        });

        // Act - get conversations
        var response = await apiContext.APIRequest.GetAsync(
            $"{BaseUrl}/api/chat/conversations", new()
        {
            Headers = new Dictionary<string, string> { ["Authorization"] = $"Bearer {userToken}" }
        });

        // Assert
        Assert.That(response.Status, Is.EqualTo(200), "Get conversations should return 200 OK");
        
        var json = await response.JsonAsync();
        var items = json?.GetProperty("items").GetArrayLength();
        Assert.That(items, Is.GreaterThanOrEqualTo(1), "Should have at least 1 conversation");
        
        await apiContext.CloseAsync();
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // SEARCH TESTS
    // ═══════════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task Api_Search_WithQuery_ReturnsResults()
    {
        // Arrange
        var (userToken, _, _) = await CreateUserAsync("Searcher");
        
        var apiContext = await Page.Context.NewPageAsync();

        // Act - search for something
        var response = await apiContext.APIRequest.GetAsync(
            $"{BaseUrl}/api/search?q=test", new()
        {
            Headers = new Dictionary<string, string> { ["Authorization"] = $"Bearer {userToken}" }
        });

        // Assert
        Assert.That(response.Status, Is.EqualTo(200), "Search should return 200 OK");
        
        await apiContext.CloseAsync();
    }

    [Test]
    public async Task Api_SearchUsers_ReturnsUserResults()
    {
        // Arrange
        var (userToken, _, _) = await CreateUserAsync("UserSearcher");
        
        var apiContext = await Page.Context.NewPageAsync();

        // Act - search users
        var response = await apiContext.APIRequest.GetAsync(
            $"{BaseUrl}/api/search/users?q=test", new()
        {
            Headers = new Dictionary<string, string> { ["Authorization"] = $"Bearer {userToken}" }
        });

        // Assert
        Assert.That(response.Status, Is.EqualTo(200), "User search should return 200 OK");
        
        await apiContext.CloseAsync();
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // INBOX TESTS
    // ═══════════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task Api_GetInbox_ReturnsNotifications()
    {
        // Arrange
        var (userToken, _, _) = await CreateUserAsync("InboxUser");
        
        var apiContext = await Page.Context.NewPageAsync();

        // Act - get inbox
        var response = await apiContext.APIRequest.GetAsync(
            $"{BaseUrl}/api/inbox", new()
        {
            Headers = new Dictionary<string, string> { ["Authorization"] = $"Bearer {userToken}" }
        });

        // Assert
        Assert.That(response.Status, Is.EqualTo(200), "Get inbox should return 200 OK");
        
        await apiContext.CloseAsync();
    }

    [Test]
    public async Task Api_GetInboxUnreadCount_ReturnsCount()
    {
        // Arrange
        var (userToken, _, _) = await CreateUserAsync("UnreadUser");
        
        var apiContext = await Page.Context.NewPageAsync();

        // Act - get unread count
        var response = await apiContext.APIRequest.GetAsync(
            $"{BaseUrl}/api/inbox/unread-count", new()
        {
            Headers = new Dictionary<string, string> { ["Authorization"] = $"Bearer {userToken}" }
        });

        // Assert
        Assert.That(response.Status, Is.EqualTo(200), "Get unread count should return 200 OK");
        
        var json = await response.JsonAsync();
        Assert.That(json?.TryGetProperty("unreadCount", out _), Is.True, "Should have unreadCount property");
        
        await apiContext.CloseAsync();
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // MEDIA TESTS
    // ═══════════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task Api_GetMedia_WithInvalidId_Returns404()
    {
        // Arrange
        var (userToken, _, _) = await CreateUserAsync("MediaUser");
        
        var apiContext = await Page.Context.NewPageAsync();

        // Act - try to get non-existent media
        var response = await apiContext.APIRequest.GetAsync(
            $"{BaseUrl}/api/media/nonexistent-id", new()
        {
            Headers = new Dictionary<string, string> { ["Authorization"] = $"Bearer {userToken}" },
            FailOnStatusCode = false
        });

        // Assert
        Assert.That(response.Status, Is.EqualTo(404), "Should return 404 for non-existent media");
        
        await apiContext.CloseAsync();
    }
}
