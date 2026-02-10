namespace Chat.Abstractions;

/// <summary>
/// Request to delete a message.
/// </summary>
public sealed class DeleteMessageRequest
{
    public required string TenantId { get; set; }
    public required string ConversationId { get; set; }
    public required string MessageId { get; set; }
    public required ActivityStream.Abstractions.EntityRefDto Actor { get; set; }

    /// <summary>If true, delete for everyone (sender only). Otherwise, delete for self.</summary>
    public bool DeleteForEveryone { get; set; }
}
