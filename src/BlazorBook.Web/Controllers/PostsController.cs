using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ActivityStream.Abstractions;
using Search.Abstractions;
using EntityRefDto = ActivityStream.Abstractions.EntityRefDto;

namespace BlazorBook.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PostsController : ControllerBase
{
    private readonly IContentService _contentService;
    private readonly ISearchIndexer _searchIndexer;
    private readonly ILogger<PostsController> _logger;

    public PostsController(
        IContentService contentService,
        ISearchIndexer searchIndexer,
        ILogger<PostsController> logger)
    {
        _contentService = contentService ?? throw new ArgumentNullException(nameof(contentService));
        _searchIndexer = searchIndexer ?? throw new ArgumentNullException(nameof(searchIndexer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    public async Task<IActionResult> GetPosts(
        [FromQuery] string? cursor = null,
        [FromQuery] int limit = 20,
        [FromQuery] string? authorId = null)
    {
        try
        {
            var tenantId = User.FindFirst("tenantId")?.Value ?? "default";
            var profileId = User.FindFirst("profileId")?.Value;

            if (string.IsNullOrEmpty(profileId))
            {
                return Unauthorized(new { error = "Profile not found in token" });
            }

            var query = new PostQuery
            {
                TenantId = tenantId,
                Limit = limit,
                Cursor = cursor,
                IncludeDeleted = false
            };

            if (!string.IsNullOrEmpty(authorId))
            {
                query.Author = new EntityRefDto
                {
                    Type = "Profile",
                    Id = authorId
                };
            }

            var result = await _contentService.QueryPostsAsync(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting posts");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPost(string id)
    {
        try
        {
            var tenantId = User.FindFirst("tenantId")?.Value ?? "default";
            var post = await _contentService.GetPostAsync(tenantId, id);

            if (post == null)
            {
                return NotFound(new { error = "Post not found" });
            }

            return Ok(post);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting post {PostId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostDto dto)
    {
        try
        {
            var tenantId = User.FindFirst("tenantId")?.Value ?? "default";
            var profileId = User.FindFirst("profileId")?.Value;

            if (string.IsNullOrEmpty(profileId))
            {
                return Unauthorized(new { error = "Profile not found in token" });
            }

            var createRequest = new CreatePostRequest
            {
                TenantId = tenantId,
                Author = new EntityRefDto
                {
                    Type = "Profile",
                    Id = profileId
                },
                Body = dto.Body,
                MediaIds = dto.MediaIds,
                Visibility = dto.Visibility
            };

            var post = await _contentService.CreatePostAsync(createRequest);

            // Index post for search
            try
            {
                await IndexPostAsync(post, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to index post {PostId} for search", post.Id);
                // Don't fail the request if indexing fails
            }

            return CreatedAtAction(nameof(GetPost), new { id = post.Id }, post);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating post");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePost(string id, [FromBody] UpdatePostDto dto)
    {
        try
        {
            var tenantId = User.FindFirst("tenantId")?.Value ?? "default";
            var profileId = User.FindFirst("profileId")?.Value;

            if (string.IsNullOrEmpty(profileId))
            {
                return Unauthorized(new { error = "Profile not found in token" });
            }

            var updateRequest = new UpdatePostRequest
            {
                TenantId = tenantId,
                PostId = id,
                Actor = new EntityRefDto
                {
                    Type = "Profile",
                    Id = profileId
                },
                Body = dto.Body,
                Visibility = dto.Visibility
            };

            var post = await _contentService.UpdatePostAsync(updateRequest);

            if (post == null)
            {
                return NotFound(new { error = "Post not found" });
            }

            // Re-index post for search
            try
            {
                await IndexPostAsync(post, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to re-index post {PostId} for search", post.Id);
            }

            return Ok(post);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating post {PostId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePost(string id)
    {
        try
        {
            var tenantId = User.FindFirst("tenantId")?.Value ?? "default";
            var profileId = User.FindFirst("profileId")?.Value;

            if (string.IsNullOrEmpty(profileId))
            {
                return Unauthorized(new { error = "Profile not found in token" });
            }

            var deleteRequest = new DeletePostRequest
            {
                TenantId = tenantId,
                PostId = id,
                Actor = new EntityRefDto
                {
                    Type = "Profile",
                    Id = profileId
                }
            };

            await _contentService.DeletePostAsync(deleteRequest);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting post {PostId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private async Task IndexPostAsync(PostDto post, string tenantId)
    {
        var doc = new SearchDocument
        {
            Id = post.Id,
            DocumentType = "Post",
            TenantId = tenantId,
            TextFields = new Dictionary<string, string>
            {
                ["body"] = post.Body ?? string.Empty,
                ["authorName"] = post.Author?.DisplayName ?? "Unknown"
            },
            KeywordFields = new Dictionary<string, List<string>>
            {
                ["authorId"] = new List<string> { post.Author?.Id ?? string.Empty },
                ["visibility"] = new List<string> { post.Visibility.ToString() }
            },
            DateFields = new Dictionary<string, DateTimeOffset>
            {
                ["createdAt"] = post.CreatedAt
            },
            Boost = 1.0
        };

        await _searchIndexer.IndexAsync(doc);
    }
}

public record CreatePostDto(string Body, List<string>? MediaIds, ContentVisibility Visibility);
public record UpdatePostDto(string Body, List<string>? MediaIds, ContentVisibility Visibility);
