using System.Collections.ObjectModel;
using Identity.Abstractions;
using RelationshipService.Abstractions;
using Sochi.Navigation.Commands;
using Sochi.Navigation.Mvvm;
using Sochi.Navigation.Navigation;
using SocialKit.Components.Abstractions;

namespace SocialKit.Components.ViewModels;

/// <summary>
/// ViewModel for the friends list page.
/// </summary>
public class FriendsViewModel : ViewModelBase, IInitialize
{
    private readonly INavigationService _navigationService;
    private readonly IRelationshipService _relationshipService;
    private readonly IProfileService _profileService;
    private readonly ICurrentUserService _currentUser;
    
    private ObservableCollection<FriendInfo> _friends = new();
    private ObservableCollection<FriendInfo> _followers = new();
    private ObservableCollection<FriendInfo> _following = new();
    private string _selectedTab = "friends";

    public FriendsViewModel(
        INavigationService navigationService,
        IRelationshipService relationshipService,
        IProfileService profileService,
        ICurrentUserService currentUser)
    {
        _navigationService = navigationService;
        _relationshipService = relationshipService;
        _profileService = profileService;
        _currentUser = currentUser;
        
        Title = "Friends";
        
        LoadFriendsCommand = new AsyncDelegateCommand(LoadFriendsAsync);
        ViewProfileCommand = new AsyncDelegateCommand<FriendInfo>(ViewProfileAsync);
        
        RegisterCommand(LoadFriendsCommand);
    }

    public ObservableCollection<FriendInfo> Friends
    {
        get => _friends;
        set => SetProperty(ref _friends, value);
    }

    public ObservableCollection<FriendInfo> Followers
    {
        get => _followers;
        set => SetProperty(ref _followers, value);
    }

    public ObservableCollection<FriendInfo> Following
    {
        get => _following;
        set => SetProperty(ref _following, value);
    }

    public string SelectedTab
    {
        get => _selectedTab;
        set => SetProperty(ref _selectedTab, value);
    }

    public IAsyncCommand LoadFriendsCommand { get; }
    public IAsyncCommand ViewProfileCommand { get; }

    public async Task InitializeAsync(INavigationParameters parameters)
    {
        await LoadFriendsAsync();
    }

    private async Task LoadFriendsAsync()
    {
        if (_currentUser.ProfileId == null) return;
        
        IsBusy = true;
        try
        {
            var followingEdges = await _relationshipService.QueryAsync(new RelationshipQuery
            {
                TenantId = "blazorbook",
                From = new ActivityStream.Abstractions.EntityRefDto { Kind = "Profile", Type = "Profile", Id = _currentUser.ProfileId },
                Kind = RelationshipKind.Follow
            });
            
            var followerEdges = await _relationshipService.QueryAsync(new RelationshipQuery
            {
                TenantId = "blazorbook",
                To = new ActivityStream.Abstractions.EntityRefDto { Kind = "Profile", Type = "Profile", Id = _currentUser.ProfileId },
                Kind = RelationshipKind.Follow
            });
            
            Following = new ObservableCollection<FriendInfo>(
                followingEdges.Select(e => new FriendInfo
                {
                    ProfileId = e.To.Id,
                    DisplayName = e.To.Display ?? e.To.Id,
                    AvatarUrl = null
                }));
            
            Followers = new ObservableCollection<FriendInfo>(
                followerEdges.Select(e => new FriendInfo
                {
                    ProfileId = e.From.Id,
                    DisplayName = e.From.Display ?? e.From.Id,
                    AvatarUrl = null
                }));
            
            var followingIds = followingEdges.Select(e => e.To.Id).ToHashSet();
            var followerIds = followerEdges.Select(e => e.From.Id).ToHashSet();
            var mutualIds = followingIds.Intersect(followerIds).ToHashSet();
            
            Friends = new ObservableCollection<FriendInfo>(
                Following.Where(f => mutualIds.Contains(f.ProfileId)));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ViewProfileAsync(FriendInfo? friend)
    {
        if (friend?.ProfileId == null) return;
        await _navigationService.NavigateAsync($"/profile/{friend.ProfileId}");
    }
}

/// <summary>
/// Simple DTO for friend display.
/// </summary>
public class FriendInfo
{
    public string ProfileId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}
