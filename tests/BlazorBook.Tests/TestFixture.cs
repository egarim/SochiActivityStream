using Microsoft.Extensions.DependencyInjection;

using ActivityStream.Abstractions;
using ActivityStream.Core;
using ActivityStream.Store.InMemory;
using Chat.Abstractions;
using Chat.Core;
using Chat.Store.InMemory;
using Content.Core;
using Content.Store.InMemory;
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
using Sochi.Navigation.Navigation;

using SocialKit.Components.Abstractions;
using SocialKit.Components.ViewModels;
using SocialKit.Components.Services;
using BlazorBook.Tests.Mocks;

namespace BlazorBook.Tests;

/// <summary>
/// Base fixture that configures DI with real in-memory services.
/// This allows testing ViewModels with actual service implementations.
/// </summary>
public class TestFixture : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;

    public IServiceProvider Services => _scope.ServiceProvider;
    public MockNavigationService Navigation { get; }
    public MockCurrentUserService CurrentUser { get; }

    public TestFixture()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        
        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();
        
        Navigation = (MockNavigationService)Services.GetRequiredService<INavigationService>();
        CurrentUser = (MockCurrentUserService)Services.GetRequiredService<ICurrentUserService>();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // ═══════════════════════════════════════════════════════════════════════
        // SOCHI.NAVIGATION (MVVM)
        // ═══════════════════════════════════════════════════════════════════════
        services.AddSochiNavigation();
        
        // Override navigation with our mock for test inspection
        services.AddSingleton<MockNavigationService>();
        services.AddSingleton<INavigationService>(sp => sp.GetRequiredService<MockNavigationService>());

        // ═══════════════════════════════════════════════════════════════════════
        // IDENTITY SERVICE (Real in-memory)
        // ═══════════════════════════════════════════════════════════════════════
        services.AddSingleton<IUserStore, InMemoryUserStore>();
        services.AddSingleton<IProfileStore, InMemoryProfileStore>();
        services.AddSingleton<IMembershipStore, InMemoryMembershipStore>();
        services.AddSingleton<ISessionStore, InMemorySessionStore>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<Identity.Abstractions.IIdGenerator, Identity.Core.UlidIdGenerator>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IProfileService, ProfileService>();

        // ═══════════════════════════════════════════════════════════════════════
        // CONTENT SERVICE (Real in-memory)
        // ═══════════════════════════════════════════════════════════════════════
        services.AddSingleton<IPostStore, InMemoryPostStore>();
        services.AddSingleton<ICommentStore, InMemoryCommentStore>();
        services.AddSingleton<IReactionStore, InMemoryReactionStore>();
        services.AddSingleton<ActivityStream.Abstractions.IIdGenerator, Content.Core.UlidIdGenerator>();
        services.AddSingleton<IContentService, ContentService>();

        // ═══════════════════════════════════════════════════════════════════════
        // CHAT SERVICE (Real in-memory)
        // ═══════════════════════════════════════════════════════════════════════
        services.AddSingleton<IConversationStore, InMemoryConversationStore>();
        services.AddSingleton<IMessageStore, InMemoryMessageStore>();
        services.AddSingleton<IChatNotifier, NullChatNotifier>();
        services.AddSingleton<Chat.Abstractions.IIdGenerator, Chat.Core.UlidIdGenerator>();
        services.AddSingleton<IChatService, ChatService>();

        // ═══════════════════════════════════════════════════════════════════════
        // MEDIA SERVICE (Real in-memory)
        // ═══════════════════════════════════════════════════════════════════════
        services.AddSingleton<IMediaStore, InMemoryMediaStore>();
        services.AddSingleton<IMediaStorageProvider, TestStorageProvider>();
        services.AddSingleton<ActivityStream.Abstractions.IIdGenerator, ActivityStream.Core.UlidIdGenerator>();
        services.AddSingleton<IMediaService, MediaService>();

        // ═══════════════════════════════════════════════════════════════════════
        // ACTIVITY STREAM SERVICE (Real in-memory)
        // ═══════════════════════════════════════════════════════════════════════
        services.AddSingleton<IActivityStore, InMemoryActivityStore>();
        services.AddSingleton<IActivityValidator, DefaultActivityValidator>();
        services.AddSingleton<IActivityStreamService, ActivityStreamService>();

        // ═══════════════════════════════════════════════════════════════════════
        // RELATIONSHIP SERVICE (Real in-memory)
        // ═══════════════════════════════════════════════════════════════════════
        services.AddSingleton<IRelationshipStore, InMemoryRelationshipStore>();
        services.AddSingleton<IRelationshipService, RelationshipServiceImpl>();

        // ═══════════════════════════════════════════════════════════════════════
        // SEARCH SERVICE (Real in-memory)
        // ═══════════════════════════════════════════════════════════════════════
        services.AddSingleton<ITextAnalyzer, SimpleTextAnalyzer>();
        services.AddSingleton<ISearchIndex, InMemorySearchIndex>();

        // ═══════════════════════════════════════════════════════════════════════
        // APPLICATION SERVICES
        // ═══════════════════════════════════════════════════════════════════════
        services.AddSingleton<MockCurrentUserService>();
        services.AddSingleton<ICurrentUserService>(sp => sp.GetRequiredService<MockCurrentUserService>());
        services.AddScoped<IFeedService, FeedService>();

        // ═══════════════════════════════════════════════════════════════════════
        // VIEWMODELS
        // ═══════════════════════════════════════════════════════════════════════
        services.AddTransient<HomeViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<SignUpViewModel>();
        services.AddTransient<FeedViewModel>();
        services.AddTransient<ProfileViewModel>();
        services.AddTransient<MessagesViewModel>();
        services.AddTransient<ConversationViewModel>();
        services.AddTransient<FriendsViewModel>();
    }

    public T GetViewModel<T>() where T : class => Services.GetRequiredService<T>();
    
    public T GetService<T>() where T : class => Services.GetRequiredService<T>();

    public void Dispose()
    {
        _scope.Dispose();
        _serviceProvider.Dispose();
    }
}

/// <summary>
/// Simple storage provider for tests.
/// </summary>
public class TestStorageProvider : IMediaStorageProvider
{
    public Task<string> GenerateUploadUrlAsync(string blobPath, string contentType, long maxSizeBytes, TimeSpan expiry, CancellationToken ct = default)
        => Task.FromResult($"https://test.local/upload/{blobPath}");
    
    public Task<string> GenerateDownloadUrlAsync(string blobPath, TimeSpan expiry, CancellationToken ct = default)
        => Task.FromResult($"https://test.local/download/{blobPath}");
    
    public Task<bool> ExistsAsync(string blobPath, CancellationToken ct = default)
        => Task.FromResult(true);
    
    public Task<BlobProperties?> GetPropertiesAsync(string blobPath, CancellationToken ct = default)
        => Task.FromResult<BlobProperties?>(new BlobProperties { SizeBytes = 1024, ContentType = "image/png" });
    
    public Task DeleteAsync(string blobPath, CancellationToken ct = default)
        => Task.CompletedTask;
    
    public Task CopyAsync(string sourcePath, string destPath, CancellationToken ct = default)
        => Task.CompletedTask;
    
    public Task UploadBytesAsync(string blobPath, byte[] data, string contentType, CancellationToken ct = default)
        => Task.CompletedTask;
}
