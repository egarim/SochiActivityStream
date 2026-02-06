namespace Chat.Abstractions;

/// <summary>
/// A message within a conversation.
/// </summary>
public sealed class MessageDto
{
    /// <summary>Unique identifier.</summary>
    public string? Id { get; set; }

    /// <summary>Tenant isolation.</summary>
    public required string TenantId { get; set; }

    /// <summary>Parent conversation ID.</summary>
    public required string ConversationId { get; set; }

    /// <summary>Message author.</summary>
    public required EntityRefDto Sender { get; set; }

    /// <summary>Message content (text).</summary>
    public required string Body { get; set; }

    /// <summary>Attached media (images, files).</summary>
    public List<MediaRefDto>? Media { get; set; }

    /// <summary>Message this is replying to (quote reply).</summary>
    public string? ReplyToMessageId { get; set; }

    /// <summary>Populated ReplyTo message (for display).</summary>
    public MessageDto? ReplyTo { get; set; }

    /// <summary>When the message was sent.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>When the message was last edited.</summary>
    public DateTimeOffset? EditedAt { get; set; }

    /// <summary>Whether the message has been deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Who deleted this message (for partial delete tracking).</summary>
    public List<string>? DeletedByProfileIds { get; set; }

    /// <summary>System message type (null for regular messages).</summary>
    public SystemMessageType? SystemMessageType { get; set; }
}
