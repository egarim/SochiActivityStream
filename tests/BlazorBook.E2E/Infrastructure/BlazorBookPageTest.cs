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
        
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Display Name" }).FillAsync(displayName);
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Username" }).FillAsync(username);
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email address" }).FillAsync(email);
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).FillAsync(password);
        
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign Up" }).ClickAsync();
        await WaitForBlazorAsync();
    }

    /// <summary>
    /// Logs in with the given credentials.
    /// </summary>
    protected async Task LoginAsync(string email, string password)
    {
        await NavigateToAsync("/login");
        
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email address" }).FillAsync(email);
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).FillAsync(password);
        
        await Page.GetByRole(AriaRole.Button, new() { Name = "Log In" }).ClickAsync();
        await WaitForBlazorAsync();
    }

    /// <summary>
    /// Creates a new post with the given content.
    /// </summary>
    protected async Task CreatePostAsync(string content)
    {
        var postBox = Page.GetByRole(AriaRole.Textbox, new() { NameRegex = new System.Text.RegularExpressions.Regex("What's on your mind") });
        await postBox.FillAsync(content);
        
        // Tab out to enable the Post button
        await Page.Keyboard.PressAsync("Tab");
        await WaitForBlazorAsync(200);
        
        await Page.GetByRole(AriaRole.Button, new() { Name = "Post" }).ClickAsync();
        await WaitForBlazorAsync();
    }

    /// <summary>
    /// Generates a unique identifier for test data.
    /// </summary>
    protected static string GenerateUniqueId() => Guid.NewGuid().ToString("N")[..8];
}
