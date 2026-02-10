namespace Chat.Abstractions;

/// <summary>
/// Request to remove a participant from a conversation.
/// </summary>
public sealed class RemoveParticipantRequest
{
    public required string TenantId { get; set; }
    public required string ConversationId { get; set; }
    public required ActivityStream.Abstractions.EntityRefDto Actor { get; set; }
    public required ActivityStream.Abstractions.EntityRefDto Participant { get; set; }
}
