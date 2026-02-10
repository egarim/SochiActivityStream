using BlazorBook.E2E.Infrastructure;
using Microsoft.Playwright;

namespace BlazorBook.E2E.Tests;

[TestFixture]
[Category("Diagnostic")]
public class ProfileEditDiagnosticTest : BlazorBookPageTest
{
    [Test]
    [Description("Diagnostic test - check if edit dialog opens and logs state")]
    public async Task ProfileEdit_Diagnostic_CheckDialogBehavior()
    {
        var uniqueId = GenerateUniqueId();
        var testUser = $"diagtest{uniqueId}";

        // Sign up
        await SignUpAsync($"Diag Test {uniqueId}", testUser, $"{testUser}@example.com", "Test123!");
        await WaitForBlazorAsync();

        // Navigate to Profile
        await Page.GetByRole(AriaRole.Link, new() { Name = "Profile" }).ClickAsync();
        await WaitForBlazorAsync();
        Console.WriteLine("✅ Navigated to profile page");

        // Take screenshot before clicking edit
        await Page.ScreenshotAsync(new() { Path = $"screenshots/diag-before-edit-{DateTime.Now:HHmmss}.png" });

        // Click Edit Profile button
        var editButton = Page.GetByRole(AriaRole.Button, new() { Name = "Edit Profile" });
        await editButton.ClickAsync();
        Console.WriteLine("✅ Clicked Edit Profile button");
        
        await WaitForBlazorAsync(2000);

        // Take screenshot after clicking edit
        await Page.ScreenshotAsync(new() { Path = $"screenshots/diag-after-edit-click-{DateTime.Now:HHmmss}.png" });

        // Check if dialog is visible
        var dialogTitle = Page.GetByText("Edit Profile", new() { Exact = true });
        var isDialogVisible = await dialogTitle.IsVisibleAsync();
        Console.WriteLine($"Dialog visible: {isDialogVisible}");

        if (isDialogVisible)
        {
            Console.WriteLine("✅ Dialog opened successfully");

            // Try to fill fields
            var displayNameField = Page.GetByLabel("Display Name");
            await displayNameField.ClearAsync();
            await displayNameField.FillAsync("Test Updated Name");
            Console.WriteLine("✅ Filled display name");

            await Page.ScreenshotAsync(new() { Path = $"screenshots/diag-before-save-{DateTime.Now:HHmmss}.png" });

            // Click save
            var saveButton = Page.Locator("#edit-profile-save");
            await saveButton.ClickAsync();
            Console.WriteLine("✅ Clicked save button");

            await WaitForBlazorAsync(3000);

            await Page.ScreenshotAsync(new() { Path = $"screenshots/diag-after-save-{DateTime.Now:HHmmss}.png" });

            // Check if dialog closed
            var dialogStillVisible = await dialogTitle.IsVisibleAsync();
            Console.WriteLine($"Dialog still visible after save: {dialogStillVisible}");

            // Check page HTML for any error messages
            var pageContent = await Page.ContentAsync();
            if (pageContent.Contains("error") || pageContent.Contains("Error"))
            {
                Console.WriteLine("⚠️ Page contains error text");
            }
        }
        else
        {
            Console.WriteLine("❌ Dialog did NOT open");
            
            // Check what's on the page instead
            var pageContent = await Page.ContentAsync();
            if (pageContent.Contains("Unknown dialog"))
            {
                Console.WriteLine("❌ Found 'Unknown dialog' message");
            }
        }
    }
}
