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
    private readonly IMediaUploadService? _mediaUploadService;
    
    private ObservableCollection<PostDto> _posts = new();
    private string _newPostText = string.Empty;
    private bool _isLoading;
    private List<string> _pendingMediaIds = new();
    
    // Comment-related state
    private readonly Dictionary<string, List<CommentDto>> _postComments = new();
    private readonly HashSet<string> _loadingComments = new();
    private readonly HashSet<string> _addingComments = new();
    
    // Reply-related state
    private readonly Dictionary<string, List<CommentDto>> _commentReplies = new();
    private readonly HashSet<string> _loadingReplies = new();
    private readonly HashSet<string> _addingReplies = new();
    private readonly HashSet<string> _expandedReplies = new();

    public FeedViewModel(
        INavigationService navigationService,
        IFeedService feedService,
        ICurrentUserService currentUser,
        IMediaUploadService? mediaUploadService = null)
    {
        _navigationService = navigationService;
        _feedService = feedService;
        _currentUser = currentUser;
        _mediaUploadService = mediaUploadService;
        
        Title = "News Feed";
        
        LoadPostsCommand = new AsyncDelegateCommand(LoadPostsAsync);
        CreatePostCommand = new AsyncDelegateCommand(CreatePostAsync, CanCreatePost);
        LikePostCommand = new AsyncDelegateCommand<PostDto>(LikePostAsync);
        RefreshCommand = new AsyncDelegateCommand(LoadPostsAsync, () => !IsLoading);
        LoadCommentsCommand = new AsyncDelegateCommand<PostDto>(LoadCommentsAsync);
        AddCommentCommand = new AsyncDelegateCommand<(PostDto Post, string Body)>(AddCommentAsync);
        DeleteCommentCommand = new AsyncDelegateCommand<(PostDto Post, CommentDto Comment)>(DeleteCommentAsync);
        LikeCommentCommand = new AsyncDelegateCommand<(PostDto Post, CommentDto Comment)>(LikeCommentAsync);
        AddReplyCommand = new AsyncDelegateCommand<(PostDto Post, CommentDto Comment, string Body)>(AddReplyAsync);
        LoadRepliesCommand = new AsyncDelegateCommand<(PostDto Post, CommentDto Comment)>(LoadRepliesAsync);
        
        RegisterCommand(CreatePostCommand);
        RegisterCommand(RefreshCommand);
    }
    
    public List<string> PendingMediaIds
    {
        get => _pendingMediaIds;
        set => SetProperty(ref _pendingMediaIds, value);
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

    public string? CurrentUserId => _currentUser.ProfileId;
    public string? CurrentUserName => _currentUser.DisplayName;
    public string? CurrentUserAvatar => _currentUser.AvatarUrl;

    public IAsyncCommand LoadPostsCommand { get; }
    public IAsyncCommand CreatePostCommand { get; }
    public IAsyncCommand LikePostCommand { get; }
    public IAsyncCommand RefreshCommand { get; }
    public IAsyncCommand LoadCommentsCommand { get; }
    public IAsyncCommand AddCommentCommand { get; }
    public IAsyncCommand DeleteCommentCommand { get; }
    public IAsyncCommand LikeCommentCommand { get; }
    public IAsyncCommand AddReplyCommand { get; }
    public IAsyncCommand LoadRepliesCommand { get; }
    
    // Comment helper methods
    public List<CommentDto> GetCommentsForPost(string postId) =>
        _postComments.TryGetValue(postId, out var comments) ? comments : new List<CommentDto>();
    
    public bool IsLoadingCommentsForPost(string postId) => _loadingComments.Contains(postId);
    public bool IsAddingCommentForPost(string postId) => _addingComments.Contains(postId);
    
    // Reply helper methods
    public List<CommentDto> GetRepliesForComment(string commentId) =>
        _commentReplies.TryGetValue(commentId, out var replies) ? replies : new List<CommentDto>();
    
    public bool IsLoadingRepliesForComment(string commentId) => _loadingReplies.Contains(commentId);
    public bool IsAddingReplyForComment(string commentId) => _addingReplies.Contains(commentId);
    public bool AreRepliesExpandedForComment(string commentId) => _expandedReplies.Contains(commentId);
    
    public void ToggleRepliesExpanded(string commentId)
    {
        if (_expandedReplies.Contains(commentId))
            _expandedReplies.Remove(commentId);
        else
            _expandedReplies.Add(commentId);
        OnPropertyChanged(nameof(Posts));
    }

    public async Task InitializeAsync(INavigationParameters parameters)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.ProfileId == null)
        {
            await _navigationService.NavigateAsync("/login");
            return;
        }

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
            _postComments.Clear();
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
            var mediaIds = _pendingMediaIds.Count > 0 ? _pendingMediaIds.ToList() : null;
            
            var post = await _feedService.CreatePostAsync(
                _currentUser.ProfileId,
                _currentUser.DisplayName,
                NewPostText,
                mediaIds);
            
            Posts.Insert(0, post);
            NewPostText = string.Empty;
            _pendingMediaIds.Clear();
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    /// <summary>
    /// Upload files and create a post with optional media.
    /// </summary>
    public async Task CreatePostWithMediaAsync(string text, List<(string Name, string ContentType, byte[] Data)> files)
    {
        if (_currentUser.ProfileId == null) return;
        
        IsBusy = true;
        try
        {
            List<string>? mediaIds = null;
            List<string>? mediaUrls = null;
            
            // Upload media files if available
            if (files.Count > 0 && _mediaUploadService != null)
            {
                mediaIds = new List<string>();
                mediaUrls = new List<string>();
                foreach (var file in files)
                {
                    var result = await _mediaUploadService.UploadAsync(file.Name, file.ContentType, file.Data);
                    mediaIds.Add(result.MediaId);
                    mediaUrls.Add(result.Url);
                }
            }
            
            var post = await _feedService.CreatePostAsync(
                _currentUser.ProfileId,
                _currentUser.DisplayName ?? "User",
                text,
                mediaIds);
            
            // Set media URLs on the post for immediate display
            if (mediaUrls != null && mediaUrls.Count > 0)
            {
                post.MediaUrls = mediaUrls;
            }
            
            Posts.Insert(0, post);
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
    
    private async Task LoadCommentsAsync(PostDto? post)
    {
        if (post?.Id == null) return;
        
        _loadingComments.Add(post.Id);
        OnPropertyChanged(nameof(Posts)); // Trigger UI update
        
        try
        {
            var comments = await _feedService.GetCommentsAsync(post.Id);
            _postComments[post.Id] = comments.ToList();
        }
        finally
        {
            _loadingComments.Remove(post.Id);
            OnPropertyChanged(nameof(Posts));
        }
    }
    
    private async Task AddCommentAsync((PostDto Post, string Body) args)
    {
        if (args.Post?.Id == null || _currentUser.ProfileId == null) return;
        
        var postId = args.Post.Id;
        _addingComments.Add(postId);
        OnPropertyChanged(nameof(Posts));
        
        try
        {
            var comment = await _feedService.CreateCommentAsync(
                postId,
                _currentUser.ProfileId,
                _currentUser.DisplayName ?? "User",
                args.Body);
            
            if (!_postComments.ContainsKey(postId))
            {
                _postComments[postId] = new List<CommentDto>();
            }
            _postComments[postId].Add(comment);
            
            // Update comment count on the post
            args.Post.CommentCount++;
            OnPropertyChanged(nameof(Posts));
        }
        finally
        {
            _addingComments.Remove(postId);
            OnPropertyChanged(nameof(Posts));
        }
    }
    
    private async Task DeleteCommentAsync((PostDto Post, CommentDto Comment) args)
    {
        if (args.Post?.Id == null || args.Comment?.Id == null || _currentUser.ProfileId == null) return;
        
        try
        {
            await _feedService.DeleteCommentAsync(args.Comment.Id, _currentUser.ProfileId);
            
            if (_postComments.TryGetValue(args.Post.Id, out var comments))
            {
                comments.RemoveAll(c => c.Id == args.Comment.Id);
            }
            
            // Update comment count on the post
            if (args.Post.CommentCount > 0)
            {
                args.Post.CommentCount--;
            }
            
            OnPropertyChanged(nameof(Posts));
        }
        catch
        {
            // Handle error silently for now
        }
    }
    
    private async Task LikeCommentAsync((PostDto Post, CommentDto Comment) args)
    {
        if (args.Comment?.Id == null || _currentUser.ProfileId == null) return;
        
        try
        {
            var isLiked = args.Comment.ViewerReaction == ReactionType.Like;
            
            if (isLiked)
            {
                await _feedService.UnlikeCommentAsync(args.Comment.Id, _currentUser.ProfileId, _currentUser.DisplayName ?? "User");
                args.Comment.ViewerReaction = null;
                if (args.Comment.ReactionCounts.ContainsKey(ReactionType.Like))
                {
                    args.Comment.ReactionCounts[ReactionType.Like]--;
                }
            }
            else
            {
                await _feedService.LikeCommentAsync(args.Comment.Id, _currentUser.ProfileId, _currentUser.DisplayName ?? "User");
                args.Comment.ViewerReaction = ReactionType.Like;
                if (!args.Comment.ReactionCounts.ContainsKey(ReactionType.Like))
                {
                    args.Comment.ReactionCounts[ReactionType.Like] = 0;
                }
                args.Comment.ReactionCounts[ReactionType.Like]++;
            }
            
            OnPropertyChanged(nameof(Posts));
        }
        catch
        {
            // Handle error silently for now
        }
    }
    
    private async Task AddReplyAsync((PostDto Post, CommentDto Comment, string Body) args)
    {
        if (args.Post?.Id == null || args.Comment?.Id == null || _currentUser.ProfileId == null) return;
        
        var commentId = args.Comment.Id;
        _addingReplies.Add(commentId);
        OnPropertyChanged(nameof(Posts));
        
        try
        {
            var reply = await _feedService.CreateCommentAsync(
                args.Post.Id,
                _currentUser.ProfileId,
                _currentUser.DisplayName ?? "User",
                args.Body,
                args.Comment.Id);
            
            if (!_commentReplies.ContainsKey(commentId))
            {
                _commentReplies[commentId] = new List<CommentDto>();
            }
            _commentReplies[commentId].Add(reply);
            
            // Update reply count on the parent comment
            args.Comment.ReplyCount++;
            
            // Auto-expand replies after adding one
            _expandedReplies.Add(commentId);
            
            OnPropertyChanged(nameof(Posts));
        }
        finally
        {
            _addingReplies.Remove(commentId);
            OnPropertyChanged(nameof(Posts));
        }
    }
    
    private async Task LoadRepliesAsync((PostDto Post, CommentDto Comment) args)
    {
        if (args.Post?.Id == null || args.Comment?.Id == null) return;
        
        var commentId = args.Comment.Id;
        _loadingReplies.Add(commentId);
        _expandedReplies.Add(commentId);
        OnPropertyChanged(nameof(Posts));
        
        try
        {
            var replies = await _feedService.GetRepliesAsync(args.Post.Id, commentId);
            _commentReplies[commentId] = replies.ToList();
        }
        finally
        {
            _loadingReplies.Remove(commentId);
            OnPropertyChanged(nameof(Posts));
        }
    }
}
