using Realtime.Abstractions;

namespace Realtime.Core;

/// <summary>
/// Validates realtime events.
/// </summary>
public static class EventValidator
{
    /// <summary>
    /// Validates a realtime event and throws if invalid.
    /// </summary>
    public static void Validate(RealtimeEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);

        if (string.IsNullOrWhiteSpace(evt.TenantId))
            throw new RealtimeValidationException(RealtimeValidationError.TenantIdRequired);

        if (string.IsNullOrWhiteSpace(evt.Type))
            throw new RealtimeValidationException(RealtimeValidationError.EventTypeRequired);

        if (evt.Payload is null)
            throw new RealtimeValidationException(RealtimeValidationError.PayloadRequired);

        if (evt.Target is null)
            throw new RealtimeValidationException(RealtimeValidationError.TargetRequired);

        ValidateTarget(evt.Target);
    }

    /// <summary>
    /// Validates an event target.
    /// </summary>
    public static void ValidateTarget(EventTarget target)
    {
        switch (target.Kind)
        {
            case TargetKind.Profile:
                if (target.Profiles is null || target.Profiles.Count == 0)
                    throw new RealtimeValidationException(RealtimeValidationError.ProfileRequired);
                break;

            case TargetKind.Profiles:
                if (target.Profiles is null || target.Profiles.Count == 0)
                    throw new RealtimeValidationException(RealtimeValidationError.ProfileRequired);
                break;

            case TargetKind.Conversation:
                if (string.IsNullOrWhiteSpace(target.ConversationId))
                    throw new RealtimeValidationException(RealtimeValidationError.ConversationIdRequired);
                break;

            case TargetKind.Group:
                if (string.IsNullOrWhiteSpace(target.GroupId))
                    throw new RealtimeValidationException(RealtimeValidationError.GroupIdRequired);
                break;

            case TargetKind.Connection:
                if (string.IsNullOrWhiteSpace(target.ConnectionId))
                    throw new RealtimeValidationException(RealtimeValidationError.ConnectionIdRequired);
                break;

            case TargetKind.Tenant:
                // No additional validation needed
                break;

            default:
                throw new RealtimeValidationException(RealtimeValidationError.InvalidTarget);
        }
    }

    /// <summary>
    /// Validates connection registration parameters.
    /// </summary>
    public static void ValidateConnectionRegistration(string connectionId, string tenantId, EntityRefDto profile)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
            throw new RealtimeValidationException(RealtimeValidationError.ConnectionIdMissing);

        if (string.IsNullOrWhiteSpace(tenantId))
            throw new RealtimeValidationException(RealtimeValidationError.TenantIdRequired);

        if (profile is null)
            throw new RealtimeValidationException(RealtimeValidationError.ProfileMissing);
    }
}
