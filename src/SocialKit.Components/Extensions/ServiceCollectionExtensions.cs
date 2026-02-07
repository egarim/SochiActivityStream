using Microsoft.Extensions.DependencyInjection;
using Sochi.Navigation.Extensions;
using SocialKit.Components.Abstractions;
using SocialKit.Components.Services;
using SocialKit.Components.ViewModels;

namespace SocialKit.Components.Extensions;

/// <summary>
/// Extension methods for registering SocialKit services and ViewModels.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all SocialKit ViewModels and shared services.
    /// Each app must still register:
    /// - ICurrentUserService (platform-specific auth)
    /// - All domain services and stores (IContentService, IChatService, etc.)
    /// </summary>
    public static IServiceCollection AddSocialKit(this IServiceCollection services)
    {
        // Shared application services
        services.AddScoped<IFeedService, FeedService>();
        
        // ViewModels
        services.AddViewModel<HomeViewModel>();
        services.AddViewModel<LoginViewModel>();
        services.AddViewModel<SignUpViewModel>();
        services.AddViewModel<FeedViewModel>();
        services.AddViewModel<ProfileViewModel>();
        services.AddViewModel<MessagesViewModel>();
        services.AddViewModel<ConversationViewModel>();
        services.AddViewModel<FriendsViewModel>();
        services.AddViewModel<NotificationsViewModel>();
        services.AddViewModel<SearchViewModel>();
        
        return services;
    }
}
