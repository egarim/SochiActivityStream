using Content.Abstractions;
using Media.Abstractions;
using SocialKit.Components.Abstractions;

namespace SocialKit.Components.Services;

/// <summary>
/// Default implementation of feed service using the Content service.
/// Reusable across any social network app.
/// </summary>
public class FeedService : IFeedService
{
    private readonly IContentService _contentService;
    private readonly IMediaService? _mediaService;
    private const string TenantId = "blazorbook";

    public FeedService(IContentService contentService, IMediaService? mediaService = null)
    {
        _contentService = contentService;
        _mediaService = mediaService;
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
        
        // Populate media URLs for posts that have media
        if (_mediaService != null)
        {
            foreach (var post in result.Items)
            {
                if (post.MediaIds != null && post.MediaIds.Count > 0)
                {
                    var mediaItems = await _mediaService.GetMediaBatchAsync(TenantId, post.MediaIds);
                    post.MediaUrls = mediaItems.Select(m => m.Url ?? string.Empty).Where(u => !string.IsNullOrEmpty(u)).ToList();
                }
            }
        }
        
        return result.Items;
    }

    public async Task<PostDto> CreatePostAsync(string authorId, string authorName, string body, List<string>? mediaIds = null)
    {
        var request = new CreatePostRequest
        {
            TenantId = TenantId,
            Author = new EntityRefDto { Type = "Profile", Id = authorId, DisplayName = authorName },
            Body = body,
            MediaIds = mediaIds,
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

    public async Task<IReadOnlyList<CommentDto>> GetCommentsAsync(string postId, int limit = 20, string? cursor = null)
    {
        var query = new CommentQuery
        {
            TenantId = TenantId,
            PostId = postId,
            Limit = limit,
            Cursor = cursor,
            IncludeDeleted = false
        };
        
        var result = await _contentService.QueryCommentsAsync(query);
        return result.Items;
    }

    public async Task<CommentDto> CreateCommentAsync(string postId, string authorId, string authorName, string body, string? parentCommentId = null)
    {
        var request = new CreateCommentRequest
        {
            TenantId = TenantId,
            PostId = postId,
            Author = new EntityRefDto { Type = "Profile", Id = authorId, DisplayName = authorName },
            Body = body,
            ParentCommentId = parentCommentId
        };
        
        return await _contentService.CreateCommentAsync(request);
    }

    public async Task DeleteCommentAsync(string commentId, string actorId)
    {
        var request = new DeleteCommentRequest
        {
            TenantId = TenantId,
            CommentId = commentId,
            Actor = new EntityRefDto { Type = "Profile", Id = actorId }
        };
        
        await _contentService.DeleteCommentAsync(request);
    }

    public async Task LikeCommentAsync(string commentId, string profileId, string displayName)
    {
        var request = new ReactRequest
        {
            TenantId = TenantId,
            TargetKind = ReactionTargetKind.Comment,
            TargetId = commentId,
            Actor = new EntityRefDto { Type = "Profile", Id = profileId, DisplayName = displayName },
            Type = ReactionType.Like
        };
        
        await _contentService.ReactAsync(request);
    }

    public async Task UnlikeCommentAsync(string commentId, string profileId, string displayName)
    {
        var request = new RemoveReactionRequest
        {
            TenantId = TenantId,
            TargetKind = ReactionTargetKind.Comment,
            TargetId = commentId,
            Actor = new EntityRefDto { Type = "Profile", Id = profileId, DisplayName = displayName }
        };
        
        await _contentService.RemoveReactionAsync(request);
    }

    public async Task<IReadOnlyList<CommentDto>> GetRepliesAsync(string postId, string parentCommentId, int limit = 20, string? cursor = null)
    {
        var query = new CommentQuery
        {
            TenantId = TenantId,
            PostId = postId,
            ParentCommentId = parentCommentId,
            Limit = limit,
            Cursor = cursor,
            IncludeDeleted = false
        };
        
        var result = await _contentService.QueryCommentsAsync(query);
        return result.Items;
    }
}
