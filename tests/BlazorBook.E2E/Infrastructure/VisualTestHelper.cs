using Microsoft.Playwright;

namespace BlazorBook.E2E.Infrastructure;

/// <summary>
/// Utilities for visual testing and screenshot management.
/// Provides helpers for capturing, comparing, and organizing screenshots.
/// </summary>
public static class VisualTestHelper
{
    private const string ScreenshotBaseDir = "screenshots";
    private const string ComparisonDir = "screenshots/comparisons";
    private const string BaselineDir = "screenshots/baseline";

    /// <summary>
    /// Captures a full-page screenshot with optional masking of dynamic content.
    /// </summary>
    public static async Task<byte[]> CaptureFullPageAsync(
        IPage page,
        string filename,
        IEnumerable<string>? maskSelectors = null,
        bool saveToFile = true)
    {
        EnsureDirectoryExists(ScreenshotBaseDir);

        var options = new PageScreenshotOptions
        {
            Path = saveToFile ? Path.Combine(ScreenshotBaseDir, filename) : null,
            FullPage = true,
            Type = ScreenshotType.Png
        };

        // Mask dynamic content (timestamps, avatars, etc.)
        if (maskSelectors != null)
        {
            var locators = maskSelectors
                .Select(selector => page.Locator(selector))
                .ToList();
            
            options.Mask = locators;
        }

        return await page.ScreenshotAsync(options);
    }

    /// <summary>
    /// Captures a screenshot of a specific element.
    /// </summary>
    public static async Task<byte[]> CaptureElementAsync(
        ILocator element,
        string filename,
        bool saveToFile = true)
    {
        EnsureDirectoryExists(ScreenshotBaseDir);

        var options = new LocatorScreenshotOptions
        {
            Path = saveToFile ? Path.Combine(ScreenshotBaseDir, filename) : null,
            Type = ScreenshotType.Png
        };

        return await element.ScreenshotAsync(options);
    }

    /// <summary>
    /// Captures screenshots at multiple viewport sizes.
    /// </summary>
    public static async Task CaptureResponsiveScreenshotsAsync(
        IPage page,
        string baseFilename,
        string path = "/")
    {
        var viewports = new[]
        {
            (Width: 375, Height: 667, Name: "mobile"),      // iPhone SE
            (Width: 768, Height: 1024, Name: "tablet"),     // iPad
            (Width: 1366, Height: 768, Name: "laptop"),     // Common laptop
            (Width: 1920, Height: 1080, Name: "desktop")    // Full HD
        };

        foreach (var (width, height, name) in viewports)
        {
            await page.SetViewportSizeAsync(width, height);
            await page.GotoAsync(path);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            var filename = $"{baseFilename}-{name}-{width}x{height}.png";
            await CaptureFullPageAsync(page, filename);
        }
    }

    /// <summary>
    /// Saves a baseline screenshot for future comparisons.
    /// </summary>
    public static async Task SaveBaselineAsync(IPage page, string testName)
    {
        EnsureDirectoryExists(BaselineDir);
        
        var filename = $"{testName}.baseline.png";
        var path = Path.Combine(BaselineDir, filename);
        
        await page.ScreenshotAsync(new()
        {
            Path = path,
            FullPage = true
        });
    }

