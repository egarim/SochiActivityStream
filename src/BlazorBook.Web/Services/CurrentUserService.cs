using Identity.Abstractions;
using SocialKit.Components.Abstractions;

namespace BlazorBook.Web.Services;

/// <summary>
/// In-memory implementation of current user tracking for BlazorBook.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    public bool IsAuthenticated => ProfileId != null;
    public string? UserId { get; private set; }
    public string? ProfileId { get; private set; }
    public string? Handle { get; private set; }
    public string? DisplayName { get; private set; }
    public string? AvatarUrl { get; private set; }
    
    public Task SignInAsync(ProfileDto profile, string userId)
    {
        UserId = userId;
        ProfileId = profile.Id;
        Handle = profile.Handle;
        DisplayName = profile.DisplayName ?? profile.Handle;
        AvatarUrl = profile.AvatarUrl;
        return Task.CompletedTask;
    }
    
    public Task SignOutAsync()
    {
        UserId = null;
        ProfileId = null;
        Handle = null;
        DisplayName = null;
        AvatarUrl = null;
        return Task.CompletedTask;
    }
}
