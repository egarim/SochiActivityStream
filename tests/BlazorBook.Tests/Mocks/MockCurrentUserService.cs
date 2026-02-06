using SocialKit.Components.Abstractions;
using Identity.Abstractions;

namespace BlazorBook.Tests.Mocks;

/// <summary>
/// Mock current user service for testing authenticated flows.
/// </summary>
public class MockCurrentUserService : ICurrentUserService
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
    
    /// <summary>
    /// Convenience method for tests to quickly sign in a user.
    /// </summary>
    public void SignIn(string profileId, string displayName, string? avatarUrl = null)
    {
        ProfileId = profileId;
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
        Handle = profileId; // Use profileId as handle for simplicity
        UserId = $"user-{profileId}";
    }
}
