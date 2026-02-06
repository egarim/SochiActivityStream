using Content.Abstractions;
using SocialKit.Components.Abstractions;

namespace SocialKit.Components.Services;

/// <summary>
/// Default implementation of feed service using the Content service.
/// Reusable across any social network app.
/// </summary>
public class FeedService : IFeedService
{
    private readonly IContentService _contentService;
    private const string TenantId = "blazorbook";

    public FeedService(IContentService contentService)
    {
        _contentService = contentService;
    }

    public async Task<IReadOnlyList<PostDto>> GetFeedAsync(string profileId, int limit = 20, string? cursor = null)
    {
        var query = new PostQuery
        {
            TenantId = TenantId,
            Limit = limit,
            Cursor = cursor,
            Viewer = new EntityRefDto { Type = "Profile", Id = profileId }
        };
        
        var result = await _contentService.QueryPostsAsync(query);
        return result.Items;
    }

    public async Task<PostDto> CreatePostAsync(string authorId, string authorName, string body)
    {
        var request = new CreatePostRequest
        {
            TenantId = TenantId,
            Author = new EntityRefDto { Type = "Profile", Id = authorId, DisplayName = authorName },
            Body = body,
            Visibility = ContentVisibility.Public
        };
        
        return await _contentService.CreatePostAsync(request);
    }

    public async Task LikePostAsync(string postId, string profileId, string displayName)
    {
        var request = new ReactRequest
        {
            TenantId = TenantId,
            TargetKind = ReactionTargetKind.Post,
            TargetId = postId,
            Actor = new EntityRefDto { Type = "Profile", Id = profileId, DisplayName = displayName },
            Type = ReactionType.Like
        };
        
        await _contentService.ReactAsync(request);
    }

    public async Task UnlikePostAsync(string postId, string profileId, string displayName)
    {
        var request = new RemoveReactionRequest
        {
            TenantId = TenantId,
            TargetKind = ReactionTargetKind.Post,
            TargetId = postId,
            Actor = new EntityRefDto { Type = "Profile", Id = profileId, DisplayName = displayName }
        };
        
        await _contentService.RemoveReactionAsync(request);
    }
}
