# BlazorBook Comprehensive E2E Testing Guide

## 🎯 Overview

This testing framework provides a complete automated solution for testing the BlazorBook.Web application. It eliminates the need for tedious manual testing by automating:

- ✅ **Page UI verification** - Automatically checks that each page renders correctly
- ✅ **Functionality testing** - Tests core features like posting, liking, commenting
- ✅ **Visual regression testing** - Captures screenshots for visual comparison
- ✅ **Cross-page navigation** - Verifies all routes work correctly
- ✅ **Responsive design testing** - Tests layouts on mobile, tablet, and desktop
- ✅ **Comprehensive reporting** - Generates beautiful HTML reports with embedded screenshots

## 🚀 Quick Start

### Option 1: Run Tests Against Running Server (Fastest)

```powershell
# Terminal 1: Start the BlazorBook.Web application
cd src/BlazorBook.Web
dotnet run --urls http://localhost:5555

# Terminal 2: Run comprehensive tests
cd tests
.\Run-ComprehensiveTests.ps1
```

### Option 2: Auto-Start Server (Most Convenient)

```powershell
cd tests
.\Run-ComprehensiveTests.ps1 -StartServer
```

The script will:
1. ✅ Clean previous test results
2. ✅ Start the server (if `-StartServer` flag is used)
3. ✅ Run all E2E tests
4. ✅ Capture screenshots of every page
5. ✅ Generate beautiful HTML report
6. ✅ Automatically open the report in your browser

## 📋 Prerequisites

1. **.NET 8.0 SDK** - Required for building and running the app
2. **Playwright** - Automatically installed when building the test project
3. **PowerShell** - Comes with Windows (scripts use PowerShell 5+)

### First-Time Setup

```powershell
# Build the test project (this will install Playwright)
cd tests/BlazorBook.E2E
dotnet build

# Install Playwright browsers
pwsh bin/Debug/net8.0/playwright.ps1 install
```

## 📖 Usage Examples

### Run All Tests

```powershell
.\Run-ComprehensiveTests.ps1
```

### Run Specific Test Categories

```powershell
# Run only UI tests
.\Run-ComprehensiveTests.ps1 -Category "UI"

# Run only functionality tests
.\Run-ComprehensiveTests.ps1 -Category "Functionality"

# Run only navigation tests
.\Run-ComprehensiveTests.ps1 -Category "Navigation"
```

### Debug Mode (See Browser)

```powershell
# Run with visible browser windows (useful for debugging)
.\Run-ComprehensiveTests.ps1 -HeadlessMode:$false
```

### Custom Server URL

```powershell
.\Run-ComprehensiveTests.ps1 -ServerUrl "http://localhost:8080"
```

### Skip Report Generation

```powershell
.\Run-ComprehensiveTests.ps1 -GenerateReport $false -OpenReport $false
```

## 🧪 Test Coverage

The comprehensive test suite covers:

### Authentication Pages
- ✅ Login page layout and functionality
- ✅ Sign-up page layout and functionality
- ✅ Home page (unauthenticated state)

### Core Application Pages
- ✅ **Feed** - Post creation, interaction buttons, post display
- ✅ **Profile** - User information display, tabs
- ✅ **Messages** - Messaging interface
- ✅ **Conversation** - Individual conversation view
- ✅ **Friends** - Friends list
- ✅ **Notifications** - Notification center
- ✅ **Search** - Search functionality

### Navigation Tests
- ✅ All routes are accessible
- ✅ Sidebar visible on all pages
- ✅ Page transitions work correctly

### Responsive Design Tests
- ✅ Desktop layout (1920x1080)
- ✅ Laptop layout (1366x768)
- ✅ Tablet layout (768x1024)
- ✅ Mobile layout (375x667)

### Error Handling
- ✅ 404 page for invalid routes

## 📸 Screenshot Organization

Screenshots are automatically organized in `tests/BlazorBook.E2E/screenshots/`:

