namespace Chat.Abstractions;

/// <summary>
/// Role of a participant in a conversation.
/// </summary>
public enum ParticipantRole
{
    /// <summary>Regular participant.</summary>
    Member = 0,

    /// <summary>Can manage participants and settings.</summary>
    Admin = 1,

    /// <summary>Creator of the conversation (cannot be removed).</summary>
    Owner = 2
}
