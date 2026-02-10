namespace Chat.Abstractions;

/// <summary>
/// Request to update conversation settings.
/// </summary>
public sealed class UpdateConversationRequest
{
    public required string TenantId { get; set; }
    public required string ConversationId { get; set; }
    public required ActivityStream.Abstractions.EntityRefDto Actor { get; set; }
    public string? Title { get; set; }
    public string? AvatarUrl { get; set; }
}
