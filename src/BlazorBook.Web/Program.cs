using ActivityStream.Abstractions;
using ActivityStream.Core;
using ActivityStream.Store.InMemory;
using Chat.Abstractions;
using Chat.Core;
using Chat.Store.InMemory;
using Content.Abstractions;
using Content.Core;
using Content.Store.InMemory;
using DevExpress.Blazor;
using Identity.Abstractions;
using Identity.Core;
using Identity.Store.InMemory;
using Media.Abstractions;
using Media.Core;
using Media.Store.InMemory;
using RelationshipService.Abstractions;
using RelationshipService.Core;
using RelationshipService.Store.InMemory;
using Search.Abstractions;
using Search.Core;
using Search.Index.InMemory;

using Sochi.Navigation.Extensions;

using SocialKit.Components.Abstractions;
using SocialKit.Components.Extensions;

using BlazorBook.Web;
using BlazorBook.Web.Components;
using BlazorBook.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════════════════════════
// BLAZOR SERVICES
// ═══════════════════════════════════════════════════════════════════════════════
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// DevExpress Blazor
builder.Services.AddDevExpressBlazor();

// ═══════════════════════════════════════════════════════════════════════════════
// SOCHI.NAVIGATION (MVVM)
// ═══════════════════════════════════════════════════════════════════════════════
builder.Services.AddSochiNavigation();

// ═══════════════════════════════════════════════════════════════════════════════
// IDENTITY SERVICE
// ═══════════════════════════════════════════════════════════════════════════════
builder.Services.AddSingleton<IUserStore, InMemoryUserStore>();
builder.Services.AddSingleton<IProfileStore, InMemoryProfileStore>();
builder.Services.AddSingleton<IMembershipStore, InMemoryMembershipStore>();
builder.Services.AddSingleton<ISessionStore, InMemorySessionStore>();
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddSingleton<Identity.Abstractions.IIdGenerator, Identity.Core.UlidIdGenerator>();
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<IProfileService, ProfileService>();

// ═══════════════════════════════════════════════════════════════════════════════
// CONTENT SERVICE (Posts, Comments, Reactions)
// ═══════════════════════════════════════════════════════════════════════════════
builder.Services.AddSingleton<IPostStore, InMemoryPostStore>();
builder.Services.AddSingleton<ICommentStore, InMemoryCommentStore>();
builder.Services.AddSingleton<IReactionStore, InMemoryReactionStore>();
builder.Services.AddSingleton<Content.Abstractions.IIdGenerator, Content.Core.UlidIdGenerator>();
builder.Services.AddSingleton<IContentService, ContentService>();

// ═══════════════════════════════════════════════════════════════════════════════
// CHAT SERVICE
// ═══════════════════════════════════════════════════════════════════════════════
builder.Services.AddSingleton<IConversationStore, InMemoryConversationStore>();
builder.Services.AddSingleton<IMessageStore, InMemoryMessageStore>();
builder.Services.AddSingleton<IChatNotifier, NullChatNotifier>();
builder.Services.AddSingleton<Chat.Abstractions.IIdGenerator, Chat.Core.UlidIdGenerator>();
builder.Services.AddSingleton<IChatService, ChatService>();

// ═══════════════════════════════════════════════════════════════════════════════
// MEDIA SERVICE
// ═══════════════════════════════════════════════════════════════════════════════
builder.Services.AddSingleton<IMediaStore, InMemoryMediaStore>();
builder.Services.AddSingleton<IMediaStorageProvider, MockStorageProvider>();
builder.Services.AddSingleton<ActivityStream.Abstractions.IIdGenerator, ActivityStream.Core.UlidIdGenerator>();
builder.Services.AddSingleton<IMediaService, MediaService>();

// ═══════════════════════════════════════════════════════════════════════════════
// ACTIVITY STREAM SERVICE
// ═══════════════════════════════════════════════════════════════════════════════
builder.Services.AddSingleton<IActivityStore, InMemoryActivityStore>();
builder.Services.AddSingleton<IActivityValidator, DefaultActivityValidator>();
builder.Services.AddSingleton<IActivityStreamService, ActivityStreamService>();

// ═══════════════════════════════════════════════════════════════════════════════
// RELATIONSHIP SERVICE
// ═══════════════════════════════════════════════════════════════════════════════
builder.Services.AddSingleton<IRelationshipStore, InMemoryRelationshipStore>();
builder.Services.AddSingleton<IRelationshipService, RelationshipServiceImpl>();

// Note: Inbox and Realtime services are not used in this demo.
// They require additional configuration (governance policies, etc.)
// and are omitted for simplicity.

// ═══════════════════════════════════════════════════════════════════════════════
// SEARCH SERVICE
// ═══════════════════════════════════════════════════════════════════════════════
builder.Services.AddSingleton<ITextAnalyzer, SimpleTextAnalyzer>();
builder.Services.AddSingleton<ISearchIndex, InMemorySearchIndex>();
// Note: SearchValidator is a static class, no DI registration needed

// ═══════════════════════════════════════════════════════════════════════════════
// APPLICATION SERVICES
// ═══════════════════════════════════════════════════════════════════════════════
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// ═══════════════════════════════════════════════════════════════════════════════
// SOCIALKIT (FeedService, ViewModels, etc.)
// ═══════════════════════════════════════════════════════════════════════════════
builder.Services.AddSocialKit();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// ═══════════════════════════════════════════════════════════════════════════════
// MOCK STORAGE PROVIDER
// ═══════════════════════════════════════════════════════════════════════════════
public class MockStorageProvider : IMediaStorageProvider
{
    public Task<string> GenerateUploadUrlAsync(string blobPath, string contentType, long maxSizeBytes, TimeSpan expiry, CancellationToken ct = default)
        => Task.FromResult($"https://blazorbook.local/upload/{blobPath}");
    
    public Task<string> GenerateDownloadUrlAsync(string blobPath, TimeSpan expiry, CancellationToken ct = default)
        => Task.FromResult($"https://blazorbook.local/download/{blobPath}");
    
    public Task<bool> ExistsAsync(string blobPath, CancellationToken ct = default)
        => Task.FromResult(true);
    
    public Task<BlobProperties?> GetPropertiesAsync(string blobPath, CancellationToken ct = default)
        => Task.FromResult<BlobProperties?>(new BlobProperties { SizeBytes = 1024, ContentType = "image/png" });
    
    public Task DeleteAsync(string blobPath, CancellationToken ct = default)
        => Task.CompletedTask;
    
    public Task CopyAsync(string sourcePath, string destPath, CancellationToken ct = default)
        => Task.CompletedTask;
}
