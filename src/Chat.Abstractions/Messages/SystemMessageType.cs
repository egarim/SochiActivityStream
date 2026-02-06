namespace Chat.Abstractions;

/// <summary>
/// Types of system-generated messages.
/// </summary>
public enum SystemMessageType
{
    /// <summary>Participant joined the conversation.</summary>
    ParticipantJoined,

    /// <summary>Participant left the conversation.</summary>
    ParticipantLeft,

    /// <summary>Participant was removed.</summary>
    ParticipantRemoved,

    /// <summary>Conversation title changed.</summary>
    TitleChanged,

    /// <summary>Conversation avatar changed.</summary>
    AvatarChanged
}
