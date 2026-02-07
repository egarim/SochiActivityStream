using BlazorBook.E2E.Infrastructure;
using Microsoft.Playwright;

namespace BlazorBook.E2E.Tests;

/// <summary>
/// E2E tests for authentication flows (sign up, login, logout).
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Self)]
public class AuthenticationTests : BlazorBookPageTest
{
    [Test]
    public async Task HomePage_ShowsLoginAndSignUpLinks()
    {
        // Act
        await NavigateToAsync("/");
        
        // Assert
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Log In" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Sign Up" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task SignUp_WithValidData_RedirectsToFeed()
    {
        // Arrange
        var uniqueId = GenerateUniqueId();
        var displayName = $"Test User {uniqueId}";
        var username = $"testuser{uniqueId}";
        var email = $"test{uniqueId}@example.com";
        var password = "password123";
        
        // Act
        await SignUpAsync(displayName, username, email, password);
        
        // Assert - should be on feed page
        await Expect(Page).ToHaveURLAsync($"{BaseUrl}/feed");
        // MudBlazor text field with ID selector
        await Expect(Page.Locator("#post-content")).ToBeVisibleAsync();
    }

    [Test]
    public async Task SignUp_WithEmptyFields_StaysOnSignUpPage()
    {
        // Arrange
        await NavigateToAsync("/signup");
        
        // Act - click sign up without filling fields (using ID selector)
        await Page.Locator("#signup-button").ClickAsync();
        await WaitForBlazorAsync();
        
        // Assert - should stay on sign up page (form doesn't submit with empty fields)
        await Expect(Page).ToHaveURLAsync($"{BaseUrl}/signup");
    }

    [Test]
    public async Task Login_WithValidCredentials_RedirectsToFeed()
    {
        // Arrange - first create a user
        var uniqueId = GenerateUniqueId();
        var displayName = $"Login Test {uniqueId}";
        var username = $"logintest{uniqueId}";
        var email = $"login{uniqueId}@example.com";
        var password = "password123";
        
        await SignUpAsync(displayName, username, email, password);
        
        // Navigate away and log back in
        await NavigateToAsync("/login");
        
        // Act
        await LoginAsync(email, password);
        
        // Assert - should be on feed page
        await Expect(Page).ToHaveURLAsync($"{BaseUrl}/feed");
    }

    [Test]
    public async Task Login_WithInvalidCredentials_ShowsError()
    {
        // Arrange
        await NavigateToAsync("/login");
        
        // Act
        await Page.GetByPlaceholder("Email address").FillAsync("nonexistent@example.com");
        await Page.GetByPlaceholder("Password").FillAsync("wrongpassword");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Log In" }).ClickAsync();
        await WaitForBlazorAsync();
        
        // Assert - should show error message
        var alert = Page.GetByRole(AriaRole.Alert);
        await Expect(alert).ToBeVisibleAsync();
        await Expect(alert).ToContainTextAsync("Invalid");
    }

    [Test]
    public async Task LoginPage_HasLinkToSignUp()
    {
        // Arrange
        await NavigateToAsync("/login");
        
        // Assert
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Create New Account" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task SignUpPage_HasLinkToLogin()
    {
        // Arrange
        await NavigateToAsync("/signup");
        
        // Assert
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Already have an account?" })).ToBeVisibleAsync();
    }
}