```
screenshots/
├── home-unauthenticated.png          # Login screen
├── login-page.png                    # Login page
├── signup-page.png                   # Sign-up page
├── feed-page.png                     # Main feed
├── feed-with-post.png                # Feed with created post
├── profile-page.png                  # User profile
├── messages-page.png                 # Messages inbox
├── friends-page.png                  # Friends list
├── notifications-page.png            # Notifications
├── search-page.png                   # Search page
├── layout-desktop.png                # Desktop layout
├── layout-tablet.png                 # Tablet layout
├── layout-mobile.png                 # Mobile layout
└── page-not-found.png                # 404 page
```

## 📊 Test Reports

After each test run, an HTML report is generated in `tests/BlazorBook.E2E/TestResults/`.

### Report Features:
- 📈 **Pass/Fail Statistics** - Visual dashboard with metrics
- 📋 **Detailed Test Results** - Each test with duration and status
- 🖼️ **Embedded Screenshots** - All screenshots embedded in the report
- 🔍 **Full-Size Image Preview** - Click any screenshot to view full size
- 🎨 **Beautiful Design** - Professional, easy-to-read layout

### Example Report

```
test-results-2026-02-09_14-30-00.html
```

The report opens automatically unless you specify `-OpenReport $false`.

## 🔧 Advanced Usage

### Visual Regression Testing

The framework includes helpers for visual regression testing:

```csharp
using BlazorBook.E2E.Infrastructure;

[Test]
public async Task ProfilePage_VisualRegression()
{
    await NavigateToAsync("/profile");
    
    // Capture with dynamic content masked
    await VisualTestHelper.CaptureWithMasksAsync(
        Page,
        "profile-baseline.png",
        ".timestamp", ".notification-badge", ".avatar"
    );
    
    // Compare with baseline
    var matches = await VisualTestHelper.CompareWithBaselineAsync(
        Page,
        "profile-baseline",
        threshold: 0.05
    );
    
    Assert.That(matches, Is.True, "Profile page has visual changes");
}
```

### Capture Responsive Screenshots

```csharp
[Test]
public async Task CaptureResponsiveLayouts()
{
    await VisualTestHelper.CaptureResponsiveScreenshotsAsync(
        Page,
        "feed-responsive",
        "/feed"
    );
}
```

### Performance Testing

```csharp
[Test]
public async Task MeasurePageLoadPerformance()
{
    var activities = await VisualTestHelper.CaptureNetworkActivityAsync(
        Page,
        async () => await NavigateToAsync("/feed")
    );
    
    var totalRequests = activities.Count;
    var slowRequests = activities.Where(a => 
        a.Timestamp.Subtract(activities.First().Timestamp).TotalMilliseconds > 1000
    );
    
    Assert.That(slowRequests, Is.Empty, "Page loaded with slow requests");
}
```

## 🎨 Custom Test Creation

### Creating a New Test

1. Create a test class in `tests/BlazorBook.E2E/Tests/`:

```csharp
using BlazorBook.E2E.Infrastructure;
using Microsoft.Playwright;

namespace BlazorBook.E2E.Tests;

[TestFixture]
public class MyFeatureTests : BlazorBookPageTest
{
    [SetUp]
    public async Task SetUp()
    {
        var uniqueId = GenerateUniqueId();
        await SignUpAsync($"Test User {uniqueId}", 
                         $"user{uniqueId}", 
                         $"user{uniqueId}@example.com", 
                         "password123");
    }

    [Test]
    [Category("MyFeature")]
    public async Task MyFeature_WorksCorrectly()
    {
        await NavigateToAsync("/my-page");
        
        // Take screenshot
        await Page.ScreenshotAsync(new() 
        { 
            Path = "screenshots/my-feature.png", 
            FullPage = true 
        });
        
        // Verify functionality
        await Expect(Page.GetByText("Expected Text")).ToBeVisibleAsync();
    }
}
```

2. Run your tests:

```powershell
.\Run-ComprehensiveTests.ps1 -Category "MyFeature"
```

## 🐛 Debugging Tests

### Run in Headed Mode

