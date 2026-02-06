using System.Collections.ObjectModel;
using Content.Abstractions;
using Sochi.Navigation.Commands;
using Sochi.Navigation.Mvvm;
using Sochi.Navigation.Navigation;
using SocialKit.Components.Abstractions;

namespace SocialKit.Components.ViewModels;

/// <summary>
/// ViewModel for the main feed page.
/// </summary>
public class FeedViewModel : ViewModelBase, IInitialize
{
    private readonly INavigationService _navigationService;
    private readonly IFeedService _feedService;
    private readonly ICurrentUserService _currentUser;
    
    private ObservableCollection<PostDto> _posts = new();
    private string _newPostText = string.Empty;
    private bool _isLoading;

    public FeedViewModel(
        INavigationService navigationService,
        IFeedService feedService,
        ICurrentUserService currentUser)
    {
        _navigationService = navigationService;
        _feedService = feedService;
        _currentUser = currentUser;
        
        Title = "News Feed";
        
        LoadPostsCommand = new AsyncDelegateCommand(LoadPostsAsync);
        CreatePostCommand = new AsyncDelegateCommand(CreatePostAsync, CanCreatePost);
        LikePostCommand = new AsyncDelegateCommand<PostDto>(LikePostAsync);
        RefreshCommand = new AsyncDelegateCommand(LoadPostsAsync, () => !IsLoading);
        
        RegisterCommand(CreatePostCommand);
        RegisterCommand(RefreshCommand);
    }

    public ObservableCollection<PostDto> Posts
    {
        get => _posts;
        set => SetProperty(ref _posts, value);
    }

    public string NewPostText
    {
        get => _newPostText;
        set
        {
            if (SetProperty(ref _newPostText, value))
            {
                RaiseCanExecuteChangedForAllCommands();
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
            {
                RaiseCanExecuteChangedForAllCommands();
            }
        }
    }

    public string? CurrentUserName => _currentUser.DisplayName;
    public string? CurrentUserAvatar => _currentUser.AvatarUrl;

    public IAsyncCommand LoadPostsCommand { get; }
    public IAsyncCommand CreatePostCommand { get; }
    public IAsyncCommand LikePostCommand { get; }
    public IAsyncCommand RefreshCommand { get; }

    public async Task InitializeAsync(INavigationParameters parameters)
    {
        await LoadPostsAsync();
    }

    private bool CanCreatePost() => 
        !IsBusy && 
        !string.IsNullOrWhiteSpace(NewPostText) &&
        _currentUser.IsAuthenticated;

    private async Task LoadPostsAsync()
    {
        if (_currentUser.ProfileId == null) return;
        
        IsLoading = true;
        try
        {
            var posts = await _feedService.GetFeedAsync(_currentUser.ProfileId);
            Posts = new ObservableCollection<PostDto>(posts);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CreatePostAsync()
    {
        if (_currentUser.ProfileId == null || _currentUser.DisplayName == null) return;
        
        IsBusy = true;
        try
        {
            var post = await _feedService.CreatePostAsync(
                _currentUser.ProfileId,
                _currentUser.DisplayName,
                NewPostText);
            
            Posts.Insert(0, post);
            NewPostText = string.Empty;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LikePostAsync(PostDto? post)
    {
        if (post == null || _currentUser.ProfileId == null) return;
        
        try
        {
            var isLiked = post.ViewerReaction == ReactionType.Like;
            
            if (isLiked)
            {
                await _feedService.UnlikePostAsync(post.Id!, _currentUser.ProfileId, _currentUser.DisplayName ?? "User");
                post.ViewerReaction = null;
                if (post.ReactionCounts.ContainsKey(ReactionType.Like))
                {
                    post.ReactionCounts[ReactionType.Like]--;
                }
            }
            else
            {
                await _feedService.LikePostAsync(post.Id!, _currentUser.ProfileId, _currentUser.DisplayName ?? "User");
                post.ViewerReaction = ReactionType.Like;
                if (!post.ReactionCounts.ContainsKey(ReactionType.Like))
                {
                    post.ReactionCounts[ReactionType.Like] = 0;
                }
                post.ReactionCounts[ReactionType.Like]++;
            }
            
            OnPropertyChanged(nameof(Posts));
        }
        catch
        {
            // Handle error silently for now
        }
    }
}
