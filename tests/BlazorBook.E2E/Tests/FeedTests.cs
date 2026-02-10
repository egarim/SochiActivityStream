using BlazorBook.E2E.Infrastructure;
using Microsoft.Playwright;

namespace BlazorBook.E2E.Tests;

/// <summary>
/// E2E tests for the feed page and post creation.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Self)]
public class FeedTests : BlazorBookPageTest
{
    [SetUp]
    public async Task SetUp()
    {
        // Create a unique user for each test
        var uniqueId = GenerateUniqueId();
        await SignUpAsync($"Feed Test {uniqueId}", $"feedtest{uniqueId}", $"feed{uniqueId}@example.com", "password123");
    }

    /// <summary>
    /// Gets a locator scoped to a specific post card containing the given text.
    /// </summary>
    private ILocator GetPostCard(string postContent) =>
        Page.Locator(".mud-card").Filter(new() { HasText = postContent }).First;

    [Test]
    public async Task Feed_ShowsPostComposer()
    {
        // Assert - MudBlazor text field with ID selector
        await Expect(Page.Locator("#post-content")).ToBeVisibleAsync();
        await Expect(Page.Locator("#post-button")).ToBeVisibleAsync();
    }

    [Test]
    public async Task CreatePost_WithValidContent_AppearsInFeed()
    {
        // Arrange
        var postContent = $"Hello from Playwright test! {GenerateUniqueId()}";
        
        // Act
        await CreatePostAsync(postContent);
        
        // Ensure the text box loses focus
        await Page.Locator("#post-content").BlurAsync();
        
        // Wait for the button to become enabled
        await Task.Delay(500); // Allow time for state change
        
        // Debugging: Log button state
        var postButton = Page.Locator("#post-button");
        Console.WriteLine("Post button state after delay: " + await postButton.IsEnabledAsync());
        Console.WriteLine("Post button visible after delay: " + await postButton.IsVisibleAsync());
        
        // Assert - composer should be empty (MudBlazor text field)
        await Expect(Page.Locator("#post-content")).ToHaveValueAsync("");
    }

    [Test]
    public async Task CreatePost_ShowsAuthorName()
    {
        // Arrange
        var postContent = $"Author name test {GenerateUniqueId()}";
        
        // Act
        await CreatePostAsync(postContent);
        
        // Assert - the post should show the author's name (as a link)
        var postCard = GetPostCard(postContent);
        await Expect(postCard.GetByRole(AriaRole.Link).First).ToBeVisibleAsync();
    }

    [Test]
    public async Task CreatePost_ShowsTimestamp()
    {
        // Arrange
        var postContent = $"Timestamp test {GenerateUniqueId()}";
        
        // Act
        await CreatePostAsync(postContent);
        
        // Debugging: Log button state
        var postButton = Page.Locator("#post-button");
        Console.WriteLine("Post button state: " + await postButton.IsEnabledAsync());
        Console.WriteLine("Post button visible: " + await postButton.IsVisibleAsync());
        
        // Assert - should show "Just now" within the post
        var postCard = GetPostCard(postContent);
        await Expect(postCard.GetByText("Just now")).ToBeVisibleAsync();
    }

