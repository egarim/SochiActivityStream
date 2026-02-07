using System.Collections.ObjectModel;
using Sochi.Navigation.Commands;
using Sochi.Navigation.Mvvm;
using Sochi.Navigation.Navigation;
using SocialKit.Components.Abstractions;

namespace SocialKit.Components.ViewModels;

/// <summary>
/// ViewModel for the global search page.
/// Currently provides query state management; result hydration will be wired once the search service is finalized.
/// </summary>
public class SearchViewModel : ViewModelBase, IInitialize
{
    private readonly INavigationService _navigationService;
    private readonly ICurrentUserService _currentUser;

    private ObservableCollection<SearchResultItem> _results = new();
    private string _query = string.Empty;
    private bool _isLoading;

    public SearchViewModel(
        INavigationService navigationService,
        ICurrentUserService currentUser)
    {
        _navigationService = navigationService;
        _currentUser = currentUser;

        Title = "Search";

        PerformSearchCommand = new AsyncDelegateCommand(PerformSearchAsync, CanSearch);
        RegisterCommand(PerformSearchCommand);
    }

    public ObservableCollection<SearchResultItem> Results
    {
        get => _results;
        set => SetProperty(ref _results, value);
    }

    public string Query
    {
        get => _query;
        set
        {
            if (SetProperty(ref _query, value))
            {
                RaiseCanExecuteChangedForAllCommands();
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public IAsyncCommand PerformSearchCommand { get; }

    public async Task InitializeAsync(INavigationParameters parameters)
    {
        if (!_currentUser.IsAuthenticated)
        {
            await _navigationService.NavigateAsync("/login");
            return;
        }

        if (parameters.TryGetValue<string>("searchQuery", out var query) && !string.IsNullOrWhiteSpace(query))
        {
            Query = query;
        }
    }

    private bool CanSearch() => !IsBusy && !string.IsNullOrWhiteSpace(Query);

    private async Task PerformSearchAsync()
    {
        if (string.IsNullOrWhiteSpace(Query))
        {
            return;
        }

        IsBusy = true;
        try
        {
            var parameters = new NavigationParameters();
            parameters.Add("searchQuery", Query);
            await _navigationService.NavigateAsync("/search", parameters);
        }
        finally
        {
            IsBusy = false;
        }
    }
}

/// <summary>
/// Temporary search result placeholder until the search service integration lands.
/// </summary>
public sealed class SearchResultItem
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
}
