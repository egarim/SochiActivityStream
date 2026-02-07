using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RelationshipService.Abstractions;
using ActivityStream.Abstractions;
using Identity.Abstractions;

namespace BlazorBook.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RelationshipsController : ControllerBase
{
    private readonly IRelationshipService _relationshipService;
    private readonly IProfileService _profileService;
    private readonly ILogger<RelationshipsController> _logger;

    public RelationshipsController(
        IRelationshipService relationshipService,
        IProfileService profileService,
        ILogger<RelationshipsController> logger)
    {
        _relationshipService = relationshipService ?? throw new ArgumentNullException(nameof(relationshipService));
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Follow a user
    /// </summary>
    [HttpPost("follow/{profileId}")]
    public async Task<IActionResult> Follow(string profileId)
    {
        try
        {
            var tenantId = User.FindFirst("tenantId")?.Value ?? "default";
            var currentProfileId = User.FindFirst("profileId")?.Value;

            if (string.IsNullOrEmpty(currentProfileId))
            {
                return Unauthorized(new { error = "Profile not found in token" });
            }

            if (currentProfileId == profileId)
            {
                return BadRequest(new { error = "Cannot follow yourself" });
            }

            // Verify target profile exists
            var targetProfile = await _profileService.GetProfileByIdAsync(profileId);
            if (targetProfile == null)
            {
                return NotFound(new { error = "User not found" });
            }

            var edge = new RelationshipEdgeDto
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                From = new EntityRefDto
                {
                    Kind = "user",
                    Type = "Profile",
                    Id = currentProfileId
                },
                To = new EntityRefDto
                {
                    Kind = "user",
                    Type = "Profile",
                    Id = profileId,
                    Display = targetProfile.DisplayName ?? targetProfile.Handle
                },
                Kind = RelationshipKind.Follow,
                Scope = RelationshipScope.Any,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };

            var result = await _relationshipService.UpsertAsync(edge);

            return Ok(new
            {
                id = result.Id,
                followingId = profileId,
                followingHandle = targetProfile.Handle,
                followingDisplayName = targetProfile.DisplayName,
                createdAt = result.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error following user {ProfileId}", profileId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Unfollow a user
    /// </summary>
    [HttpDelete("follow/{profileId}")]
    public async Task<IActionResult> Unfollow(string profileId)
    {
        try
        {
            var tenantId = User.FindFirst("tenantId")?.Value ?? "default";
            var currentProfileId = User.FindFirst("profileId")?.Value;

            if (string.IsNullOrEmpty(currentProfileId))
            {
                return Unauthorized(new { error = "Profile not found in token" });
            }

            var existingEdge = await _relationshipService.GetEdgeAsync(
                tenantId,
                new EntityRefDto { Kind = "user", Type = "Profile", Id = currentProfileId },
                new EntityRefDto { Kind = "user", Type = "Profile", Id = profileId },
                RelationshipKind.Follow
            );

            if (existingEdge == null)
            {
                return NotFound(new { error = "Not following this user" });
            }

            await _relationshipService.RemoveAsync(tenantId, existingEdge.Id!);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unfollowing user {ProfileId}", profileId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Check if current user follows a specific user
    /// </summary>
    [HttpGet("following/{profileId}")]
    public async Task<IActionResult> IsFollowing(string profileId)
    {
        try
        {
            var tenantId = User.FindFirst("tenantId")?.Value ?? "default";
            var currentProfileId = User.FindFirst("profileId")?.Value;

            if (string.IsNullOrEmpty(currentProfileId))
            {
                return Unauthorized(new { error = "Profile not found in token" });
            }

            var edge = await _relationshipService.GetEdgeAsync(
                tenantId,
                new EntityRefDto { Kind = "user", Type = "Profile", Id = currentProfileId },
                new EntityRefDto { Kind = "user", Type = "Profile", Id = profileId },
                RelationshipKind.Follow
            );

            return Ok(new { isFollowing = edge != null && edge.IsActive });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking follow status for {ProfileId}", profileId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get users that the current user follows
    /// </summary>
    [HttpGet("following")]
    public async Task<IActionResult> GetFollowing([FromQuery] int limit = 50)
    {
        try
        {
            var tenantId = User.FindFirst("tenantId")?.Value ?? "default";
            var currentProfileId = User.FindFirst("profileId")?.Value;

            if (string.IsNullOrEmpty(currentProfileId))
            {
                return Unauthorized(new { error = "Profile not found in token" });
            }

            var query = new RelationshipQuery
            {
                TenantId = tenantId,
                From = new EntityRefDto { Kind = "user", Type = "Profile", Id = currentProfileId },
                Kind = RelationshipKind.Follow,
                IsActive = true,
                Limit = limit
            };

            var edges = await _relationshipService.QueryAsync(query);

            var following = new List<object>();
            foreach (var edge in edges)
            {
                var profile = await _profileService.GetProfileByIdAsync(edge.To.Id);
                if (profile != null)
                {
                    following.Add(new
                    {
                        id = profile.Id,
                        handle = profile.Handle,
                        displayName = profile.DisplayName,
                        avatarUrl = profile.AvatarUrl,
                        followedAt = edge.CreatedAt
                    });
                }
            }

            return Ok(new { items = following, count = following.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting following list");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get users that follow the current user
    /// </summary>
    [HttpGet("followers")]
    public async Task<IActionResult> GetFollowers([FromQuery] int limit = 50)
    {
        try
        {
            var tenantId = User.FindFirst("tenantId")?.Value ?? "default";
            var currentProfileId = User.FindFirst("profileId")?.Value;

            if (string.IsNullOrEmpty(currentProfileId))
            {
                return Unauthorized(new { error = "Profile not found in token" });
            }

            var query = new RelationshipQuery
            {
                TenantId = tenantId,
                To = new EntityRefDto { Kind = "user", Type = "Profile", Id = currentProfileId },
                Kind = RelationshipKind.Follow,
                IsActive = true,
                Limit = limit
            };

            var edges = await _relationshipService.QueryAsync(query);

            var followers = new List<object>();
            foreach (var edge in edges)
            {
                var profile = await _profileService.GetProfileByIdAsync(edge.From.Id);
                if (profile != null)
                {
                    followers.Add(new
                    {
                        id = profile.Id,
                        handle = profile.Handle,
                        displayName = profile.DisplayName,
                        avatarUrl = profile.AvatarUrl,
                        followedAt = edge.CreatedAt
                    });
                }
            }

            return Ok(new { items = followers, count = followers.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting followers list");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get a specific user's followers
    /// </summary>
    [HttpGet("users/{profileId}/followers")]
    public async Task<IActionResult> GetUserFollowers(string profileId, [FromQuery] int limit = 50)
    {
        try
        {
            var tenantId = User.FindFirst("tenantId")?.Value ?? "default";

            var query = new RelationshipQuery
            {
                TenantId = tenantId,
                To = new EntityRefDto { Kind = "user", Type = "Profile", Id = profileId },
                Kind = RelationshipKind.Follow,
                IsActive = true,
                Limit = limit
            };

            var edges = await _relationshipService.QueryAsync(query);

            var followers = new List<object>();
            foreach (var edge in edges)
            {
                var profile = await _profileService.GetProfileByIdAsync(edge.From.Id);
                if (profile != null)
                {
                    followers.Add(new
                    {
                        id = profile.Id,
                        handle = profile.Handle,
                        displayName = profile.DisplayName,
                        avatarUrl = profile.AvatarUrl,
                        followedAt = edge.CreatedAt
                    });
                }
            }

            return Ok(new { items = followers, count = followers.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting followers for user {ProfileId}", profileId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get a specific user's following
    /// </summary>
    [HttpGet("users/{profileId}/following")]
    public async Task<IActionResult> GetUserFollowing(string profileId, [FromQuery] int limit = 50)
    {
        try
        {
            var tenantId = User.FindFirst("tenantId")?.Value ?? "default";

            var query = new RelationshipQuery
            {
                TenantId = tenantId,
                From = new EntityRefDto { Kind = "user", Type = "Profile", Id = profileId },
                Kind = RelationshipKind.Follow,
                IsActive = true,
                Limit = limit
            };

            var edges = await _relationshipService.QueryAsync(query);

            var following = new List<object>();
            foreach (var edge in edges)
            {
                var profile = await _profileService.GetProfileByIdAsync(edge.To.Id);
                if (profile != null)
                {
                    following.Add(new
                    {
                        id = profile.Id,
                        handle = profile.Handle,
                        displayName = profile.DisplayName,
                        avatarUrl = profile.AvatarUrl,
                        followedAt = edge.CreatedAt
                    });
                }
            }

            return Ok(new { items = following, count = following.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting following for user {ProfileId}", profileId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Check if two users follow each other (mutual)
    /// </summary>
    [HttpGet("mutual/{profileId}")]
    public async Task<IActionResult> AreMutual(string profileId)
    {
        try
        {
            var tenantId = User.FindFirst("tenantId")?.Value ?? "default";
            var currentProfileId = User.FindFirst("profileId")?.Value;

            if (string.IsNullOrEmpty(currentProfileId))
            {
                return Unauthorized(new { error = "Profile not found in token" });
            }

            var areMutual = await _relationshipService.AreMutualAsync(
                tenantId,
                new EntityRefDto { Kind = "user", Type = "Profile", Id = currentProfileId },
                new EntityRefDto { Kind = "user", Type = "Profile", Id = profileId },
                RelationshipKind.Follow
            );

            return Ok(new { areMutual });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking mutual status for {ProfileId}", profileId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
