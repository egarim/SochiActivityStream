namespace Chat.Abstractions;

/// <summary>
/// Request to mark messages as read.
/// </summary>
public sealed class MarkReadRequest
{
    public required string TenantId { get; set; }
    public required string ConversationId { get; set; }
    public required EntityRefDto Profile { get; set; }
    public required string MessageId { get; set; }
}
