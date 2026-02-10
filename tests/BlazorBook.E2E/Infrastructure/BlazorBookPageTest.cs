using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace BlazorBook.E2E.Infrastructure;

/// <summary>
/// Base class for all BlazorBook E2E tests.
/// Provides common setup, page helpers, and test utilities.
/// 
/// By default, expects BlazorBook.Web to be running on http://localhost:5555
/// Set BLAZORBOOK_URL environment variable or use runsettings to override.
/// Set BLAZORBOOK_START_SERVER=true to auto-start the server.
/// </summary>
public class BlazorBookPageTest : PageTest
{
    private static BlazorBookWebFactory? _webFactory;
    private static readonly object _lock = new();
    private static bool _isServerStarted;
    private static string _baseUrl = "http://localhost:5555";
    
    /// <summary>
    /// The base URL of the running BlazorBook application.
    /// </summary>
    protected string BaseUrl => _baseUrl;

    /// <summary>
    /// One-time setup: Optionally start the web application.
    /// </summary>
    [OneTimeSetUp]
    public async Task OneTimeSetupAsync()
    {
        // Check for custom URL
        var customUrl = Environment.GetEnvironmentVariable("BLAZORBOOK_URL") ??
                        TestContext.Parameters.Get("BlazorBookUrl");
        if (!string.IsNullOrEmpty(customUrl))
        {
            _baseUrl = customUrl;
        }

        // Check if we should auto-start the server
        var startServer = Environment.GetEnvironmentVariable("BLAZORBOOK_START_SERVER") ??
                          TestContext.Parameters.Get("StartServer");
        
        if (string.Equals(startServer, "true", StringComparison.OrdinalIgnoreCase))
        {
            lock (_lock)
            {
                if (!_isServerStarted)
                {
                    _webFactory = new BlazorBookWebFactory();
                    _webFactory.StartAsync().GetAwaiter().GetResult();
                    _baseUrl = _webFactory.BaseUrl;
                    _isServerStarted = true;
                }
            }
        }
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// One-time teardown: Stop the web application.
    /// </summary>
    [OneTimeTearDown]
    public void OneTimeTeardown()
    {
        // Only dispose if we're the last test class
        // In practice, the factory handles cleanup on process exit
    }

    /// <summary>
    /// Navigates to the specified path on the BlazorBook application.
    /// </summary>
    protected async Task NavigateToAsync(string path = "/")
    {
        var url = $"{BaseUrl}{path}";
        await Page.GotoAsync(url);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Waits for Blazor to finish processing updates.
    /// </summary>
    protected async Task WaitForBlazorAsync(int delayMs = 500)
    {
        await Task.Delay(delayMs);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Signs up a new user with the given details.
    /// </summary>
    protected async Task SignUpAsync(string displayName, string username, string email, string password)
    {
        await NavigateToAsync("/signup");
        
        // Use ID-based selectors for MudBlazor components
        // IMPORTANT: Fill username BEFORE displayname because DisplayName setter
        // auto-generates Handle if empty. We want explicit username, not auto-generated.
        await Page.Locator("#signup-username").FillAsync(username);
        await Page.Locator("#signup-displayname").FillAsync(displayName);
        await Page.Locator("#signup-email").FillAsync(email);
        await Page.Locator("#signup-password").FillAsync(password);
        
        await Page.Locator("#signup-button").ClickAsync();
        await WaitForBlazorAsync();
    }

    /// <summary>
    /// Logs in with the given credentials.
    /// </summary>
    protected async Task LoginAsync(string email, string password)
    {
        await NavigateToAsync("/login");
        
        // Use ID-based selectors for MudBlazor components
        await Page.Locator("#login-email").FillAsync(email);
        await Page.Locator("#login-password").FillAsync(password);
        
        await Page.Locator("#login-button").ClickAsync();
        await WaitForBlazorAsync();
    }

    /// <summary>
    /// Creates a new post with the given content.
    /// MudBlazor components work with Playwright automation.
    /// </summary>
    protected async Task CreatePostAsync(string content)
    {
        // MudBlazor MudTextField works with standard Playwright FillAsync
        await Page.Locator("#post-content").FillAsync(content);
        await WaitForBlazorAsync(300);
        
        await Page.Locator("#post-button").ClickAsync();
        await WaitForBlazorAsync();
    }

    /// <summary>
    /// Generates a unique identifier for test data.
    /// </summary>
    protected static string GenerateUniqueId() => Guid.NewGuid().ToString("N")[..8];
}
