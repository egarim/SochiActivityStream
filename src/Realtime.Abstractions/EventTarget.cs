namespace Realtime.Abstractions;

/// <summary>
/// Specifies where an event should be delivered.
/// </summary>
public sealed class EventTarget
{
    /// <summary>What kind of target.</summary>
    public required TargetKind Kind { get; set; }

    /// <summary>Profile(s) for Profile/Profiles targets.</summary>
    public List<EntityRefDto>? Profiles { get; set; }

    /// <summary>Conversation ID for Conversation target.</summary>
    public string? ConversationId { get; set; }

    /// <summary>Group ID for Group target.</summary>
    public string? GroupId { get; set; }

    /// <summary>Specific connection ID (for direct ack).</summary>
    public string? ConnectionId { get; set; }

    /// <summary>Creates a target for a single profile.</summary>
    public static EventTarget ToProfile(EntityRefDto profile) =>
        new() { Kind = TargetKind.Profile, Profiles = [profile] };

    /// <summary>Creates a target for multiple profiles.</summary>
    public static EventTarget ToProfiles(IEnumerable<EntityRefDto> profiles) =>
        new() { Kind = TargetKind.Profiles, Profiles = profiles.ToList() };

    /// <summary>Creates a target for a conversation.</summary>
    public static EventTarget ToConversation(string conversationId) =>
        new() { Kind = TargetKind.Conversation, ConversationId = conversationId };

    /// <summary>Creates a target for a group.</summary>
    public static EventTarget ToGroup(string groupId) =>
        new() { Kind = TargetKind.Group, GroupId = groupId };

    /// <summary>Creates a broadcast target for the entire tenant.</summary>
    public static EventTarget ToTenant() =>
        new() { Kind = TargetKind.Tenant };

    /// <summary>Creates a target for a specific connection.</summary>
    public static EventTarget ToConnection(string connectionId) =>
        new() { Kind = TargetKind.Connection, ConnectionId = connectionId };
}
