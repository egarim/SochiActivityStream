using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ActivityStream.Abstractions;
using Chat.Abstractions;

namespace BlazorBook.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IChatService chatService,
        ILogger<ChatController> logger)
    {
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private EntityRefDto GetCurrentUser()
    {
        var profileId = User.FindFirst("profileId")?.Value ?? throw new UnauthorizedAccessException("Profile not found");
        var handle = User.FindFirst("handle")?.Value;
        return new EntityRefDto
        {
            Type = "profile",
            Id = profileId,
            DisplayName = handle
        };
    }

    private string GetTenantId() => User.FindFirst("tenantId")?.Value ?? "default";

    // ═══════════════════════════════════════════════════════════════════════════════
    // CONVERSATIONS
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Get or create a direct conversation with another user
    /// </summary>
    [HttpPost("conversations/direct/{targetProfileId}")]
    public async Task<IActionResult> GetOrCreateDirectConversation(string targetProfileId)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var tenantId = GetTenantId();

            var targetUser = new EntityRefDto
            {
                Type = "profile",
                Id = targetProfileId
            };

            var conversation = await _chatService.GetOrCreateDirectConversationAsync(
                tenantId, currentUser, targetUser);

            return Ok(conversation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating direct conversation with {TargetProfileId}", targetProfileId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Create a new group conversation
    /// </summary>
    [HttpPost("conversations/group")]
    public async Task<IActionResult> CreateGroupConversation([FromBody] CreateGroupRequest request)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var tenantId = GetTenantId();

            var createRequest = new CreateConversationRequest
            {
                TenantId = tenantId,
                Title = request.Title,
                Creator = currentUser,
                Participants = request.ParticipantIds.Select(id => new EntityRefDto
                {
                    Type = "profile",
                    Id = id
                }).ToList()
            };

            var conversation = await _chatService.CreateGroupConversationAsync(createRequest);
            return Ok(conversation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating group conversation");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// List conversations for the current user
    /// </summary>
    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations(
        [FromQuery] string? cursor = null,
        [FromQuery] int limit = 20,
        [FromQuery] bool includeArchived = false)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var tenantId = GetTenantId();

            var query = new ConversationQuery
            {
                TenantId = tenantId,
                Participant = currentUser,
                IncludeArchived = includeArchived,
                Cursor = cursor,
                Limit = limit
            };

            var result = await _chatService.GetConversationsAsync(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversations");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get a specific conversation
    /// </summary>
    [HttpGet("conversations/{conversationId}")]
    public async Task<IActionResult> GetConversation(string conversationId)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var tenantId = GetTenantId();

            var conversation = await _chatService.GetConversationAsync(
                tenantId, conversationId, currentUser);

            if (conversation == null)
                return NotFound(new { error = "Conversation not found" });

            return Ok(conversation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation {ConversationId}", conversationId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Archive/unarchive a conversation
    /// </summary>
    [HttpPut("conversations/{conversationId}/archive")]
    public async Task<IActionResult> SetArchived(string conversationId, [FromBody] SetArchivedRequest request)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var tenantId = GetTenantId();

            await _chatService.SetArchivedAsync(tenantId, conversationId, currentUser, request.Archived);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving conversation {ConversationId}", conversationId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Mute/unmute a conversation
    /// </summary>
    [HttpPut("conversations/{conversationId}/mute")]
    public async Task<IActionResult> SetMuted(string conversationId, [FromBody] SetMutedRequest request)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var tenantId = GetTenantId();

            await _chatService.SetMutedAsync(tenantId, conversationId, currentUser, request.Muted);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error muting conversation {ConversationId}", conversationId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Leave a group conversation
    /// </summary>
    [HttpPost("conversations/{conversationId}/leave")]
    public async Task<IActionResult> LeaveConversation(string conversationId)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var tenantId = GetTenantId();

            await _chatService.LeaveConversationAsync(tenantId, conversationId, currentUser);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving conversation {ConversationId}", conversationId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // MESSAGES
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Send a message to a conversation
    /// </summary>
    [HttpPost("conversations/{conversationId}/messages")]
    public async Task<IActionResult> SendMessage(string conversationId, [FromBody] SendMessageDto request)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var tenantId = GetTenantId();

            var sendRequest = new SendMessageRequest
            {
                TenantId = tenantId,
                ConversationId = conversationId,
                Sender = currentUser,
                Body = request.Body,
                ReplyToMessageId = request.ReplyToMessageId
            };

            var message = await _chatService.SendMessageAsync(sendRequest);
            return Ok(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to conversation {ConversationId}", conversationId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get messages in a conversation
    /// </summary>
    [HttpGet("conversations/{conversationId}/messages")]
    public async Task<IActionResult> GetMessages(
        string conversationId,
        [FromQuery] string? cursor = null,
        [FromQuery] int limit = 50,
        [FromQuery] string direction = "older")
    {
        try
        {
            var currentUser = GetCurrentUser();
            var tenantId = GetTenantId();

            var query = new MessageQuery
            {
                TenantId = tenantId,
                ConversationId = conversationId,
                Viewer = currentUser,
                Cursor = cursor,
                Limit = limit,
                Direction = direction.ToLower() == "newer" 
                    ? MessageQueryDirection.Newer 
                    : MessageQueryDirection.Older
            };

            var result = await _chatService.GetMessagesAsync(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting messages for conversation {ConversationId}", conversationId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Edit a message
    /// </summary>
    [HttpPut("conversations/{conversationId}/messages/{messageId}")]
    public async Task<IActionResult> EditMessage(
        string conversationId, 
        string messageId, 
        [FromBody] EditMessageDto request)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var tenantId = GetTenantId();

            var editRequest = new EditMessageRequest
            {
                TenantId = tenantId,
                ConversationId = conversationId,
                MessageId = messageId,
                Actor = currentUser,
                Body = request.Body
            };

            var message = await _chatService.EditMessageAsync(editRequest);
            return Ok(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing message {MessageId}", messageId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete a message
    /// </summary>
    [HttpDelete("conversations/{conversationId}/messages/{messageId}")]
    public async Task<IActionResult> DeleteMessage(
        string conversationId, 
        string messageId,
        [FromQuery] bool forEveryone = false)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var tenantId = GetTenantId();

            var deleteRequest = new DeleteMessageRequest
            {
                TenantId = tenantId,
                ConversationId = conversationId,
                MessageId = messageId,
                Actor = currentUser,
                DeleteForEveryone = forEveryone
            };

            await _chatService.DeleteMessageAsync(deleteRequest);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting message {MessageId}", messageId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Mark messages as read
    /// </summary>
    [HttpPost("conversations/{conversationId}/read")]
    public async Task<IActionResult> MarkRead(string conversationId, [FromBody] MarkReadDto request)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var tenantId = GetTenantId();

            var markReadRequest = new MarkReadRequest
            {
                TenantId = tenantId,
                ConversationId = conversationId,
                Profile = currentUser,
                MessageId = request.UpToMessageId
            };

            await _chatService.MarkReadAsync(markReadRequest);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking messages as read in conversation {ConversationId}", conversationId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get total unread message count
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        try
        {
            var currentUser = GetCurrentUser();
            var tenantId = GetTenantId();

            var count = await _chatService.GetTotalUnreadCountAsync(tenantId, currentUser);
            return Ok(new { unreadCount = count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread count");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// REQUEST DTOs (API-specific)
// ═══════════════════════════════════════════════════════════════════════════════

public class CreateGroupRequest
{
    public required string Title { get; set; }
    public required List<string> ParticipantIds { get; set; }
}

public class SendMessageDto
{
    public required string Body { get; set; }
    public string? ReplyToMessageId { get; set; }
}

public class EditMessageDto
{
    public required string Body { get; set; }
}

public class MarkReadDto
{
    public required string UpToMessageId { get; set; }
}

public class SetArchivedRequest
{
    public bool Archived { get; set; }
}

public class SetMutedRequest
{
    public bool Muted { get; set; }
}
