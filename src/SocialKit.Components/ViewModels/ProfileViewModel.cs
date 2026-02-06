using System.Collections.ObjectModel;
using Content.Abstractions;
using Identity.Abstractions;
using RelationshipService.Abstractions;
using Sochi.Navigation.Commands;
using Sochi.Navigation.Mvvm;
using Sochi.Navigation.Navigation;
using SocialKit.Components.Abstractions;

namespace SocialKit.Components.ViewModels;

/// <summary>
/// ViewModel for the user profile page.
/// </summary>
public class ProfileViewModel : ViewModelBase, IInitialize
{
    private readonly INavigationService _navigationService;
    private readonly IProfileService _profileService;
    private readonly IContentService _contentService;
    private readonly IRelationshipService _relationshipService;
    private readonly ICurrentUserService _currentUser;
    
    private ProfileDto? _profile;
    private ObservableCollection<PostDto> _posts = new();
    private bool _isOwnProfile;
    private bool _isFollowing;
    private int _followerCount;
    private int _followingCount;

    public ProfileViewModel(
        INavigationService navigationService,
        IProfileService profileService,
        IContentService contentService,
        IRelationshipService relationshipService,
        ICurrentUserService currentUser)
    {
        _navigationService = navigationService;
        _profileService = profileService;
        _contentService = contentService;
        _relationshipService = relationshipService;
        _currentUser = currentUser;
        
        Title = "Profile";
        
        FollowCommand = new AsyncDelegateCommand(FollowAsync, () => !IsBusy && !IsOwnProfile);
        UnfollowCommand = new AsyncDelegateCommand(UnfollowAsync, () => !IsBusy && !IsOwnProfile && IsFollowing);
        
        RegisterCommand(FollowCommand);
        RegisterCommand(UnfollowCommand);
    }

    public ProfileDto? Profile
    {
        get => _profile;
        set
        {
            if (SetProperty(ref _profile, value))
            {
                Title = value?.DisplayName ?? value?.Handle ?? "Profile";
            }
        }
    }

    public ObservableCollection<PostDto> Posts
    {
        get => _posts;
        set => SetProperty(ref _posts, value);
    }

    public bool IsOwnProfile
    {
        get => _isOwnProfile;
        set
        {
            if (SetProperty(ref _isOwnProfile, value))
            {
                RaiseCanExecuteChangedForAllCommands();
            }
        }
    }

    public bool IsFollowing
    {
        get => _isFollowing;
        set
        {
            if (SetProperty(ref _isFollowing, value))
            {
                RaiseCanExecuteChangedForAllCommands();
            }
        }
    }

    public int FollowerCount
    {
        get => _followerCount;
        set => SetProperty(ref _followerCount, value);
    }

    public int FollowingCount
    {
        get => _followingCount;
        set => SetProperty(ref _followingCount, value);
    }

    public IAsyncCommand FollowCommand { get; }
    public IAsyncCommand UnfollowCommand { get; }

    public async Task InitializeAsync(INavigationParameters parameters)
    {
        string? profileId = null;
        
        if (parameters.TryGetValue<string>("profileId", out var id))
        {
            profileId = id;
        }
        
        if (string.IsNullOrEmpty(profileId))
        {
            profileId = _currentUser.ProfileId;
        }
        
        if (string.IsNullOrEmpty(profileId)) return;
        
        IsBusy = true;
        try
        {
            await LoadProfileAsync(profileId);
            await LoadPostsAsync(profileId);
            await LoadRelationshipStatusAsync(profileId);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadProfileAsync(string profileId)
    {
        Profile = await _profileService.GetProfileByIdAsync(profileId);
        IsOwnProfile = profileId == _currentUser.ProfileId;
    }

    private async Task LoadPostsAsync(string profileId)
    {
        var query = new PostQuery
        {
            TenantId = "blazorbook",
            Author = new EntityRefDto { Type = "Profile", Id = profileId },
            Limit = 20
        };
        
        var result = await _contentService.QueryPostsAsync(query);
        Posts = new ObservableCollection<PostDto>(result.Items);
    }

    private async Task LoadRelationshipStatusAsync(string profileId)
    {
        if (_currentUser.ProfileId == null || IsOwnProfile) return;
        
        var edges = await _relationshipService.QueryAsync(new RelationshipQuery
        {
            TenantId = "blazorbook",
            From = new ActivityStream.Abstractions.EntityRefDto { Kind = "Profile", Type = "Profile", Id = _currentUser.ProfileId },
            To = new ActivityStream.Abstractions.EntityRefDto { Kind = "Profile", Type = "Profile", Id = profileId },
            Kind = RelationshipKind.Follow
        });
        
        IsFollowing = edges.Any();
        
        var followers = await _relationshipService.QueryAsync(new RelationshipQuery
        {
            TenantId = "blazorbook",
            To = new ActivityStream.Abstractions.EntityRefDto { Kind = "Profile", Type = "Profile", Id = profileId },
            Kind = RelationshipKind.Follow
        });
        
        var following = await _relationshipService.QueryAsync(new RelationshipQuery
        {
            TenantId = "blazorbook",
            From = new ActivityStream.Abstractions.EntityRefDto { Kind = "Profile", Type = "Profile", Id = profileId },
            Kind = RelationshipKind.Follow
        });
        
        FollowerCount = followers.Count;
        FollowingCount = following.Count;
    }

    private async Task FollowAsync()
    {
        if (Profile?.Id == null || _currentUser.ProfileId == null) return;
        
        IsBusy = true;
        try
        {
            await _relationshipService.UpsertAsync(new RelationshipEdgeDto
            {
                TenantId = "blazorbook",
                From = new ActivityStream.Abstractions.EntityRefDto
                {
                    Kind = "Profile",
                    Type = "Profile",
                    Id = _currentUser.ProfileId,
                    Display = _currentUser.DisplayName
                },
                To = new ActivityStream.Abstractions.EntityRefDto
                {
                    Kind = "Profile",
                    Type = "Profile",
                    Id = Profile.Id,
                    Display = Profile.DisplayName
                },
                Kind = RelationshipKind.Follow
            });
            
            IsFollowing = true;
            FollowerCount++;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task UnfollowAsync()
    {
        if (Profile?.Id == null || _currentUser.ProfileId == null) return;
        
        IsBusy = true;
        try
        {
            var edges = await _relationshipService.QueryAsync(new RelationshipQuery
            {
                TenantId = "blazorbook",
                From = new ActivityStream.Abstractions.EntityRefDto { Kind = "Profile", Type = "Profile", Id = _currentUser.ProfileId },
                To = new ActivityStream.Abstractions.EntityRefDto { Kind = "Profile", Type = "Profile", Id = Profile.Id },
                Kind = RelationshipKind.Follow
            });
            
            foreach (var edge in edges)
            {
                if (edge.Id != null)
                {
                    await _relationshipService.RemoveAsync("blazorbook", edge.Id);
                }
            }
            
            IsFollowing = false;
            FollowerCount--;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
