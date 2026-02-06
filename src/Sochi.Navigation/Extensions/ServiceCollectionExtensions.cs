using Microsoft.Extensions.DependencyInjection;
using Sochi.Navigation.Dialogs;
using Sochi.Navigation.Navigation;

namespace Sochi.Navigation.Extensions;

/// <summary>
/// Extension methods for registering Sochi.Navigation services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Sochi.Navigation services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSochiNavigation(this IServiceCollection services)
    {
        services.AddScoped<INavigationService, NavigationService>();
        services.AddScoped<DialogService>();
        services.AddScoped<IDialogService>(sp => sp.GetRequiredService<DialogService>());

        return services;
    }

    /// <summary>
    /// Registers a ViewModel with scoped lifetime.
    /// </summary>
    /// <typeparam name="TViewModel">The ViewModel type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddViewModel<TViewModel>(this IServiceCollection services)
        where TViewModel : class
    {
        services.AddScoped<TViewModel>();
        return services;
    }

    /// <summary>
    /// Registers multiple ViewModels with scoped lifetime.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="viewModelTypes">The ViewModel types to register.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddViewModels(
        this IServiceCollection services,
        params Type[] viewModelTypes)
    {
        foreach (var type in viewModelTypes)
        {
            services.AddScoped(type);
        }
        return services;
    }
}
