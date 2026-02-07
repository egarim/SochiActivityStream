using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Media.Abstractions;
using ActivityStream.Abstractions;

namespace BlazorBook.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MediaController : ControllerBase
{
    private readonly IMediaService _mediaService;
    private readonly ILogger<MediaController> _logger;
    private readonly IWebHostEnvironment _environment;

    public MediaController(
        IMediaService mediaService,
        ILogger<MediaController> logger,
        IWebHostEnvironment environment)
    {
        _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    private EntityRefDto GetCurrentUser()
    {
        var profileId = User.FindFirst("profileId")?.Value ?? throw new UnauthorizedAccessException("Profile not found");
        var handle = User.FindFirst("handle")?.Value;
        return new EntityRefDto
        {
            Kind = "user",
            Type = "Profile",
            Id = profileId,
            Display = handle
        };
    }

    private string GetTenantId() => User.FindFirst("tenantId")?.Value ?? "default";

    /// <summary>
    /// Request a signed upload URL
    /// </summary>
    [HttpPost("upload-url")]
    public async Task<IActionResult> RequestUploadUrl([FromBody] RequestUploadUrlDto request)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var tenantId = GetTenantId();

            var uploadRequest = new RequestUploadRequest
            {
                TenantId = tenantId,
                Owner = currentUser,
                FileName = request.FileName,
                ContentType = request.ContentType,
                SizeBytes = request.SizeBytes,
                Purpose = request.Purpose
            };

            var result = await _mediaService.RequestUploadUrlAsync(uploadRequest);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting upload URL");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Upload a file directly (simple upload for smaller files)
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50MB limit
    public async Task<IActionResult> Upload(IFormFile file, [FromQuery] string? purpose = null)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file provided" });

            var currentUser = GetCurrentUser();
            var tenantId = GetTenantId();

            // Request upload URL first to create the media record
            var uploadRequest = new RequestUploadRequest
            {
                TenantId = tenantId,
                Owner = currentUser,
                FileName = file.FileName,
                ContentType = file.ContentType,
                SizeBytes = file.Length,
                Purpose = purpose
            };

            var urlResult = await _mediaService.RequestUploadUrlAsync(uploadRequest);

            // For local development, save to wwwroot/uploads
            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsPath);

            var extension = Path.GetExtension(file.FileName);
            var storedFileName = $"{urlResult.MediaId}{extension}";
            var filePath = Path.Combine(uploadsPath, storedFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Confirm the upload
            var media = await _mediaService.ConfirmUploadAsync(tenantId, urlResult.MediaId);

            // Update the URL to point to local file
            media.Url = $"/uploads/{storedFileName}";

            return Ok(media);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Confirm a file upload completed
    /// </summary>
    [HttpPost("{mediaId}/confirm")]
    public async Task<IActionResult> ConfirmUpload(string mediaId)
    {
        try
        {
            var tenantId = GetTenantId();
            var media = await _mediaService.ConfirmUploadAsync(tenantId, mediaId);
            return Ok(media);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming upload {MediaId}", mediaId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get media metadata
    /// </summary>
    [HttpGet("{mediaId}")]
    public async Task<IActionResult> GetMedia(string mediaId)
    {
        try
        {
            var tenantId = GetTenantId();
            var media = await _mediaService.GetMediaAsync(tenantId, mediaId);

            if (media == null)
                return NotFound(new { error = "Media not found" });

            return Ok(media);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting media {MediaId}", mediaId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get multiple media items by IDs
    /// </summary>
    [HttpPost("batch")]
    public async Task<IActionResult> GetMediaBatch([FromBody] List<string> mediaIds)
    {
        try
        {
            var tenantId = GetTenantId();
            var media = await _mediaService.GetMediaBatchAsync(tenantId, mediaIds);
            return Ok(media);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting media batch");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Update media metadata
    /// </summary>
    [HttpPut("{mediaId}")]
    public async Task<IActionResult> UpdateMedia(string mediaId, [FromBody] UpdateMediaDto request)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var tenantId = GetTenantId();

            var updateRequest = new UpdateMediaRequest
            {
                TenantId = tenantId,
                MediaId = mediaId,
                Actor = currentUser,
                AltText = request.AltText
            };

            var media = await _mediaService.UpdateMediaAsync(updateRequest);
            return Ok(media);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating media {MediaId}", mediaId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete a media item
    /// </summary>
    [HttpDelete("{mediaId}")]
    public async Task<IActionResult> DeleteMedia(string mediaId)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var tenantId = GetTenantId();

            await _mediaService.DeleteMediaAsync(tenantId, mediaId, currentUser);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting media {MediaId}", mediaId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// List media for the current user
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListMedia(
        [FromQuery] string? type = null,
        [FromQuery] string? cursor = null,
        [FromQuery] int limit = 20)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var tenantId = GetTenantId();

            var query = new MediaQuery
            {
                TenantId = tenantId,
                Owner = currentUser,
                Cursor = cursor,
                Limit = limit
            };

            if (!string.IsNullOrEmpty(type) && Enum.TryParse<MediaType>(type, true, out var mediaType))
            {
                query.Type = mediaType;
            }

            var result = await _mediaService.ListMediaAsync(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing media");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// REQUEST DTOs
// ═══════════════════════════════════════════════════════════════════════════════

public class RequestUploadUrlDto
{
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public long? SizeBytes { get; set; }
    public string? Purpose { get; set; }
}

public class UpdateMediaDto
{
    public string? AltText { get; set; }
}
