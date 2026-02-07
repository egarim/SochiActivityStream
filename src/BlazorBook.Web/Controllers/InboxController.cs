using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Inbox.Abstractions;
using ActivityStream.Abstractions;

namespace BlazorBook.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InboxController : ControllerBase
{
    private readonly IInboxNotificationService _inboxService;
    private readonly ILogger<InboxController> _logger;

    public InboxController(
        IInboxNotificationService inboxService,
        ILogger<InboxController> logger)
    {
        _inboxService = inboxService ?? throw new ArgumentNullException(nameof(inboxService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
    /// Get inbox notifications for the current user
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetInbox(
        [FromQuery] string? status = null,
        [FromQuery] string? kind = null,
        [FromQuery] string? cursor = null,
        [FromQuery] int limit = 50)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var tenantId = GetTenantId();

            var query = new InboxQuery
            {
                TenantId = tenantId,
                Recipients = [currentUser],
                Cursor = cursor,
                Limit = limit
            };

            // Parse status if provided
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<InboxItemStatus>(status, true, out var parsedStatus))
            {
                query.Status = parsedStatus;
            }

            // Parse kind if provided
            if (!string.IsNullOrEmpty(kind) && Enum.TryParse<InboxItemKind>(kind, true, out var parsedKind))
            {
                query.Kind = parsedKind;
            }

            var result = await _inboxService.QueryInboxAsync(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inbox");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get unread notification count
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        try
        {
            var currentUser = GetCurrentUser();
            var tenantId = GetTenantId();

            var query = new InboxQuery
            {
                TenantId = tenantId,
                Recipients = [currentUser],
                Status = InboxItemStatus.Unread,
                Limit = 100 // Just to get count
            };

            var result = await _inboxService.QueryInboxAsync(query);
            return Ok(new { unreadCount = result.Items.Count, hasMore = result.NextCursor != null });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread count");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Mark a notification as read
    /// </summary>
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkRead(string id)
    {
        try
        {
            var tenantId = GetTenantId();
            await _inboxService.MarkReadAsync(tenantId, id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {Id} as read", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Archive a notification
    /// </summary>
    [HttpPut("{id}/archive")]
    public async Task<IActionResult> Archive(string id)
    {
        try
        {
            var tenantId = GetTenantId();
            await _inboxService.ArchiveAsync(tenantId, id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving notification {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Mark all notifications as read
    /// </summary>
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        try
        {
            var currentUser = GetCurrentUser();
            var tenantId = GetTenantId();

            // Get all unread items
            var query = new InboxQuery
            {
                TenantId = tenantId,
                Recipients = [currentUser],
                Status = InboxItemStatus.Unread,
                Limit = 100
            };

            var result = await _inboxService.QueryInboxAsync(query);
            
            // Mark each as read
            foreach (var item in result.Items)
            {
                await _inboxService.MarkReadAsync(tenantId, item.Id!);
            }

            return Ok(new { markedRead = result.Items.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // FOLLOW REQUESTS
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Approve a follow request
    /// </summary>
    [HttpPut("follow-requests/{requestId}/approve")]
    public async Task<IActionResult> ApproveFollowRequest(string requestId, [FromBody] ApproveRejectRequest? request = null)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var tenantId = GetTenantId();

            var result = await _inboxService.ApproveRequestAsync(
                tenantId, requestId, currentUser, request?.Reason);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving follow request {RequestId}", requestId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Deny a follow request
    /// </summary>
    [HttpPut("follow-requests/{requestId}/deny")]
    public async Task<IActionResult> DenyFollowRequest(string requestId, [FromBody] ApproveRejectRequest? request = null)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var tenantId = GetTenantId();

            var result = await _inboxService.DenyRequestAsync(
                tenantId, requestId, currentUser, request?.Reason);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error denying follow request {RequestId}", requestId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

public class ApproveRejectRequest
{
    public string? Reason { get; set; }
}
