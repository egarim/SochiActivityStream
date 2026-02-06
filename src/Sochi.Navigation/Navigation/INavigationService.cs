namespace Sochi.Navigation.Navigation;

/// <summary>
/// Service for handling navigation between pages.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Gets the current route.
    /// </summary>
    string CurrentRoute { get; }

    /// <summary>
    /// Gets a value indicating whether the navigation service can go back.
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// Navigates to the specified URI.
    /// </summary>
    /// <param name="uri">The target URI.</param>
    Task NavigateAsync(string uri);

    /// <summary>
    /// Navigates to the specified URI with parameters.
    /// </summary>
    /// <param name="uri">The target URI.</param>
    /// <param name="parameters">The navigation parameters.</param>
    Task NavigateAsync(string uri, INavigationParameters parameters);

    /// <summary>
    /// Navigates to the specified URI with parameters and options.
    /// </summary>
    /// <param name="uri">The target URI.</param>
    /// <param name="parameters">The navigation parameters.</param>
    /// <param name="forceLoad">If true, forces a full page reload.</param>
    /// <returns>The navigation result.</returns>
    Task<NavigationResult> NavigateAsync(string uri, INavigationParameters parameters, bool forceLoad);

    /// <summary>
    /// Navigates back to the previous page.
    /// </summary>
    Task GoBackAsync();
}
