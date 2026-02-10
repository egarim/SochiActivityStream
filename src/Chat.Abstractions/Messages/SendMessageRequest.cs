namespace Chat.Abstractions;

/// <summary>
/// Request to send a message.
/// </summary>
public sealed class SendMessageRequest
{
    public required string TenantId { get; set; }
    public required string ConversationId { get; set; }
    public required ActivityStream.Abstractions.EntityRefDto Sender { get; set; }
    public required string Body { get; set; }
    public List<MediaRefDto>? Media { get; set; }
    public string? ReplyToMessageId { get; set; }
}
