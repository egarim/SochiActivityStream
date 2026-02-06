namespace Chat.Abstractions;

/// <summary>
/// Represents a chat conversation (direct or group).
/// </summary>
public sealed class ConversationDto
{
    /// <summary>Unique identifier.</summary>
    public string? Id { get; set; }

    /// <summary>Tenant isolation.</summary>
    public required string TenantId { get; set; }

    /// <summary>Direct (1:1) or Group.</summary>
    public ConversationType Type { get; set; }

    /// <summary>Display title (group chats only, null for direct).</summary>
    public string? Title { get; set; }

    /// <summary>Group avatar URL (optional).</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>Conversation participants.</summary>
    public List<ConversationParticipantDto> Participants { get; set; } = [];

    /// <summary>Most recent message (for conversation list display).</summary>
    public MessageDto? LastMessage { get; set; }

    /// <summary>When the conversation was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Last activity (message sent, participant change).</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Whether the conversation is archived by the viewer.</summary>
    public bool IsArchived { get; set; }

    /// <summary>Whether the conversation is muted by the viewer.</summary>
    public bool IsMuted { get; set; }

    /// <summary>Unread message count for the current viewer.</summary>
    public int UnreadCount { get; set; }
}
