namespace Chat.Abstractions;

/// <summary>
/// Request to create a new conversation.
/// </summary>
public sealed class CreateConversationRequest
{
    /// <summary>Tenant isolation.</summary>
    public required string TenantId { get; set; }

    /// <summary>User creating the conversation.</summary>
    public required EntityRefDto Creator { get; set; }

    /// <summary>Direct or Group.</summary>
    public ConversationType Type { get; set; }

    /// <summary>Initial participants (including creator for group).</summary>
    public required List<EntityRefDto> Participants { get; set; }

    /// <summary>Group title (required for group, ignored for direct).</summary>
    public string? Title { get; set; }
}