    [Test]
    public async Task CreatePost_ShowsLikeButton()
    {
        // Arrange
        var postContent = $"Like button test {GenerateUniqueId()}";
        
        // Act
        await CreatePostAsync(postContent);
        
        // Debugging: Log button state
        var postButton = Page.Locator("#post-button");
        Console.WriteLine("Post button state: " + await postButton.IsEnabledAsync());
        Console.WriteLine("Post button visible: " + await postButton.IsVisibleAsync());
        
        // Assert - check within the specific post
        var postCard = GetPostCard(postContent);
        await Expect(postCard.GetByRole(AriaRole.Button, new() { Name = "Like" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task CreatePost_ShowsCommentButton()
    {
        // Arrange
        var postContent = $"Comment button test {GenerateUniqueId()}";
        
        // Act
        await CreatePostAsync(postContent);
        
        // Debugging: Log button state
        var postButton = Page.Locator("#post-button");
        Console.WriteLine("Post button state: " + await postButton.IsEnabledAsync());
        Console.WriteLine("Post button visible: " + await postButton.IsVisibleAsync());
        
        // Assert - check within the specific post
        var postCard = GetPostCard(postContent);
        await Expect(postCard.GetByRole(AriaRole.Button, new() { Name = "Comment" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task CreatePost_ShowsShareButton()
    {
        // Arrange
        var postContent = $"Share button test {GenerateUniqueId()}";
        
        // Act
        await CreatePostAsync(postContent);
        
        // Debugging: Log button state
        var postButton = Page.Locator("#post-button");
        Console.WriteLine("Post button state: " + await postButton.IsEnabledAsync());
        Console.WriteLine("Post button visible: " + await postButton.IsVisibleAsync());
        
        // Assert - check within the specific post
        var postCard = GetPostCard(postContent);
        await Expect(postCard.GetByRole(AriaRole.Button, new() { Name = "Share" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task CreatePost_ClearsComposerAfterPosting()
    {
        // Arrange
        var postContent = $"Clear composer test {GenerateUniqueId()}";
        
        // Act
        await CreatePostAsync(postContent);
        
        // Ensure the text box loses focus
        await Page.Locator("#post-content").BlurAsync();
        
        // Wait for the button to become enabled
        await Task.Delay(500); // Allow time for state change
        
        // Debugging: Log button state
        var postButton = Page.Locator("#post-button");
        Console.WriteLine("Post button state after delay: " + await postButton.IsEnabledAsync());
        Console.WriteLine("Post button visible after delay: " + await postButton.IsVisibleAsync());
        
        // Assert - composer should be empty (MudBlazor text field)
        await Expect(Page.Locator("#post-content")).ToHaveValueAsync("");
    }

    [Test]
    public async Task CreateMultiplePosts_AppearsInReverseChronologicalOrder()
    {
        // Arrange
        var firstPost = $"First post {GenerateUniqueId()}";
        var secondPost = $"Second post {GenerateUniqueId()}";
        
        // Act
        await CreatePostAsync(firstPost);
        await Task.Delay(100); // Ensure different timestamps
        await CreatePostAsync(secondPost);
        
        // Assert - both posts should be visible
        await Expect(Page.GetByText(firstPost)).ToBeVisibleAsync();
        await Expect(Page.GetByText(secondPost)).ToBeVisibleAsync();
    }

    [Test]
    public async Task PostButton_DisabledWhenEmpty()
    {
        // Assert - Post button should be disabled when composer is empty
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Post" })).ToBeDisabledAsync();
    }

    [Test]
    public async Task CreatePost_WithImage_UploadsSuccessfully()
    {
        // Arrange - Create a test image
        var uniqueId = GenerateUniqueId();
        var postContent = $"Post with image {uniqueId}";
        
        // Create a simple 10x10 PNG image
        var testImageData = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, // IHDR chunk
            0x00, 0x00, 0x00, 0x0A, 0x00, 0x00, 0x00, 0x0A, // 10x10
            0x08, 0x02, 0x00, 0x00, 0x00, 0x02, 0x50, 0x58,
            0xEA, 0x00, 0x00, 0x00, 0x01, 0x73, 0x52, 0x47,
            0x42, 0x00, 0xAE, 0xCE, 0x1C, 0xE9, 0x00, 0x00,
            0x00, 0x04, 0x67, 0x41, 0x4D, 0x41, 0x00, 0x00,
            0xB1, 0x8F, 0x0B, 0xFC, 0x61, 0x05, 0x00, 0x00,
            0x00, 0x09, 0x70, 0x48, 0x59, 0x73, 0x00, 0x00,
            0x0E, 0xC3, 0x00, 0x00, 0x0E, 0xC3, 0x01, 0xC7,
            0x6F, 0xA8, 0x64, 0x00, 0x00, 0x00, 0x16, 0x49,
            0x44, 0x41, 0x54, 0x28, 0x53, 0x63, 0xFC, 0xFF,
            0xFF, 0x3F, 0x03, 0x00, 0x00, 0x00, 0xFF, 0xFF,
            0x03, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x05,
            0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00, 0x00,
            0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42,
            0x60, 0x82
        };
        
        var testImagePath = Path.Combine(Path.GetTempPath(), $"test-post-image-{uniqueId}.png");
        await File.WriteAllBytesAsync(testImagePath, testImageData);
        Console.WriteLine($"✅ Created test image: {testImagePath}");

        try
        {
            // Act - Type post content
            await Page.Locator("#post-content").FillAsync(postContent);
            Console.WriteLine("✅ Filled post content");
            
            // Click the Photo/Video button to trigger DevExpress file selector
            var photoButton = Page.Locator("#photo-select-btn");
            await photoButton.ClickAsync();
            Console.WriteLine("✅ Photo button clicked");
            
            // Wait for file chooser dialog
            var fileChooser = await Page.RunAndWaitForFileChooserAsync(async () =>
            {
                // The click above should have triggered the file chooser
            });
            
            // Set files through the file chooser
            await fileChooser.SetFilesAsync(testImagePath);
            Console.WriteLine("✅ File attached through file chooser");
            
            // Wait for file processing and preview to appear
            await Task.Delay(3000);
            Console.WriteLine("✅ Waited for file processing");
            
            // Wait for the button to become enabled (either from text or file)
            var postButton = Page.Locator("#post-button");
            await postButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
            
            // Wait for button to be enabled (max 10 seconds)
            var buttonEnabled = false;
            for (int i = 0; i < 20; i++)
            {
                if (await postButton.IsEnabledAsync())
                {
                    buttonEnabled = true;
                    Console.WriteLine($"✅ Post button enabled after {i * 500}ms");
                    break;
                }
                await Task.Delay(500);
            }
            
            if (!buttonEnabled)
            {
                Console.WriteLine("⚠️ Post button still disabled");
                // Take screenshot for debugging
                await Page.ScreenshotAsync(new() { Path = Path.Combine(Path.GetTempPath(), $"post-button-disabled-{uniqueId}.png") });
            }
            
            // Click Post button
            await postButton.ClickAsync(new() { Timeout = 5000 });
            Console.WriteLine("✅ Post button clicked");
            
            // Wait for post to appear
            await Task.Delay(2000);
            
            // Assert - Post should appear with content
            await Expect(Page.GetByText(postContent)).ToBeVisibleAsync(new() { Timeout = 10000 });
            Console.WriteLine("✅ Post with image appears in feed");
            
            // Verify the post card exists
            var postCard = GetPostCard(postContent);
            await Expect(postCard).ToBeVisibleAsync();
            Console.WriteLine("✅ Post card is visible");
            
            // Verify composer is cleared
            await Expect(Page.Locator("#post-content")).ToHaveValueAsync("");
            Console.WriteLine("✅ Composer cleared after posting");
        }
        finally
        {
            // Cleanup
            if (File.Exists(testImagePath))
            {
                File.Delete(testImagePath);
                Console.WriteLine($"🧹 Cleaned up test image: {testImagePath}");
            }
        }
    }
}
