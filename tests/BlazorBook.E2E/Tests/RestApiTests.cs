using BlazorBook.E2E.Infrastructure;
using Microsoft.Playwright;
using System.Net.Http.Json;
using System.Text.Json;

namespace BlazorBook.E2E.Tests;

/// <summary>
/// E2E tests for REST API endpoints with JWT authentication.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Self)]
public class RestApiTests : BlazorBookPageTest
{
    private string? _jwtToken;
    private string? _testUserId;
    private string? _testProfileId;

    [Test, Order(1)]
    public async Task Api_Signup_ReturnsJwtToken()
    {
        // Arrange
        var uniqueId = GenerateUniqueId();
        var request = new
        {
            tenantId = "blazorbook",
            displayName = $"API Test User {uniqueId}",
            username = $"apitest{uniqueId}",
            email = $"apitest{uniqueId}@example.com",
            password = "TestPassword123!"
        };

        // Act
        var apiContext = await Page.Context.NewPageAsync();
        var response = await apiContext.APIRequest.PostAsync($"{BaseUrl}/api/auth/signup", new()
        {
            DataObject = request
        });

        // Assert
        Assert.That(response.Status, Is.EqualTo(200), "Signup should return 200 OK");
        
        var json = await response.JsonAsync();
        var token = json?.GetProperty("token").GetString();
        var userId = json?.GetProperty("user").GetProperty("id").GetString();
        var profileId = json?.GetProperty("profile").GetProperty("id").GetString();
        
        Assert.That(token, Is.Not.Null.And.Not.Empty, "Should return JWT token");
        Assert.That(userId, Is.Not.Null.And.Not.Empty, "Should return user.id");
        Assert.That(profileId, Is.Not.Null.And.Not.Empty, "Should return profile.id");
        
        // Store for other tests
        _jwtToken = token;
        _testUserId = userId;
        _testProfileId = profileId;
        
        await apiContext.CloseAsync();
    }

    [Test, Order(2)]
    public async Task Api_Login_ReturnsJwtToken()
    {
        // Arrange - create user first
        var uniqueId = GenerateUniqueId();
        var email = $"logintest{uniqueId}@example.com";
        var password = "TestPassword123!";
        
        var signupRequest = new
        {
            tenantId = "blazorbook",
            displayName = $"Login Test {uniqueId}",
            username = $"logintest{uniqueId}",
            email,
            password
        };

        var apiContext = await Page.Context.NewPageAsync();
        await apiContext.APIRequest.PostAsync($"{BaseUrl}/api/auth/signup", new()
        {
            DataObject = signupRequest
        });

        // Act - login with same credentials
        var loginRequest = new { tenantId = "blazorbook", usernameOrEmail = email, password };
        var response = await apiContext.APIRequest.PostAsync($"{BaseUrl}/api/auth/login", new()
        {
            DataObject = loginRequest
        });

        // Assert
        Assert.That(response.Status, Is.EqualTo(200), "Login should return 200 OK");
        
        var json = await response.JsonAsync();
        var token = json?.GetProperty("token").GetString();
        
        Assert.That(token, Is.Not.Null.And.Not.Empty, "Should return JWT token");
        
        await apiContext.CloseAsync();
    }

    [Test, Order(3)]
    public async Task Api_CreatePost_WithValidToken_ReturnsPost()
    {
        // Arrange - create user and get token
        var uniqueId = GenerateUniqueId();
        var apiContext = await Page.Context.NewPageAsync();
        
        var signupResponse = await apiContext.APIRequest.PostAsync($"{BaseUrl}/api/auth/signup", new()
        {
            DataObject = new
            {
                tenantId = "blazorbook",
                displayName = $"Post Creator {uniqueId}",
                username = $"postcreator{uniqueId}",
                email = $"postcreator{uniqueId}@example.com",
                password = "TestPassword123!"
            }
        });
        
        var signupJson = await signupResponse.JsonAsync();
        var token = signupJson?.GetProperty("token").GetString();

        // Act - create a post
        var postRequest = new
        {
            body = "This is a test post from the REST API! #testing",
            mediaIds = new string[] { },
            visibility = "Public"
        };
        
        var response = await apiContext.APIRequest.PostAsync($"{BaseUrl}/api/posts", new()
        {
            Headers = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {token}"
            },
            DataObject = postRequest
        });

