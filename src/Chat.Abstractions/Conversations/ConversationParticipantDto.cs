namespace Chat.Abstractions;

/// <summary>
/// A participant in a conversation.
/// </summary>
public sealed class ConversationParticipantDto
{
    /// <summary>The participant's profile.</summary>
    public required ActivityStream.Abstractions.EntityRefDto Profile { get; set; }

    /// <summary>Role in the conversation (member, admin).</summary>
    public ParticipantRole Role { get; set; }

    /// <summary>When the participant joined.</summary>
    public DateTimeOffset JoinedAt { get; set; }

    /// <summary>Last time the participant read messages.</summary>
    public DateTimeOffset? LastReadAt { get; set; }

    /// <summary>ID of the last message the participant read.</summary>
    public string? LastReadMessageId { get; set; }

    /// <summary>Whether this participant has left the conversation.</summary>
    public bool HasLeft { get; set; }

    /// <summary>When the participant left (if applicable).</summary>
    public DateTimeOffset? LeftAt { get; set; }
}
