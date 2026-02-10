using ActivityStream.Abstractions;

namespace SocialKit.Components.Abstractions;

/// <summary>
/// Service for retrieving and managing the user's feed.
/// </summary>
public interface IFeedService
{
    Task<IReadOnlyList<PostDto>> GetFeedAsync(string profileId, int limit = 20, string? cursor = null);
    Task<PostDto> CreatePostAsync(string authorId, string authorName, string body, List<string>? mediaIds = null);
    Task LikePostAsync(string postId, string profileId, string displayName);
    Task UnlikePostAsync(string postId, string profileId, string displayName);
    
    // Comment operations
    Task<IReadOnlyList<CommentDto>> GetCommentsAsync(string postId, int limit = 20, string? cursor = null);
    Task<CommentDto> CreateCommentAsync(string postId, string authorId, string authorName, string body, string? parentCommentId = null);
    Task DeleteCommentAsync(string commentId, string actorId);
    Task LikeCommentAsync(string commentId, string profileId, string displayName);
    Task UnlikeCommentAsync(string commentId, string profileId, string displayName);
    Task<IReadOnlyList<CommentDto>> GetRepliesAsync(string postId, string parentCommentId, int limit = 20, string? cursor = null);
}
