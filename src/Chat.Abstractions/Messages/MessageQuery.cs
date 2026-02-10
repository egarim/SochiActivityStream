namespace Chat.Abstractions;

/// <summary>
/// Query for listing messages.
/// </summary>
public sealed class MessageQuery
{
    public required string TenantId { get; set; }
    public required string ConversationId { get; set; }

    /// <summary>Viewer context (for delete filtering).</summary>
    public required ActivityStream.Abstractions.EntityRefDto Viewer { get; set; }

    /// <summary>Cursor for pagination (message ID).</summary>
    public string? Cursor { get; set; }

    /// <summary>Direction: newer or older than cursor.</summary>
    public MessageQueryDirection Direction { get; set; } = MessageQueryDirection.Older;

    /// <summary>Page size.</summary>
    public int Limit { get; set; } = 50;
}

/// <summary>
/// Direction for message query pagination.
/// </summary>
public enum MessageQueryDirection
{
    /// <summary>Get messages older than cursor (scrolling up).</summary>
    Older = 0,

    /// <summary>Get messages newer than cursor (new messages).</summary>
    Newer = 1
}
