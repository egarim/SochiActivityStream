namespace Realtime.Abstractions;

/// <summary>
/// Validation error codes for realtime operations.
/// </summary>
public enum RealtimeValidationError
{
    None,
    TenantIdRequired,
    EventTypeRequired,
    PayloadRequired,
    TargetRequired,
    InvalidTarget,
    ProfileRequired,
    ConversationIdRequired,
    GroupIdRequired,
    ConnectionIdRequired,
    ConnectionIdMissing,
    ProfileMissing
}