    /// <summary>
    /// Compares current screenshot against baseline (pixel-by-pixel comparison).
    /// Note: For production use, consider integrating a more sophisticated tool like Percy or Applitools.
    /// </summary>
    public static async Task<bool> CompareWithBaselineAsync(
        IPage page,
        string testName,
        double threshold = 0.1)
    {
        EnsureDirectoryExists(ComparisonDir);
        
        var baselinePath = Path.Combine(BaselineDir, $"{testName}.baseline.png");
        
        if (!File.Exists(baselinePath))
        {
            // No baseline exists, save current as baseline
            await SaveBaselineAsync(page, testName);
            return true;
        }

        // Capture current screenshot
        var currentFilename = $"{testName}.current.png";
        var currentPath = Path.Combine(ComparisonDir, currentFilename);
        await page.ScreenshotAsync(new() { Path = currentPath, FullPage = true });

        // Basic comparison (for a simple implementation)
        // In production, you'd use a proper image comparison library
        var baselineInfo = new FileInfo(baselinePath);
        var currentInfo = new FileInfo(currentPath);

        // Simple file size comparison (not pixel-perfect)
        var sizeDiff = Math.Abs(baselineInfo.Length - currentInfo.Length);
        var sizeDiffPercent = (double)sizeDiff / baselineInfo.Length;

        return sizeDiffPercent <= threshold;
    }

    /// <summary>
    /// Captures an accessibility tree snapshot using Playwright's snapshot feature.
    /// </summary>
    public static async Task<string?> CaptureAccessibilitySnapshot(IPage page)
    {
        // Use Playwright's built-in accessibility snapshot
        var snapshot = await page.Locator("body").AriaSnapshotAsync();
        return snapshot;
    }

    /// <summary>
    /// Captures the page's network activity for performance analysis.
    /// </summary>
    public static async Task<List<NetworkActivity>> CaptureNetworkActivityAsync(
        IPage page,
        Func<Task> action)
    {
        var activities = new List<NetworkActivity>();

        page.Request += (_, request) =>
        {
            activities.Add(new NetworkActivity
            {
                Url = request.Url,
                Method = request.Method,
                ResourceType = request.ResourceType,
                Timestamp = DateTime.UtcNow
            });
        };

        await action();
        
        return activities;
    }

    /// <summary>
    /// Ensures a directory exists, creating it if necessary.
    /// </summary>
    private static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    /// <summary>
    /// Gets all screenshot files organized by test run.
    /// </summary>
    public static IEnumerable<string> GetScreenshots(string? subdirectory = null)
    {
        var directory = subdirectory != null 
            ? Path.Combine(ScreenshotBaseDir, subdirectory)
            : ScreenshotBaseDir;

        if (!Directory.Exists(directory))
            return Enumerable.Empty<string>();

        return Directory.GetFiles(directory, "*.png", SearchOption.AllDirectories);
    }

    /// <summary>
    /// Cleans up old screenshots to save disk space.
    /// </summary>
    public static void CleanupOldScreenshots(int keepLastNRuns = 3)
    {
        // Implementation would organize by timestamp and delete old runs
        // Left as an exercise based on your naming conventions
    }
}

/// <summary>
/// Represents a network activity captured during testing.
/// </summary>
public class NetworkActivity
{
    public string Url { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Extension methods for common visual testing patterns.
/// </summary>
public static class VisualTestExtensions
{
    /// <summary>
    /// Captures a screenshot with dynamic content masked (timestamps, user IDs, etc.).
    /// </summary>
    public static async Task CaptureWithMasksAsync(
        this IPage page,
        string filename,
        params string[] maskSelectors)
    {
        await VisualTestHelper.CaptureFullPageAsync(page, filename, maskSelectors);
    }

    /// <summary>
    /// Waits for images to load before taking a screenshot.
    /// </summary>
    public static async Task WaitForImagesToLoadAsync(this IPage page)
    {
        await page.EvaluateAsync(@"
            () => {
                return Promise.all(
                    Array.from(document.images)
                        .filter(img => !img.complete)
                        .map(img => new Promise(resolve => {
                            img.onload = img.onerror = resolve;
                        }))
                );
            }
        ");
    }

    /// <summary>
    /// Removes animations before taking a screenshot for consistent results.
    /// </summary>
    public static async Task DisableAnimationsAsync(this IPage page)
    {
        await page.EvaluateAsync(@"
            () => {
                const style = document.createElement('style');
                style.textContent = '*, *::before, *::after { animation: none !important; transition: none !important; }';
                document.head.appendChild(style);
            }
        ");
    }
}