```powershell
.\Run-ComprehensiveTests.ps1 -HeadlessMode:$false
```

### Run Single Test via IDE

You can also run individual tests directly from Visual Studio or VS Code:

1. Open the test file
2. Click "Run Test" on the test method
3. View results in Test Explorer

### Playwright Inspector

For step-by-step debugging:

```powershell
$env:PWDEBUG=1
dotnet test tests/BlazorBook.E2E
```

## 📝 Test Categories

Tests are organized by category:

| Category          | Description                              |
|-------------------|------------------------------------------|
| `UI`              | User interface layout tests              |
| `Functionality`   | Feature behavior tests                   |
| `Navigation`      | Page navigation and routing tests        |
| `Authentication`  | Login, signup, logout tests              |
| `Feed`            | Feed and post-related tests              |
| `Profile`         | User profile tests                       |
| `Messages`        | Messaging system tests                   |
| `Friends`         | Friend management tests                  |
| `Notifications`   | Notification system tests                |
| `Search`          | Search functionality tests               |
| `Responsive`      | Responsive design tests                  |

## 🔄 CI/CD Integration

### GitHub Actions Example

```yaml
name: E2E Tests

on: [push, pull_request]

jobs:
  e2e-tests:
    runs-on: windows-latest
    
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Build
        run: dotnet build
      
      - name: Install Playwright
        run: |
          cd tests/BlazorBook.E2E
          dotnet build
          pwsh bin/Debug/net8.0/playwright.ps1 install --with-deps
      
      - name: Run E2E Tests
        run: |
          cd tests
          .\Run-ComprehensiveTests.ps1 -StartServer
      
      - name: Upload Test Results
        if: always()
        uses: actions/upload-artifact@v3
        with:
          name: test-results
          path: tests/BlazorBook.E2E/TestResults/
      
      - name: Upload Screenshots
        if: always()
        uses: actions/upload-artifact@v3
        with:
          name: screenshots
          path: tests/BlazorBook.E2E/screenshots/
```

## 📚 Additional Resources

- [Playwright for .NET Documentation](https://playwright.dev/dotnet/)
- [NUnit Documentation](https://docs.nunit.org/)
- [Best Practices for E2E Testing](https://playwright.dev/dotnet/docs/best-practices)

## 🤝 Contributing

When adding new pages or features:

1. Add corresponding tests in `ComprehensivePageTests.cs` or create a new test file
2. Use appropriate test categories
3. Capture screenshots for visual verification
4. Update this documentation

## 💡 Tips

### Speed Up Tests
```powershell
# Run tests in parallel
.\Run-ComprehensiveTests.ps1 -ParallelWorkers 8
```

### Only Capture Failed Tests
```powershell
# Modify playwright.runsettings to only capture screenshots on failure
```

### Test Data Isolation
Each test creates a unique user to avoid data conflicts. Use `GenerateUniqueId()` for test data.

### Clean Test Environment

```powershell
# Remove old screenshots and results
Remove-Item tests/BlazorBook.E2E/screenshots/* -Recurse -Force
Remove-Item tests/BlazorBook.E2E/TestResults/* -Recurse -Force
```

## 🆘 Troubleshooting

### "Server not accessible"
- Make sure BlazorBook.Web is running on the expected port (default: 5555)
- Check firewall settings
- Use `-StartServer` flag to auto-start

### "Playwright browsers not installed"
```powershell
cd tests/BlazorBook.E2E
pwsh bin/Debug/net8.0/playwright.ps1 install
```

### Tests are flaky
- Increase wait times in `BlazorBookPageTest.WaitForBlazorAsync()`
- Use `Page.WaitForLoadStateAsync(LoadState.NetworkIdle)`
- Disable animations with `VisualTestExtensions.DisableAnimationsAsync()`

### Screenshots are blank
- Ensure pages are fully loaded before capturing
- Use `await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded)`
- Wait for images: `await Page.WaitForImagesToLoadAsync()`

---

**Questions?** Check existing tests for examples or consult the [Playwright documentation](https://playwright.dev/dotnet/).
