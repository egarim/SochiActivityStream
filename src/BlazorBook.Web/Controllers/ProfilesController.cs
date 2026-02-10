using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Identity.Abstractions;
using Search.Abstractions;

namespace BlazorBook.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfilesController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly IProfileStore _profileStore;
    private readonly ISearchIndexer _searchIndexer;
    private readonly ILogger<ProfilesController> _logger;

    public ProfilesController(
        IProfileService profileService,
        IProfileStore profileStore,
        ISearchIndexer searchIndexer,
        ILogger<ProfilesController> logger)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _profileStore = profileStore ?? throw new ArgumentNullException(nameof(profileStore));
        _searchIndexer = searchIndexer ?? throw new ArgumentNullException(nameof(searchIndexer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get the current user's profile
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentProfile()
    {
        try
        {
            var profileId = User.FindFirst("profileId")?.Value;

            if (string.IsNullOrEmpty(profileId))
            {
                return Unauthorized(new { error = "Profile not found in token" });
            }

            var profile = await _profileService.GetProfileByIdAsync(profileId);

            if (profile == null)
            {
                return NotFound(new { error = "Profile not found" });
            }

            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current profile");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get a profile by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProfile(string id)
    {
        try
        {
            var profile = await _profileService.GetProfileByIdAsync(id);

            if (profile == null)
            {
                return NotFound(new { error = "Profile not found" });
            }

            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profile {ProfileId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get a profile by handle (username)
    /// </summary>
    [HttpGet("by-handle/{handle}")]
    public async Task<IActionResult> GetProfileByHandle(string handle)
    {
        try
        {
            var record = await _profileStore.FindByHandleAsync(handle.ToLowerInvariant());

            if (record == null)
            {
                return NotFound(new { error = "Profile not found" });
            }

            return Ok(record.Profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profile by handle {Handle}", handle);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Update the current user's profile
    /// </summary>
    [HttpPut("me")]
    public async Task<IActionResult> UpdateCurrentProfile([FromBody] UpdateProfileDto dto)
    {
        try
        {
            var profileId = User.FindFirst("profileId")?.Value;

            if (string.IsNullOrEmpty(profileId))
            {
                return Unauthorized(new { error = "Profile not found in token" });
            }

            var existingRecord = await _profileStore.GetByIdAsync(profileId);

            if (existingRecord == null)
            {
                return NotFound(new { error = "Profile not found" });
            }

            var updatedProfile = existingRecord.Profile;
            
            if (!string.IsNullOrEmpty(dto.DisplayName))
            {
                updatedProfile.DisplayName = dto.DisplayName;
            }

            if (dto.AvatarUrl != null)
            {
                updatedProfile.AvatarUrl = dto.AvatarUrl;
            }

            if (dto.IsPrivate.HasValue)
            {
                updatedProfile.IsPrivate = dto.IsPrivate.Value;
            }

            await _profileStore.UpdateAsync(new ProfileRecord { Profile = updatedProfile });

            // Index profile for search
            try
            {
                await IndexProfileAsync(updatedProfile, "blazorbook");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to index profile {ProfileId} for search", updatedProfile.Id);
            }

            return Ok(updatedProfile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Upload avatar for the current user's profile
    /// </summary>
    [HttpPost("me/avatar")]
    public async Task<IActionResult> UploadAvatar([FromForm] IFormFile file)
    {
        try
        {
            var profileId = User.FindFirst("profileId")?.Value;

            if (string.IsNullOrEmpty(profileId))
            {
                return Unauthorized(new { error = "Profile not found in token" });
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file provided" });
            }

            // Validate file type
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                return BadRequest(new { error = "Invalid file type. Allowed: JPEG, PNG, GIF, WebP" });
            }

            // Validate file size (max 5MB)
            if (file.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new { error = "File too large. Maximum size: 5MB" });
            }

            // Save file to wwwroot/uploads/avatars
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{profileId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var avatarUrl = $"/uploads/avatars/{fileName}";

            // Update profile with new avatar URL
            var existingRecord = await _profileStore.GetByIdAsync(profileId);
            if (existingRecord != null)
            {
                existingRecord.Profile.AvatarUrl = avatarUrl;
                await _profileStore.UpdateAsync(existingRecord);

                // Re-index profile for search
                try
                {
                    await IndexProfileAsync(existingRecord.Profile, "blazorbook");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to re-index profile after avatar upload");
                }
            }

            return Ok(new { avatarUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading avatar");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private async Task IndexProfileAsync(ProfileDto profile, string tenantId)
    {
        var doc = new SearchDocument
        {
            Id = profile.Id,
            DocumentType = "Profile",
            TenantId = tenantId,
            TextFields = new Dictionary<string, string>
            {
                ["displayName"] = profile.DisplayName ?? string.Empty,
                ["username"] = profile.Handle ?? string.Empty,
                ["bio"] = string.Empty // Bio field would be added if ProfileDto has it
            },
            KeywordFields = new Dictionary<string, List<string>>
            {
                ["handle"] = new List<string> { profile.Handle ?? string.Empty }
            },
            DateFields = new Dictionary<string, DateTimeOffset>
            {
                ["createdAt"] = profile.CreatedAt
            },
            Boost = 1.5 // Profiles are slightly more important than posts
        };

        await _searchIndexer.IndexAsync(doc);
    }
}

public record UpdateProfileDto(
    string? DisplayName = null,
    string? AvatarUrl = null,
    bool? IsPrivate = null
);
