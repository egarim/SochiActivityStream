# Realtime.Core

Core business logic for the Realtime Hub service. This project implements the publisher and presence tracking logic without any transport dependencies.

## Key Types

### RealtimePublisher

Main implementation of `IRealtimePublisher`. Handles:
- Event validation
- Target resolution (profiles → connections)
- Dispatching to the transport layer

```csharp
var publisher = new RealtimePublisher(
    dispatcher,          // IEventDispatcher (transport provides this)
    connectionManager,   // IConnectionManager
    membershipResolver,  // IGroupMembershipResolver
    idGenerator);        // IIdGenerator

// Send to a single profile
await publisher.SendToProfileAsync(
    tenantId: "tenant1",
    profile: EntityRefDto.Profile("user123"),
    eventType: RealtimeEventTypes.MessageReceived,
    payload: messageDto);
```

### PresenceTrackerCore

Tracks presence based on connection state:
- Online when at least one active connection exists
- Away when all connections are idle (configurable timeout)
- Offline when no connections exist

Fires `PresenceChanged` events for status transitions.

### EventValidator

Static validation for events and targets:
- Required fields validation
- Target type validation
- Connection registration validation

### NullGroupMembershipResolver

Default no-op resolver for conversation/group membership. Replace with actual implementation when Chat/Groups services exist.

## Configuration

```csharp
var options = new RealtimeServiceOptions
{
    IdleTimeoutMinutes = 5,           // When to mark as Away
    AutoPublishPresenceEvents = true, // Auto-publish presence changes
    MaxConnectionsPerProfile = 0      // 0 = unlimited
};
```

## Dependencies

- `Realtime.Abstractions` - DTOs and interfaces
