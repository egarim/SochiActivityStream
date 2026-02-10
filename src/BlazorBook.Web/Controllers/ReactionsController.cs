using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ActivityStream.Abstractions;

namespace BlazorBook.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReactionsController : ControllerBase
{
    private readonly IContentService _contentService;
    private readonly ILogger<ReactionsController> _logger;

    public ReactionsController(
        IContentService contentService,
        ILogger<ReactionsController> logger)
    {
        _contentService = contentService ?? throw new ArgumentNullException(nameof(contentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Add or toggle a reaction on a post
    /// </summary>
    [HttpPut("posts/{postId}")]
    public async Task<IActionResult> ReactToPost(string postId, [FromBody] ReactDto dto)
    {
        try
        {
            var tenantId = User.FindFirst("tenantId")?.Value ?? "default";
            var profileId = User.FindFirst("profileId")?.Value;

            if (string.IsNullOrEmpty(profileId))
            {
                return Unauthorized(new { error = "Profile not found in token" });
            }

            var request = new ReactRequest
            {
                TenantId = tenantId,
                Actor = new EntityRefDto
                {
                    Type = "Profile",
                    Id = profileId
                },
                TargetId = postId,
                TargetKind = ReactionTargetKind.Post,
                Type = dto.Type
            };

            var reaction = await _contentService.ReactAsync(request);

            return Ok(reaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reacting to post {PostId}", postId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Remove reaction from a post
    /// </summary>
    [HttpDelete("posts/{postId}")]
    public async Task<IActionResult> RemoveReactionFromPost(string postId)
    {
        try
        {
            var tenantId = User.FindFirst("tenantId")?.Value ?? "default";
            var profileId = User.FindFirst("profileId")?.Value;

            if (string.IsNullOrEmpty(profileId))
            {
                return Unauthorized(new { error = "Profile not found in token" });
            }

            var request = new RemoveReactionRequest
            {
                TenantId = tenantId,
                Actor = new EntityRefDto
                {
                    Type = "Profile",
                    Id = profileId
                },
                TargetId = postId,
                TargetKind = ReactionTargetKind.Post
            };

            await _contentService.RemoveReactionAsync(request);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing reaction from post {PostId}", postId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get reactions on a post
    /// </summary>
    [HttpGet("posts/{postId}")]
    public async Task<IActionResult> GetPostReactions(
        string postId,
        [FromQuery] ReactionType? type = null,
        [FromQuery] string? cursor = null,
        [FromQuery] int limit = 20)
    {
        try
        {
            var tenantId = User.FindFirst("tenantId")?.Value ?? "default";

            var query = new ReactionQuery
            {
                TenantId = tenantId,
                TargetId = postId,
                TargetKind = ReactionTargetKind.Post,
                Type = type,
                Limit = limit,
                Cursor = cursor
            };

            var result = await _contentService.QueryReactionsAsync(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reactions for post {PostId}", postId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Add or toggle a reaction on a comment
    /// </summary>
    [HttpPut("comments/{commentId}")]
    public async Task<IActionResult> ReactToComment(string commentId, [FromBody] ReactDto dto)
    {
        try
        {
            var tenantId = User.FindFirst("tenantId")?.Value ?? "default";
            var profileId = User.FindFirst("profileId")?.Value;

            if (string.IsNullOrEmpty(profileId))
            {
                return Unauthorized(new { error = "Profile not found in token" });
            }

            var request = new ReactRequest
            {
                TenantId = tenantId,
                Actor = new EntityRefDto
                {
                    Type = "Profile",
                    Id = profileId
                },
                TargetId = commentId,
                TargetKind = ReactionTargetKind.Comment,
                Type = dto.Type
            };

            var reaction = await _contentService.ReactAsync(request);

            return Ok(reaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reacting to comment {CommentId}", commentId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Remove reaction from a comment
    /// </summary>
    [HttpDelete("comments/{commentId}")]
    public async Task<IActionResult> RemoveReactionFromComment(string commentId)
    {
        try
        {
            var tenantId = User.FindFirst("tenantId")?.Value ?? "default";
            var profileId = User.FindFirst("profileId")?.Value;

            if (string.IsNullOrEmpty(profileId))
            {
                return Unauthorized(new { error = "Profile not found in token" });
            }

            var request = new RemoveReactionRequest
            {
                TenantId = tenantId,
                Actor = new EntityRefDto
                {
                    Type = "Profile",
                    Id = profileId
                },
                TargetId = commentId,
                TargetKind = ReactionTargetKind.Comment
            };

            await _contentService.RemoveReactionAsync(request);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing reaction from comment {CommentId}", commentId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

public record ReactDto(ReactionType Type);
