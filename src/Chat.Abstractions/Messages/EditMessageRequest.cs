namespace Chat.Abstractions;

/// <summary>
/// Request to edit a message.
/// </summary>
public sealed class EditMessageRequest
{
    public required string TenantId { get; set; }
    public required string ConversationId { get; set; }
    public required string MessageId { get; set; }
    public required EntityRefDto Actor { get; set; }
    public required string Body { get; set; }
}
