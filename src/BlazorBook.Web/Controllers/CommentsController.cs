using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ActivityStream.Abstractions;

namespace BlazorBook.Web.Controllers;

[ApiController]
[Route("api/posts/{postId}/[controller]")]
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly IContentService _contentService;
    private readonly ILogger<CommentsController> _logger;

    public CommentsController(
        IContentService contentService,
        ILogger<CommentsController> logger)
    {
        _contentService = contentService ?? throw new ArgumentNullException(nameof(contentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get comments for a post
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetComments(
        string postId,
        [FromQuery] string? parentCommentId = null,
        [FromQuery] string? cursor = null,
        [FromQuery] int limit = 20)
    {
        try
        {
            var tenantId = User.FindFirst("tenantId")?.Value ?? "default";
            var profileId = User.FindFirst("profileId")?.Value;

            var query = new CommentQuery
            {
                TenantId = tenantId,
                PostId = postId,
                ParentCommentId = parentCommentId,
                Limit = limit,
                Cursor = cursor,
                IncludeDeleted = false,
                Viewer = !string.IsNullOrEmpty(profileId) ? new EntityRefDto { Type = "Profile", Id = profileId } : null
            };

            var result = await _contentService.QueryCommentsAsync(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comments for post {PostId}", postId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get a specific comment
    /// </summary>
    [HttpGet("{commentId}")]
    public async Task<IActionResult> GetComment(string postId, string commentId)
    {
        try
        {
            var tenantId = User.FindFirst("tenantId")?.Value ?? "default";
            var profileId = User.FindFirst("profileId")?.Value;

            var viewer = !string.IsNullOrEmpty(profileId) 
                ? new EntityRefDto { Type = "Profile", Id = profileId } 
                : null;

            var comment = await _contentService.GetCommentAsync(tenantId, commentId, viewer);

            if (comment == null)
            {
                return NotFound(new { error = "Comment not found" });
            }

            // Verify comment belongs to the specified post
            if (comment.PostId != postId)
            {
                return NotFound(new { error = "Comment not found for this post" });
            }

            return Ok(comment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comment {CommentId}", commentId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Create a new comment
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateComment(string postId, [FromBody] CreateCommentDto dto)
    {
        try
        {
            var tenantId = User.FindFirst("tenantId")?.Value ?? "default";
            var profileId = User.FindFirst("profileId")?.Value;

            if (string.IsNullOrEmpty(profileId))
            {
                return Unauthorized(new { error = "Profile not found in token" });
            }

            var request = new CreateCommentRequest
            {
                TenantId = tenantId,
                Author = new EntityRefDto
                {
                    Type = "Profile",
                    Id = profileId
                },
                PostId = postId,
                ParentCommentId = dto.ParentCommentId,
                Body = dto.Body
            };

            var comment = await _contentService.CreateCommentAsync(request);

            return CreatedAtAction(
                nameof(GetComment), 
                new { postId, commentId = comment.Id }, 
                comment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating comment for post {PostId}", postId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete a comment
    /// </summary>
    [HttpDelete("{commentId}")]
    public async Task<IActionResult> DeleteComment(string postId, string commentId)
    {
        try
        {
            var tenantId = User.FindFirst("tenantId")?.Value ?? "default";
            var profileId = User.FindFirst("profileId")?.Value;

            if (string.IsNullOrEmpty(profileId))
            {
                return Unauthorized(new { error = "Profile not found in token" });
            }

            // Verify comment exists and belongs to the post
            var existingComment = await _contentService.GetCommentAsync(tenantId, commentId);
            if (existingComment == null || existingComment.PostId != postId)
            {
                return NotFound(new { error = "Comment not found" });
            }

            var request = new DeleteCommentRequest
            {
                TenantId = tenantId,
                CommentId = commentId,
                Actor = new EntityRefDto
                {
                    Type = "Profile",
                    Id = profileId
                }
            };

            await _contentService.DeleteCommentAsync(request);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting comment {CommentId}", commentId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

public record CreateCommentDto(string Body, string? ParentCommentId = null);
