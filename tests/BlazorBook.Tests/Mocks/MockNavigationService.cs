using Sochi.Navigation.Navigation;

namespace BlazorBook.Tests.Mocks;

/// <summary>
/// Mock navigation service for testing ViewModels.
/// Tracks all navigation calls for verification.
/// </summary>
public class MockNavigationService : INavigationService
{
    private readonly List<string> _navigationHistory = new();
    private int _backCount;
    private string _currentRoute = "/";
    
    /// <summary>
    /// All URIs that were navigated to.
    /// </summary>
    public IReadOnlyList<string> NavigationHistory => _navigationHistory;
    
    /// <summary>
    /// Number of times GoBackAsync was called.
    /// </summary>
    public int BackCount => _backCount;
    
    /// <summary>
    /// The last URI navigated to.
    /// </summary>
    public string? LastNavigatedUri => _navigationHistory.LastOrDefault();
    
    /// <summary>
    /// Gets the current route.
    /// </summary>
    public string CurrentRoute => _currentRoute;
    
    /// <summary>
    /// Gets whether back navigation is possible.
    /// </summary>
    public bool CanGoBack => _navigationHistory.Count > 1;
    
    public Task NavigateAsync(string uri)
    {
        _navigationHistory.Add(uri);
        _currentRoute = uri;
        return Task.CompletedTask;
    }
    
    public Task NavigateAsync(string uri, INavigationParameters parameters)
    {
        _navigationHistory.Add(uri);
        _currentRoute = uri;
        return Task.CompletedTask;
    }
    
    public Task<NavigationResult> NavigateAsync(string uri, INavigationParameters parameters, bool forceLoad)
    {
        _navigationHistory.Add(uri);
        _currentRoute = uri;
        return Task.FromResult(new NavigationResult { Success = true });
    }
    
    public Task GoBackAsync()
    {
        _backCount++;
        if (_navigationHistory.Count > 1)
        {
            _navigationHistory.RemoveAt(_navigationHistory.Count - 1);
            _currentRoute = _navigationHistory.LastOrDefault() ?? "/";
        }
        return Task.CompletedTask;
    }
    
    public void Clear()
    {
        _navigationHistory.Clear();
        _backCount = 0;
        _currentRoute = "/";
    }
}
