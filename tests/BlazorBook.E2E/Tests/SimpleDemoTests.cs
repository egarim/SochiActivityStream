using Microsoft.Playwright.NUnit;
using Microsoft.Playwright;

namespace BlazorBook.E2E.Tests;

/// <summary>
/// Simple demo test to verify the automated testing framework works.
/// This test demonstrates capturing  screenshots and testing page elements.
/// </summary>
[TestFixture]
public class SimpleDemoTests : PageTest
{
    private string BaseUrl => "http://localhost:5555";

    [Test]
    public async Task Demo_HomePage_Loads_And_CapturesScreenshot()
    {
        // Arrange - Navigate to home page
        await Page.GotoAsync(BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        // Create screenshots directory if it doesn't exist
        var screenshotsDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "screenshots");
        Directory.CreateDirectory(screenshotsDir);

        // Act - Capture screenshot
        var screenshotPath = Path.Combine(screenshotsDir, "demo-homepage.png");
        await Page.ScreenshotAsync(new() { Path = screenshotPath, FullPage = true });

        // Assert
        await Expect(Page).ToHaveTitleAsync(new System.Text.RegularExpressions.Regex("BlazorBook"));
        
        // Verify screenshot was created
        Assert.That(File.Exists(screenshotPath), Is.True, $"Screenshot should be created at {screenshotPath}");
        
        var fileInfo = new FileInfo(screenshotPath);
        Console.WriteLine($"✓ Screenshot captured: {screenshotPath}");
        Console.WriteLine($"✓ File size: {fileInfo.Length / 1024} KB");
        Console.WriteLine($"✓ Page title contains 'BlazorBook'");
    }

    [Test]
    public async Task Demo_VerifyBlazorBookBrandingVisible()
    {
        // Navigate to home page
        await Page.GotoAsync(BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify BlazorBook branding is visible
        var brandingLocator = Page.GetByText("BlazorBook");
        await Expect(brandingLocator.First).ToBeVisibleAsync();

        Console.WriteLine("✓ BlazorBook branding is visible on homepage");
    }

    [Test]
    public async Task Demo_MultipleViewportSizes()
    {
        var screenshotsDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "screenshots");
        Directory.CreateDirectory(screenshotsDir);

        var viewports = new[]
        {
            (Width: 375, Height: 667, Name: "mobile"),
            (Width: 768, Height: 1024, Name: "tablet"),
            (Width: 1920, Height: 1080, Name: "desktop")
        };

        foreach (var (width, height, name) in viewports)
        {
            await Page.SetViewportSizeAsync(width, height);
            await Page.GotoAsync(BaseUrl);
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var screenshotPath = Path.Combine(screenshotsDir, $"demo-{name}-{width}x{height}.png");
            await Page.ScreenshotAsync(new() { Path = screenshotPath, FullPage = true });

            Console.WriteLine($"✓ Captured {name} screenshot ({width}x{height}): {screenshotPath}");
        }

        var screenshots = Directory.GetFiles(screenshotsDir, "demo-*.png");
        Assert.That(screenshots.Length, Is.GreaterThanOrEqualTo(3), "Should have captured at least 3 responsive screenshots");
        
        Console.WriteLine($"✓ Total screenshots captured: {screenshots.Length}");
    }
}
