namespace Realtime.Abstractions;

/// <summary>
/// User presence status.
/// </summary>
public enum PresenceStatus
{
    /// <summary>User has at least one active connection.</summary>
    Online,

    /// <summary>User has no active connections.</summary>
    Offline,

    /// <summary>User is connected but idle.</summary>
    Away
}
