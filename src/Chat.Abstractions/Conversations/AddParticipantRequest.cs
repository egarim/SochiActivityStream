namespace Chat.Abstractions;

/// <summary>
/// Request to add a participant to a conversation.
/// </summary>
public sealed class AddParticipantRequest
{
    public required string TenantId { get; set; }
    public required string ConversationId { get; set; }
    public required EntityRefDto Actor { get; set; }
    public required EntityRefDto NewParticipant { get; set; }
}
