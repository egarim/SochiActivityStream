using Microsoft.AspNetCore.Components;

namespace Sochi.Navigation.Navigation;

/// <summary>
/// Implementation of <see cref="INavigationService"/> using Blazor's <see cref="NavigationManager"/>.
/// </summary>
public sealed class NavigationService : INavigationService
{
    private readonly NavigationManager _navigationManager;
    private readonly Stack<string> _navigationStack = new();
    private object? _currentViewModel;

    /// <summary>
    /// Initializes a new instance of <see cref="NavigationService"/>.
    /// </summary>
    /// <param name="navigationManager">The Blazor navigation manager.</param>
    public NavigationService(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));
    }

    /// <inheritdoc />
    public string CurrentRoute => _navigationManager.Uri;

    /// <inheritdoc />
    public bool CanGoBack => _navigationStack.Count > 0;

    /// <inheritdoc />
    public Task NavigateAsync(string uri) => NavigateAsync(uri, new NavigationParameters());

    /// <inheritdoc />
    public Task NavigateAsync(string uri, INavigationParameters parameters) =>
        NavigateAsync(uri, parameters, false);

    /// <inheritdoc />
    public async Task<NavigationResult> NavigateAsync(
        string uri,
        INavigationParameters parameters,
        bool forceLoad)
    {
        try
        {
            // Handle OnNavigatedFrom for current ViewModel
            if (_currentViewModel is INavigationAware currentNavAware)
            {
                currentNavAware.OnNavigatedFrom(parameters);
            }

            // Check if navigation can proceed
            if (_currentViewModel is IConfirmNavigation confirmNavigation)
            {
                var canNavigate = await confirmNavigation.CanNavigateAsync(parameters);
                if (!canNavigate)
                {
                    return NavigationResult.CreateFailure(
                        new NavigationException("Navigation was cancelled by the ViewModel."));
                }
            }

            // Build URI with query string
            var fullUri = uri;
            if (parameters.Count > 0 && parameters is NavigationParameters navParams)
            {
                fullUri += navParams.ToQueryString();
            }

            // Store current URI for back navigation
            var currentUri = _navigationManager.Uri;
            if (!string.IsNullOrEmpty(currentUri))
            {
                _navigationStack.Push(currentUri);
            }

            // Perform navigation
            _navigationManager.NavigateTo(fullUri, forceLoad);

            return NavigationResult.CreateSuccess();
        }
        catch (Exception ex)
        {
            return NavigationResult.CreateFailure(ex);
        }
    }

    /// <inheritdoc />
    public Task GoBackAsync()
    {
        if (_navigationStack.Count > 0)
        {
            var previousUri = _navigationStack.Pop();
            _navigationManager.NavigateTo(previousUri);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Sets the current ViewModel. Called by <c>MvvmComponentBase</c>.
    /// </summary>
    /// <param name="viewModel">The current ViewModel.</param>
    internal void SetCurrentViewModel(object? viewModel)
    {
        _currentViewModel = viewModel;
    }
}
