using System.Collections.ObjectModel;
using Sochi.Navigation.Commands;
using Sochi.Navigation.Mvvm;
using Sochi.Navigation.Navigation;
using SocialKit.Components.Abstractions;
using Search.Abstractions;
using Microsoft.Extensions.Logging;

namespace SocialKit.Components.ViewModels;

/// <summary>
/// ViewModel for the global search page.
/// </summary>
public class SearchViewModel : ViewModelBase, IInitialize
{
    private readonly INavigationService _navigationService;
    private readonly ICurrentUserService _currentUser;
    private readonly ISearchService _searchService;
    private readonly ILogger<SearchViewModel> _logger;

    private ObservableCollection<SearchResultItem> _results = new();
    private string _query = string.Empty;
    private bool _isLoading;
    private string? _nextCursor;
    private long _totalCount;

    public SearchViewModel(
        INavigationService navigationService,
        ICurrentUserService currentUser,
        ISearchService searchService,
        ILogger<SearchViewModel> logger)
    {
        _navigationService = navigationService;
        _currentUser = currentUser;
        _searchService = searchService;
        _logger = logger;

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

    public long TotalCount
    {
        get => _totalCount;
        set => SetProperty(ref _totalCount, value);
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

        _logger.LogInformation("Starting search for query: '{Query}'", Query);
        
        IsLoading = true;
        IsBusy = true;
        try
        {
            var request = new SearchRequest
            {
                TenantId = "blazorbook",
                Query = Query,
                Limit = 20,
                IncludeSource = true, // Include source entities for profile/post data
                Highlight = true
            };

            _logger.LogDebug("Search request: {@Request}", request);

            var result = await _searchService.SearchAsync(request);
            
            _logger.LogInformation("Search completed. Found {HitCount} results out of {TotalCount} total", 
                result.Hits.Count, result.TotalCount);
            
            var items = new ObservableCollection<SearchResultItem>();
            foreach (var hit in result.Hits)
            {
                _logger.LogDebug("Search hit: {DocumentType} - {Id} (Score: {Score})", 
                    hit.DocumentType, hit.Id, hit.Score);
                
                _logger.LogDebug("Hit Source: {HasSource}, SourceType: {SourceType}", 
                    hit.Source != null, hit.Source?.GetType().Name ?? "null");
                    
                var title = GetTitle(hit);
                _logger.LogDebug("Extracted title: '{Title}' from hit {Id}", title, hit.Id);
                    
                items.Add(new SearchResultItem
                {
                    Id = hit.Id,
                    Type = hit.DocumentType,
                    Title = title,
                    Description = GetDescription(hit),
                    Route = GetRoute(hit),
                    Score = hit.Score
                });
            }

            Results = items;
            TotalCount = result.TotalCount;
            _nextCursor = result.NextCursor;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search FAILED for query: '{Query}'", Query);
            Results = new ObservableCollection<SearchResultItem>();
            TotalCount = 0;
        }
        finally
        {
            IsLoading = false;
            IsBusy = false;
        }
    }

    private string GetTitle(SearchHit hit)
    {
        // Try to get title from source entity first
        if (hit.Source != null)
        {
            // If source is already a JsonElement, use it directly
            if (hit.Source is System.Text.Json.JsonElement jsonElement)
            {
                _logger.LogDebug("JsonElement properties: {Props}", 
                    string.Join(", ", jsonElement.EnumerateObject().Select(p => p.Name)));
                
                if (jsonElement.TryGetProperty("displayName", out var displayName) && displayName.ValueKind != System.Text.Json.JsonValueKind.Null)
                {
                    _logger.LogDebug("Found displayName: {DisplayName}", displayName.GetString());
                    return displayName.GetString() ?? hit.Id;
                }
                if (jsonElement.TryGetProperty("handle", out var handle) && handle.ValueKind != System.Text.Json.JsonValueKind.Null)
                {
                    _logger.LogDebug("Found handle: {Handle}", handle.GetString());
                    return handle.GetString() ?? hit.Id;
                }
                if (jsonElement.TryGetProperty("body", out var body) && body.ValueKind != System.Text.Json.JsonValueKind.Null)
                {
                    _logger.LogDebug("Found body: {Body}", body.GetString());
                    return body.GetString() ?? hit.Id;
                }
                
                _logger.LogWarning("No displayName, handle, or body found in JsonElement");
            }
            else
            {
                // Otherwise serialize and parse
                var sourceJson = System.Text.Json.JsonSerializer.Serialize(hit.Source);
                var sourceDoc = System.Text.Json.JsonDocument.Parse(sourceJson);
                
                if (sourceDoc.RootElement.TryGetProperty("DisplayName", out var displayName) && displayName.ValueKind != System.Text.Json.JsonValueKind.Null)
                    return displayName.GetString() ?? hit.Id;
                if (sourceDoc.RootElement.TryGetProperty("Handle", out var handle) && handle.ValueKind != System.Text.Json.JsonValueKind.Null)
                    return handle.GetString() ?? hit.Id;
                if (sourceDoc.RootElement.TryGetProperty("Body", out var body) && body.ValueKind != System.Text.Json.JsonValueKind.Null)
                    return body.GetString() ?? hit.Id;
            }
        }
        
        // Fall back to highlights
        if (hit.Highlights?.ContainsKey("title") == true)
            return hit.Highlights["title"];
        if (hit.Highlights?.ContainsKey("displayName") == true)
            return hit.Highlights["displayName"];
        if (hit.Highlights?.ContainsKey("username") == true)
            return hit.Highlights["username"];
        return hit.Id;
    }

    private string GetDescription(SearchHit hit)
    {
        if (hit.Highlights?.ContainsKey("body") == true)
            return hit.Highlights["body"];
        if (hit.Highlights?.ContainsKey("bio") == true)
            return hit.Highlights["bio"];
        return string.Empty;
    }

    private string GetRoute(SearchHit hit) => hit.DocumentType.ToLowerInvariant() switch
    {
        "profile" => $"/profile/{hit.Id}",
        "post" => $"/", // Posts shown in feed
        _ => "/"
    };
}

/// <summary>
/// Search result item for display.
/// </summary>
public sealed class SearchResultItem
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public double Score { get; set; }
}
