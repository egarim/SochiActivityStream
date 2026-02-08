using Identity.Abstractions;

namespace SocialKit.Components.Abstractions;

/// <summary>
/// Provides access to the current authenticated user's profile.
/// Each app provides its own implementation (Web cookie, MAUI SecureStorage, etc.).
/// </summary>
public interface ICurrentUserService
{
    bool IsAuthenticated { get; }
    string? UserId { get; }
    string? ProfileId { get; }
    string? Handle { get; }
    string? DisplayName { get; }
    string? AvatarUrl { get; }
    
    /// <summary>
    /// Event raised when authentication state changes (sign in/sign out).
    /// </summary>
    event Action? OnAuthStateChanged;
    
    Task SignInAsync(ProfileDto profile, string userId);
    Task SignOutAsync();
}
