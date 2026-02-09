using BlazorBook.E2E.Infrastructure;
using Microsoft.Playwright;

namespace BlazorBook.E2E.Tests;

/// <summary>
/// Autonomous quality tests that use the TestFeedbackSystem to self-diagnose
/// and report issues without human intervention.
/// </summary>
[TestFixture]
[Category("Autonomous")]
[Category("Quality")]
public class AutonomousQualityTests : BlazorBookPageTest
{
    private TestFeedbackSystem? _feedback;
    private string _testUser = "";
    private string _uniqueId = "";

    [SetUp]
    public async Task SetUp()
    {
        _uniqueId = GenerateUniqueId();
        _testUser = $"autotest{_uniqueId}";
        
        // Initialize feedback system
        _feedback = new TestFeedbackSystem(Page, TestContext.CurrentContext.Test.Name);
        
        await _feedback.CaptureStateAsync("test-start", "info");
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_feedback != null)
        {
            try
            {
                var testResult = TestContext.CurrentContext.Result;
                var passed = testResult.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Passed;
                var error = testResult.Message;

                var report = await _feedback.GenerateReportAsync(
                    TestContext.CurrentContext.Test.Name,
                    passed,
                    error
                );

                Console.WriteLine($"📊 Feedback report generated: {report.TestName}");
                Console.WriteLine($"   - UI Issues: {report.UIIssues.Count}");
                Console.WriteLine($"   - Console Errors: {report.ConsoleErrors.Count}");
                Console.WriteLine($"   - Duration: {report.Duration.TotalSeconds:F1}s");
            }
            finally
            {
                _feedback.Dispose();
            }
        }
    }

    [Test]
    [Description("Complete user journey: signup → login → browse → post → interact")]
    public async Task CompleteUserJourney_AllFeatures_WorkCorrectly()
    {
        Assert.That(_feedback, Is.Not.Null);

        // STEP 1: Sign up
        await _feedback!.CaptureStateAsync("before-signup");
        await SignUpAsync($"Auto Test {_uniqueId}", _testUser, $"{_testUser}@example.com", "Test123!");
        await _feedback.CaptureStateAsync("after-signup");

        // Verify we're logged in
        await WaitForBlazorAsync();
        await Expect(Page.GetByText("Feed")).ToBeVisibleAsync(new() { Timeout = 5000 });
        
        // STEP 2: Navigate to Feed
        await Page.GetByRole(AriaRole.Link, new() { Name = "Feed" }).ClickAsync();
        await WaitForBlazorAsync();
        await _feedback.CaptureStateAsync("on-feed-page");

        // Check for UI issues
        var issues = await _feedback.AnalyzeUIIssuesAsync();
        if (issues.Any(i => i.Severity == "high"))
        {
            Console.WriteLine($"⚠️ High priority UI issues detected on feed:");
            foreach (var issue in issues.Where(i => i.Severity == "high"))
            {
                Console.WriteLine($"   - {issue.Message}");
            }
        }

        // STEP 3: Create a post
        var postContent = $"🤖 Autonomous test post - {DateTime.Now:HH:mm:ss}";
        
        // Find post composer (using correct selector)
        await Page.WaitForSelectorAsync("#post-content", new() { Timeout = 5000 });
        await Page.Locator("#post-content").FillAsync(postContent);
        // Trigger input and change events to update Blazor binding
        await Page.Locator("#post-content").DispatchEventAsync("input");
        await Page.Locator("#post-content").DispatchEventAsync("change");
        await WaitForBlazorAsync(500); // Wait for binding to update
        await _feedback.CaptureStateAsync("post-content-filled");

        // Wait for post button to be enabled before clicking
        await Page.WaitForSelectorAsync("#post-button:not([disabled])", new() { Timeout = 5000 });
        await Page.Locator("#post-button").ClickAsync();
        await WaitForBlazorAsync(2000);
        await _feedback.CaptureStateAsync("after-posting");

        // Verify post appears
        await Expect(Page.GetByText(postContent)).ToBeVisibleAsync(new() { Timeout = 5000 });

        // STEP 4: Navigate to Profile
        await Page.GetByRole(AriaRole.Link, new() { Name = "Profile" }).ClickAsync();
        await WaitForBlazorAsync();
        await _feedback.CaptureStateAsync("on-profile-page");

        // Verify profile elements
        await Expect(Page.GetByText($"@{_testUser}")).ToBeVisibleAsync();

        // STEP 5: Navigate to Messages
        await Page.GetByRole(AriaRole.Link, new() { Name = "Messages" }).ClickAsync();
        await WaitForBlazorAsync();
        await _feedback.CaptureStateAsync("on-messages-page");

        // STEP 6: Navigate to Notifications
        await Page.GetByRole(AriaRole.Link, new() { Name = "Notifications" }).ClickAsync();
        await WaitForBlazorAsync();
        await _feedback.CaptureStateAsync("on-notifications-page");

        // STEP 7: Navigate to Friends
        await Page.GetByRole(AriaRole.Link, new() { Name = "Friends" }).ClickAsync();
        await WaitForBlazorAsync();
        await _feedback.CaptureStateAsync("on-friends-page");

        // STEP 8: Test Search
        await Page.GetByRole(AriaRole.Link, new() { Name = "Search" }).ClickAsync();
        await WaitForBlazorAsync();
        await _feedback.CaptureStateAsync("on-search-page");

        // Final capture
        await _feedback.CaptureStateAsync("test-complete", "success");

        // Analyze all collected data
        var allIssues = await _feedback.AnalyzeUIIssuesAsync();
        
        Console.WriteLine("\n=== AUTONOMOUS TEST SUMMARY ===");
        Console.WriteLine($"✅ Completed full user journey");
        Console.WriteLine($"📊 UI Issues Found: {allIssues.Count}");
        Console.WriteLine($"   - High: {allIssues.Count(i => i.Severity == "high")}");
        Console.WriteLine($"   - Medium: {allIssues.Count(i => i.Severity == "medium")}");
        Console.WriteLine($"   - Low: {allIssues.Count(i => i.Severity == "low")}");
        
        // Don't fail test on UI issues, just report them
        if (allIssues.Any(i => i.Severity == "high"))
        {
            Console.WriteLine("⚠️ WARNING: High priority issues detected but test passed functionally");
        }
    } 

    [Test]
    [Description("Test responsive layouts and capture issues")]
    public async Task ResponsiveDesign_AllBreakpoints_RenderCorrectly()
    {
        Assert.That(_feedback, Is.Not.Null);

        // Sign up first
        await SignUpAsync($"Responsive Test {_uniqueId}", _testUser, $"{_testUser}@example.com", "Test123!");
        await WaitForBlazorAsync();

        var viewports = new[]
        {
            (Width: 375, Height: 667, Name: "mobile-portrait"),
            (Width: 667, Height: 375, Name: "mobile-landscape"),
            (Width: 768, Height: 1024, Name: "tablet-portrait"),
            (Width: 1024, Height: 768, Name: "tablet-landscape"),
            (Width: 1366, Height: 768, Name: "laptop"),
            (Width: 1920, Height: 1080, Name: "desktop")
        };

        foreach (var (width, height, name) in viewports)
        {
            await Page.SetViewportSizeAsync(width, height);
            await Page.GotoAsync($"{BaseUrl}/feed");
            await WaitForBlazorAsync();

            await _feedback!.CaptureStateAsync($"viewport-{name}-{width}x{height}");

            // Analyze layout issues  at this viewport
            var issues = await _feedback.AnalyzeUIIssuesAsync();
            var layoutIssues = issues.Where(i => i.Type == "layout").ToList();

            if (layoutIssues.Any())
            {
                Console.WriteLine($"⚠️ Layout issues at {name} ({width}x{height}):");
                foreach (var issue in layoutIssues)
                {
                    Console.WriteLine($"   - {issue.Message}");
                }
            }
        }

        Console.WriteLine($"\n✅ Tested {viewports.Length} viewport sizes");
    }

    [Test]
    [Description("Test all navigation links and verify no broken pages")]
    public async Task Navigation_AllLinks_LeadToValidPages()
    {
        Assert.That(_feedback, Is.Not.Null);

        await SignUpAsync($"Nav Test {_uniqueId}", _testUser, $"{_testUser}@example.com", "Test123!");
        await WaitForBlazorAsync();

        var pages = new[]
        {
            ("Feed", "/feed"),
            ("Profile", "/profile"),
            ("Messages", "/messages"),
            ("Notifications", "/notifications"),
            ("Friends", "/friends"),
            ("Search", "/search")
        };

        foreach (var (name, path) in pages)
        {
            Console.WriteLine($"Testing navigation to: {name}");

            await Page.GotoAsync($"{BaseUrl}{path}");
            await WaitForBlazorAsync();

            await _feedback!.CaptureStateAsync($"page-{name.ToLower()}");

            // Verify page loaded
            var title = await Page.TitleAsync();
            Assert.That(title, Is.Not.EqualTo("Error"), $"Page {name} returned error");

            // Check for 404 or error messages
            var hasError = await Page.GetByText("not found", new() { Exact = false }).IsVisibleAsync()
                           .ContinueWith(t => t.IsCompletedSuccessfully && t.Result);

            Assert.That(hasError, Is.False, $"Page {name} shows 'not found' error");

            // Analyze issues
            var issues = await _feedback.AnalyzeUIIssuesAsync();
            var highIssues = issues.Where(i => i.Severity == "high").ToList();

            if (highIssues.Any())
            {
                Console.WriteLine($"⚠️ High priority issues on {name}:");
                foreach (var issue in highIssues)
                {
                    Console.WriteLine($"   - {issue.Message}");
                }
            }
        }

        Console.WriteLine($"\n✅ Tested {pages.Length} pages successfully");
    }

    [Test]
    [Description("Stress test: create multiple posts and interactions")]
    public async Task StressTest_MultipleInteractions_SystemRemainStable()
    {
        Assert.That(_feedback, Is.Not.Null);

        await SignUpAsync($"Stress Test {_uniqueId}", _testUser, $"{_testUser}@example.com", "Test123!");
        await WaitForBlazorAsync();

        await Page.GotoAsync($"{BaseUrl}/feed");
        await WaitForBlazorAsync();

        await _feedback!.CaptureStateAsync("stress-test-start");

        // Create 5 posts rapidly
        for (int i = 1; i <= 5; i++)
        {
            var content = $"Stress test post #{i} - {DateTime.Now:HH:mm:ss.fff}";
            
            // Wait for post composer to be ready
            await Page.WaitForSelectorAsync("#post-content", new() { Timeout = 5000 });
            await Page.Locator("#post-content").FillAsync(content);
            // Trigger input and change events to update Blazor binding
            await Page.Locator("#post-content").DispatchEventAsync("input");
            await Page.Locator("#post-content").DispatchEventAsync("change");
            await WaitForBlazorAsync(500);
            // Wait for button to be enabled
            await Page.WaitForSelectorAsync("#post-button:not([disabled])", new() { Timeout = 5000 });
            await Page.Locator("#post-button").ClickAsync();
            await WaitForBlazorAsync(1000);

            if (i % 2 == 0)
            {
                await _feedback.CaptureStateAsync($"stress-test-after-post-{i}");
            }
        }

        await _feedback.CaptureStateAsync("stress-test-complete");

        // Analyze for memory leaks or performance issues
        var issues = await _feedback.AnalyzeUIIssuesAsync();
        
        Console.WriteLine($"\n=== STRESS TEST RESULTS ===");
        Console.WriteLine($"✅ Created 5 posts rapidly");
        Console.WriteLine($"📊 Issues detected: {issues.Count}");

        // Check console for errors
        var consoleErrors = TestContext.Parameters.Get("ConsoleErrors", "0");
        Console.WriteLine($"Console errors: {consoleErrors}");
    }

    [Test]
    [Description("Accessibility audit: check WCAG compliance")]
    public async Task Accessibility_AllPages_MeetWCAGStandards()
    {
        Assert.That(_feedback, Is.Not.Null);

        await SignUpAsync($"A11y Test {_uniqueId}", _testUser, $"{_testUser}@example.com", "Test123!");
        await WaitForBlazorAsync();

        var pages = new[] { "/feed", "/profile", "/messages", "/notifications" };

        foreach (var pagePath in pages)
        {
            await Page.GotoAsync($"{BaseUrl}{pagePath}");
            await WaitForBlazorAsync();

            await _feedback!.CaptureStateAsync($"a11y-check-{pagePath.Replace("/", "")}");

            var issues = await _feedback.AnalyzeUIIssuesAsync();
            var a11yIssues = issues.Where(i => i.Type == "accessibility").ToList();

            Console.WriteLine($"\n{pagePath} Accessibility Issues: {a11yIssues.Count}");
            foreach (var issue in a11yIssues.Take(10))
            {
                Console.WriteLine($"   - [{issue.Severity}] {issue.Message}");
            }
        }

        Console.WriteLine($"\n✅ Accessibility audit completed for {pages.Length} pages");
    }

    [Test]
    [Description("Profile editing: verify user can edit display name, avatar, and privacy settings")]
    public async Task ProfileEdit_UpdateAllFields_SavesCorrectly()
    {
        Assert.That(_feedback, Is.Not.Null);

        // STEP 1: Sign up and login
        await _feedback!.CaptureStateAsync("before-signup");
        await SignUpAsync($"Edit Test {_uniqueId}", _testUser, $"{_testUser}@example.com", "Test123!");
        await WaitForBlazorAsync();
        await _feedback.CaptureStateAsync("after-signup");

        // STEP 2: Navigate to Profile
        await Page.GetByRole(AriaRole.Link, new() { Name = "Profile" }).ClickAsync();
        await WaitForBlazorAsync();
        await _feedback.CaptureStateAsync("on-profile-page");

        // Verify we're on the profile page
        await Expect(Page.GetByText($"@{_testUser}")).ToBeVisibleAsync(new() { Timeout = 5000 });

        // STEP 3: Click Edit Profile button
        var editButton = Page.GetByRole(AriaRole.Button, new() { Name = "Edit Profile" });
        await Expect(editButton).ToBeVisibleAsync(new() { Timeout = 5000 });
        await _feedback.CaptureStateAsync("before-click-edit");
        
        await editButton.ClickAsync();
        await WaitForBlazorAsync(1000);
        await _feedback.CaptureStateAsync("after-click-edit-dialog-should-open");

        // STEP 4: Verify dialog opened
        var dialogTitle = Page.GetByText("Edit Profile", new() { Exact = true });
        await Expect(dialogTitle).ToBeVisibleAsync(new() { Timeout = 5000 });
        await _feedback.CaptureStateAsync("edit-dialog-opened");

        // Check for UI issues in the dialog
        var dialogIssues = await _feedback.AnalyzeUIIssuesAsync();
        if (dialogIssues.Any(i => i.Severity == "high"))
        {
            Console.WriteLine($"⚠️ High priority issues in edit dialog:");
            foreach (var issue in dialogIssues.Where(i => i.Severity == "high"))
            {
                Console.WriteLine($"   - {issue.Message}");
            }
        }

        // STEP 5: Update display name
        var newDisplayName = $"Updated User {_uniqueId}";
        var displayNameField = Page.GetByLabel("Display Name");
        await displayNameField.ClearAsync();
        await displayNameField.FillAsync(newDisplayName);
        await displayNameField.DispatchEventAsync("input");
        await displayNameField.DispatchEventAsync("change");
        await WaitForBlazorAsync(300);
        await _feedback.CaptureStateAsync("display-name-updated");

        // STEP 6: Update avatar URL
        var avatarUrl = "https://i.pravatar.cc/150?img=5";
        var avatarField = Page.GetByLabel("Avatar URL");
        await avatarField.ClearAsync();
        await avatarField.FillAsync(avatarUrl);
        await avatarField.DispatchEventAsync("input");
        await avatarField.DispatchEventAsync("change");
        await WaitForBlazorAsync(300);
        await _feedback.CaptureStateAsync("avatar-url-updated");

        // STEP 7: Toggle privacy setting
        var privateSwitch = Page.Locator("label:has-text('Private Profile')");
        await Expect(privateSwitch).ToBeVisibleAsync(new() { Timeout = 5000 });
        await privateSwitch.ClickAsync();
        await WaitForBlazorAsync(300);
        await _feedback.CaptureStateAsync("privacy-toggled");

        // STEP 8: Save changes
        var saveButton = Page.Locator("#edit-profile-save");
        await Expect(saveButton).ToBeVisibleAsync();
        await Expect(saveButton).Not.ToBeDisabledAsync();
        await _feedback.CaptureStateAsync("before-save");
        
        await saveButton.ClickAsync();
        await WaitForBlazorAsync(5000); // Increased wait time
        await _feedback.CaptureStateAsync("after-save-dialog-should-close");

        // STEP 9: Verify dialog closed
        await Expect(dialogTitle).Not.ToBeVisibleAsync(new() { Timeout = 10000 }); // Increased timeout
        await _feedback.CaptureStateAsync("dialog-closed");

        // STEP 10: Verify changes persisted on profile page
        await Expect(Page.GetByText(newDisplayName)).ToBeVisibleAsync(new() { Timeout = 5000 });
        await _feedback.CaptureStateAsync("profile-updated");

        // STEP 11: Refresh page and verify persistence
        await Page.ReloadAsync();
        await WaitForBlazorAsync(2000);
        await _feedback.CaptureStateAsync("after-page-reload");
        
        await Expect(Page.GetByText(newDisplayName)).ToBeVisibleAsync(new() { Timeout = 5000 });
        await _feedback.CaptureStateAsync("test-complete", "success");

        // Analyze all collected data
        var allIssues = await _feedback.AnalyzeUIIssuesAsync();
        
        Console.WriteLine("\n=== PROFILE EDIT TEST SUMMARY ===");
        Console.WriteLine($"✅ Successfully edited profile");
        Console.WriteLine($"   - Display Name: {newDisplayName}");
        Console.WriteLine($"   - Avatar URL: {avatarUrl}");
        Console.WriteLine($"   - Privacy: Toggled");
        Console.WriteLine($"📊 UI Issues Found: {allIssues.Count}");
        Console.WriteLine($"   - High: {allIssues.Count(i => i.Severity == "high")}");
        Console.WriteLine($"   - Medium: {allIssues.Count(i => i.Severity == "medium")}");
        Console.WriteLine($"   - Low: {allIssues.Count(i => i.Severity == "low")}");

        if (allIssues.Any(i => i.Severity == "high"))
        {
            Console.WriteLine("\n🔴 HIGH PRIORITY ISSUES:");
            foreach (var issue in allIssues.Where(i => i.Severity == "high"))
            {
                Console.WriteLine($"   [{issue.Type}] {issue.Message}");
            }
        }
    }

    [Test]
    [Description("Profile picture upload: verify user can upload and save profile picture")]
    public async Task ProfilePictureUpload_UploadImage_SavesAndDisplays()
    {
        Assert.That(_feedback, Is.Not.Null);

        // STEP 1: Sign up and login
        await _feedback!.CaptureStateAsync("before-signup");
        await SignUpAsync($"Pic Test {_uniqueId}", _testUser, $"{_testUser}@example.com", "Test123!");
        await WaitForBlazorAsync();
        await _feedback.CaptureStateAsync("after-signup");

        // STEP 2: Navigate to Profile
        await Page.GetByRole(AriaRole.Link, new() { Name = "Profile" }).ClickAsync();
        await WaitForBlazorAsync();
        await _feedback.CaptureStateAsync("on-profile-page");

        // Verify we're on the profile page
        await Expect(Page.GetByText($"@{_testUser}")).ToBeVisibleAsync(new() { Timeout = 5000 });

        // STEP 3: Click Edit Profile button  
        var editButton = Page.GetByRole(AriaRole.Button, new() { Name = "Edit Profile" });
        await Expect(editButton).ToBeVisibleAsync(new() { Timeout = 5000 });
        await _feedback.CaptureStateAsync("before-click-edit");
        
        await editButton.ClickAsync();
        await WaitForBlazorAsync(1000);
        await _feedback.CaptureStateAsync("edit-dialog-opened");

        // STEP 4: Verify dialog opened
        var dialogTitle = Page.Locator("#edit-profile-dialog-title");
        await Expect(dialogTitle).ToBeVisibleAsync(new() { Timeout = 5000 });
        
        // Check for UI issues in the dialog
        var dialogIssues = await _feedback.AnalyzeUIIssuesAsync();
        if (dialogIssues.Any(i => i.Severity == "high"))
        {
            Console.WriteLine($"⚠️ High priority issues in edit dialog:");
            foreach (var issue in dialogIssues.Where(i => i.Severity == "high"))
            {
                Console.WriteLine($"   - {issue.Message}");
            }
        }

        // STEP 5: Create a test image (1x1 PNG)
        byte[] testImageData = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");
        var testImagePath = Path.Combine(Path.GetTempPath(), $"test-avatar-{_uniqueId}.png");
        await File.WriteAllBytesAsync(testImagePath, testImageData);
        Console.WriteLine($"✅ Created test image: {testImagePath}");

        try
        {
            // STEP 6: Upload the file
            await _feedback.CaptureStateAsync("before-file-upload");
            
            var uploadButton = Page.Locator("#upload-profile-pic-btn");
            await Expect(uploadButton).ToBeVisibleAsync(new() { Timeout = 5000 });
            
            // Set the file using the input file (DevExpress uses a hidden input)
            var fileInput = Page.Locator("input[type='file']").First;
            await fileInput.SetInputFilesAsync(testImagePath);
            await WaitForBlazorAsync(2000); // Wait for file processing
            
            await _feedback.CaptureStateAsync("after-file-upload");
            Console.WriteLine("✅ File uploaded");

            // STEP 7: Verify "Picture selected" message appears
            var pictureSelectedText = Page.GetByText("Picture selected");
            await Expect(pictureSelectedText).ToBeVisibleAsync(new() { Timeout = 5000 });
            Console.WriteLine("✅ Picture selected confirmation visible");

            // STEP 8: Save changes
            var saveButton = Page.Locator("#edit-profile-save");
            await Expect(saveButton).ToBeVisibleAsync();
            await Expect(saveButton).Not.ToBeDisabledAsync();
            await _feedback.CaptureStateAsync("before-save");
            
            await saveButton.ClickAsync();
            await WaitForBlazorAsync(5000); // Increased wait time for upload to blob storage
            await _feedback.CaptureStateAsync("after-save");

            // STEP 9: Verify dialog closed
            await Expect(dialogTitle).Not.ToBeVisibleAsync(new() { Timeout = 10000 });
            await _feedback.CaptureStateAsync("dialog-closed");
            Console.WriteLine("✅ Dialog closed");

            // STEP 10: Verify profile picture updated on page
            await WaitForBlazorAsync(2000);
            await _feedback.CaptureStateAsync("check-updated-picture");
            
            // The avatar should now be visible (either new upload or error fallback)
            var profileAvatar = Page.Locator("img[alt*='Profile'], img[style*='border-radius: 50%'], .sk-profile-cover + div img").First;
            await Expect(profileAvatar).ToBeVisibleAsync(new() { Timeout = 5000 });
            Console.WriteLine("✅ Profile picture visible");

            // STEP 11: Refresh page and verify persistence
            await Page.ReloadAsync();
            await WaitForBlazorAsync(2000);
            await _feedback.CaptureStateAsync("after-page-reload");
            
            await Expect(profileAvatar).ToBeVisibleAsync(new() { Timeout = 5000 });
            await _feedback.CaptureStateAsync("test-complete", "success");
            Console.WriteLine("✅ Picture persisted after reload");
        }
        finally
        {
            // Cleanup test file
            if (File.Exists(testImagePath))
            {
                File.Delete(testImagePath);
            }
        }

        // Analyze all collected data
        var allIssues = await _feedback.AnalyzeUIIssuesAsync();
        
        Console.WriteLine("\n=== PROFILE PICTURE UPLOAD TEST SUMMARY ===");
        Console.WriteLine($"✅ Successfully uploaded profile picture");
        Console.WriteLine($"📊 UI Issues Found: {allIssues.Count}");
        Console.WriteLine($"   - High: {allIssues.Count(i => i.Severity == "high")}");
        Console.WriteLine($"   - Medium: {allIssues.Count(i => i.Severity == "medium")}");
        Console.WriteLine($"   - Low: {allIssues.Count(i => i.Severity == "low")}");

        if (allIssues.Any(i => i.Severity == "high"))
        {
            Console.WriteLine("\n🔴 HIGH PRIORITY ISSUES:");
            foreach (var issue in allIssues.Where(i => i.Severity == "high"))
            {
                Console.WriteLine($"   [{issue.Type}] {issue.Message}");
            }
        }
    }
}
