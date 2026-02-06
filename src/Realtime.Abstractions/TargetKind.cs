namespace Realtime.Abstractions;

/// <summary>
/// Specifies the kind of target for event delivery.
/// </summary>
public enum TargetKind
{
    /// <summary>Single profile (all their connections).</summary>
    Profile,

    /// <summary>Multiple specific profiles.</summary>
    Profiles,

    /// <summary>All participants in a conversation.</summary>
    Conversation,

    /// <summary>All members of a group.</summary>
    Group,

    /// <summary>All connected users in tenant (broadcast).</summary>
    Tenant,

    /// <summary>Specific connection (for acks, errors).</summary>
    Connection
}
