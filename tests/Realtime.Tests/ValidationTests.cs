using Realtime.Abstractions;
using Realtime.Core;
using Xunit;

namespace Realtime.Tests;

/// <summary>
/// Tests for event and target validation.
/// </summary>
public class ValidationTests
{
    [Fact]
    public void ValidateEvent_MissingTenantId_Throws()
    {
        var evt = new RealtimeEvent
        {
            TenantId = "",
            Type = "test.event",
            Payload = new { },
            Target = EventTarget.ToTenant()
        };

        var ex = Assert.Throws<RealtimeValidationException>(() => EventValidator.Validate(evt));
        Assert.Equal(RealtimeValidationError.TenantIdRequired, ex.Error);
    }

    [Fact]
    public void ValidateEvent_MissingEventType_Throws()
    {
        var evt = new RealtimeEvent
        {
            TenantId = "tenant1",
            Type = "",
            Payload = new { },
            Target = EventTarget.ToTenant()
        };

        var ex = Assert.Throws<RealtimeValidationException>(() => EventValidator.Validate(evt));
        Assert.Equal(RealtimeValidationError.EventTypeRequired, ex.Error);
    }

    [Fact]
    public void ValidateEvent_NullPayload_Throws()
    {
        var evt = new RealtimeEvent
        {
            TenantId = "tenant1",
            Type = "test.event",
            Payload = null!,
            Target = EventTarget.ToTenant()
        };

        var ex = Assert.Throws<RealtimeValidationException>(() => EventValidator.Validate(evt));
        Assert.Equal(RealtimeValidationError.PayloadRequired, ex.Error);
    }

    [Fact]
    public void ValidateEvent_NullTarget_Throws()
    {
        var evt = new RealtimeEvent
        {
            TenantId = "tenant1",
            Type = "test.event",
            Payload = new { },
            Target = null!
        };

        var ex = Assert.Throws<RealtimeValidationException>(() => EventValidator.Validate(evt));
        Assert.Equal(RealtimeValidationError.TargetRequired, ex.Error);
    }

    [Fact]
    public void ValidateTarget_Profile_MissingProfiles_Throws()
    {
        var target = new EventTarget { Kind = TargetKind.Profile, Profiles = null };

        var ex = Assert.Throws<RealtimeValidationException>(() => EventValidator.ValidateTarget(target));
        Assert.Equal(RealtimeValidationError.ProfileRequired, ex.Error);
    }

    [Fact]
    public void ValidateTarget_Profile_EmptyProfiles_Throws()
    {
        var target = new EventTarget { Kind = TargetKind.Profile, Profiles = [] };

        var ex = Assert.Throws<RealtimeValidationException>(() => EventValidator.ValidateTarget(target));
        Assert.Equal(RealtimeValidationError.ProfileRequired, ex.Error);
    }

    [Fact]
    public void ValidateTarget_Profiles_MissingProfiles_Throws()
    {
        var target = new EventTarget { Kind = TargetKind.Profiles, Profiles = null };

        var ex = Assert.Throws<RealtimeValidationException>(() => EventValidator.ValidateTarget(target));
        Assert.Equal(RealtimeValidationError.ProfileRequired, ex.Error);
    }

    [Fact]
    public void ValidateTarget_Conversation_MissingId_Throws()
    {
        var target = new EventTarget { Kind = TargetKind.Conversation, ConversationId = null };

        var ex = Assert.Throws<RealtimeValidationException>(() => EventValidator.ValidateTarget(target));
        Assert.Equal(RealtimeValidationError.ConversationIdRequired, ex.Error);
    }

    [Fact]
    public void ValidateTarget_Group_MissingId_Throws()
    {
        var target = new EventTarget { Kind = TargetKind.Group, GroupId = null };

        var ex = Assert.Throws<RealtimeValidationException>(() => EventValidator.ValidateTarget(target));
        Assert.Equal(RealtimeValidationError.GroupIdRequired, ex.Error);
    }

    [Fact]
    public void ValidateTarget_Connection_MissingId_Throws()
    {
        var target = new EventTarget { Kind = TargetKind.Connection, ConnectionId = null };

        var ex = Assert.Throws<RealtimeValidationException>(() => EventValidator.ValidateTarget(target));
        Assert.Equal(RealtimeValidationError.ConnectionIdRequired, ex.Error);
    }

    [Fact]
    public void ValidateTarget_Tenant_NoAdditionalValidation()
    {
        var target = new EventTarget { Kind = TargetKind.Tenant };

        // Should not throw
        EventValidator.ValidateTarget(target);
    }

    [Fact]
    public void ValidateConnectionRegistration_MissingConnectionId_Throws()
    {
        var ex = Assert.Throws<RealtimeValidationException>(() =>
            EventValidator.ValidateConnectionRegistration("", "tenant1", EntityRefDto.Profile("user1")));
        Assert.Equal(RealtimeValidationError.ConnectionIdMissing, ex.Error);
    }

    [Fact]
    public void ValidateConnectionRegistration_MissingTenantId_Throws()
    {
        var ex = Assert.Throws<RealtimeValidationException>(() =>
            EventValidator.ValidateConnectionRegistration("conn1", "", EntityRefDto.Profile("user1")));
        Assert.Equal(RealtimeValidationError.TenantIdRequired, ex.Error);
    }

    [Fact]
    public void ValidateConnectionRegistration_MissingProfile_Throws()
    {
        var ex = Assert.Throws<RealtimeValidationException>(() =>
            EventValidator.ValidateConnectionRegistration("conn1", "tenant1", null!));
        Assert.Equal(RealtimeValidationError.ProfileMissing, ex.Error);
    }

    [Fact]
    public void ValidateConnectionRegistration_ValidParams_NoThrow()
    {
        // Should not throw
        EventValidator.ValidateConnectionRegistration("conn1", "tenant1", EntityRefDto.Profile("user1"));
    }

    [Fact]
    public void ValidateEvent_ValidEvent_NoThrow()
    {
        var evt = new RealtimeEvent
        {
            TenantId = "tenant1",
            Type = "test.event",
            Payload = new { data = 123 },
            Target = EventTarget.ToProfile(EntityRefDto.Profile("user1"))
        };

        // Should not throw
        EventValidator.Validate(evt);
    }
}