        // Assert
        Assert.That(response.Status, Is.EqualTo(201), "Create post should return 201 Created");
        
        var json = await response.JsonAsync();
        var postId = json?.GetProperty("id").GetString();
        var body = json?.GetProperty("body").GetString();
        
        Assert.That(postId, Is.Not.Null.And.Not.Empty, "Should return post ID");
        Assert.That(body, Is.EqualTo(postRequest.body), "Should return the post body");
        
        await apiContext.CloseAsync();
    }

    [Test, Order(4)]
    public async Task Api_GetPosts_WithValidToken_ReturnsPosts()
    {
        // Arrange - create user, token, and post
        var uniqueId = GenerateUniqueId();
        var apiContext = await Page.Context.NewPageAsync();
        
        var signupResponse = await apiContext.APIRequest.PostAsync($"{BaseUrl}/api/auth/signup", new()
        {
            DataObject = new
            {
                tenantId = "blazorbook",
                displayName = $"Feed Reader {uniqueId}",
                username = $"feedreader{uniqueId}",
                email = $"feedreader{uniqueId}@example.com",
                password = "TestPassword123!"
            }
        });
        
        var signupJson = await signupResponse.JsonAsync();
        var token = signupJson?.GetProperty("token").GetString();
        
        // Create a post
        await apiContext.APIRequest.PostAsync($"{BaseUrl}/api/posts", new()
        {
            Headers = new Dictionary<string, string> { ["Authorization"] = $"Bearer {token}" },
            DataObject = new
            {
                body = "Test post for feed",
                mediaIds = new string[] { },
                visibility = "Public"
            }
        });

        // Act - get posts
        var response = await apiContext.APIRequest.GetAsync($"{BaseUrl}/api/posts?limit=10", new()
        {
            Headers = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {token}"
            }
        });

        // Assert
        Assert.That(response.Status, Is.EqualTo(200), "Get posts should return 200 OK");
        
        var json = await response.JsonAsync();
        var items = json?.GetProperty("items");
        
        Assert.That(items?.GetArrayLength(), Is.GreaterThan(0), "Should return at least one post");
        
        await apiContext.CloseAsync();
    }

    [Test, Order(5)]
    public async Task Api_CreatePost_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var apiContext = await Page.Context.NewPageAsync();
        var postRequest = new
        {
            body = "This should fail without auth",
            mediaIds = new string[] { },
            visibility = "Public"
        };

        // Act - try to create post without token
        var response = await apiContext.APIRequest.PostAsync($"{BaseUrl}/api/posts", new()
        {
            DataObject = postRequest,
            FailOnStatusCode = false
        });

        // Assert
        Assert.That(response.Status, Is.EqualTo(401), "Should return 401 Unauthorized");
        
        await apiContext.CloseAsync();
    }

    [Test, Order(6)]
    public async Task Api_UpdatePost_WithValidToken_ReturnsUpdatedPost()
    {
        // Arrange - create user, token, and post
        var uniqueId = GenerateUniqueId();
        var apiContext = await Page.Context.NewPageAsync();
        
        var signupResponse = await apiContext.APIRequest.PostAsync($"{BaseUrl}/api/auth/signup", new()
        {
            DataObject = new
            {
                tenantId = "blazorbook",
                displayName = $"Post Editor {uniqueId}",
                username = $"posteditor{uniqueId}",
                email = $"posteditor{uniqueId}@example.com",
                password = "TestPassword123!"
            }
        });
        
        var signupJson = await signupResponse.JsonAsync();
        var token = signupJson?.GetProperty("token").GetString();
        
        // Create initial post
        var createResponse = await apiContext.APIRequest.PostAsync($"{BaseUrl}/api/posts", new()
        {
            Headers = new Dictionary<string, string> { ["Authorization"] = $"Bearer {token}" },
            DataObject = new
            {
                body = "Original post content",
                mediaIds = new string[] { },
                visibility = "Public"
            }
        });
        
        var createJson = await createResponse.JsonAsync();
        var postId = createJson?.GetProperty("id").GetString();

        // Act - update the post
        var updateRequest = new
        {
            body = "Updated post content! 🎉",
            mediaIds = new string[] { },
            visibility = "Public"
        };
        
        var response = await apiContext.APIRequest.PutAsync($"{BaseUrl}/api/posts/{postId}", new()
        {
            Headers = new Dictionary<string, string> { ["Authorization"] = $"Bearer {token}" },
            DataObject = updateRequest
        });

        // Assert
        Assert.That(response.Status, Is.EqualTo(200), "Update should return 200 OK");
        
        var json = await response.JsonAsync();
        var updatedBody = json?.GetProperty("body").GetString();
        
        Assert.That(updatedBody, Is.EqualTo(updateRequest.body), "Should return updated content");
        
        await apiContext.CloseAsync();
    }

    [Test, Order(7)]
    public async Task Api_DeletePost_WithValidToken_ReturnsNoContent()
    {
        // Arrange - create user, token, and post
        var uniqueId = GenerateUniqueId();
        var apiContext = await Page.Context.NewPageAsync();
        
        var signupResponse = await apiContext.APIRequest.PostAsync($"{BaseUrl}/api/auth/signup", new()
        {
            DataObject = new
            {
                tenantId = "blazorbook",
                displayName = $"Post Deleter {uniqueId}",
                username = $"postdeleter{uniqueId}",
                email = $"postdeleter{uniqueId}@example.com",
                password = "TestPassword123!"
            }
        });
        
        var signupJson = await signupResponse.JsonAsync();
        var token = signupJson?.GetProperty("token").GetString();
        
        // Create post to delete
        var createResponse = await apiContext.APIRequest.PostAsync($"{BaseUrl}/api/posts", new()
        {
            Headers = new Dictionary<string, string> { ["Authorization"] = $"Bearer {token}" },
            DataObject = new
            {
                body = "Post to be deleted",
                mediaIds = new string[] { },
                visibility = "Public"
            }
        });
        
        var createJson = await createResponse.JsonAsync();
        var postId = createJson?.GetProperty("id").GetString();

        // Act - delete the post
        var response = await apiContext.APIRequest.DeleteAsync($"{BaseUrl}/api/posts/{postId}", new()
        {
            Headers = new Dictionary<string, string> { ["Authorization"] = $"Bearer {token}" }
        });

        // Assert
        Assert.That(response.Status, Is.EqualTo(204), "Delete should return 204 No Content");
        
        // Verify post is deleted (should return 404 or not appear in list)
        var getResponse = await apiContext.APIRequest.GetAsync($"{BaseUrl}/api/posts/{postId}", new()
        {
            Headers = new Dictionary<string, string> { ["Authorization"] = $"Bearer {token}" },
            FailOnStatusCode = false
        });
        
        Assert.That(getResponse.Status, Is.EqualTo(404), "Deleted post should return 404");
        
        await apiContext.CloseAsync();
    }

    [Test]
    public async Task Swagger_IsAccessible()
    {
        // Act
        var apiContext = await Page.Context.NewPageAsync();
        var response = await apiContext.APIRequest.GetAsync($"{BaseUrl}/swagger/index.html");

        // Assert
        Assert.That(response.Status, Is.EqualTo(200), "Swagger UI should be accessible");
        var html = await response.TextAsync();
        Assert.That(html, Does.Contain("swagger"), "Should contain Swagger UI");
        
        await apiContext.CloseAsync();
    }
}
