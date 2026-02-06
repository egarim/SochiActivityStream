using Content.Abstractions;

namespace SocialKit.Components.Abstractions;

/// <summary>
/// Service for retrieving and managing the user's feed.
/// </summary>
public interface IFeedService
{
    Task<IReadOnlyList<PostDto>> GetFeedAsync(string profileId, int limit = 20, string? cursor = null);
    Task<PostDto> CreatePostAsync(string authorId, string authorName, string body);
    Task LikePostAsync(string postId, string profileId, string displayName);
    Task UnlikePostAsync(string postId, string profileId, string displayName);
}
