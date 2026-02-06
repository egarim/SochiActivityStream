namespace Chat.Abstractions;

/// <summary>
/// Type of conversation.
/// </summary>
public enum ConversationType
{
    /// <summary>1:1 private conversation.</summary>
    Direct = 0,

    /// <summary>Multi-user group conversation.</summary>
    Group = 1
}
