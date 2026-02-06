# Realtime.Abstractions

Pure interface and DTO definitions for the Realtime Hub service. This project has **zero dependencies** and defines the contracts that any transport implementation can fulfill.

## Purpose

This library defines the abstractions for real-time event delivery:
- Event envelopes and targeting
- Connection management interfaces
- Presence tracking interfaces
- Transport-agnostic dispatch interfaces

## Key Types

### Events
- `RealtimeEvent` - Event envelope with type, payload, target, and metadata
- `RealtimeEventTypes` - Constants for standard event types
- `EventTarget` - Specifies where to deliver an event
- `TargetKind` - Profile, Profiles, Conversation, Group, Tenant, Connection

### Connection
- `ConnectionInfo` - Metadata about an active connection
- `IConnectionManager` - Add/remove connections, query by profile/tenant
- `IPresenceTracker` - Online/offline/away tracking with change events

### Publishing
- `IRealtimePublisher` - Main interface services use to push events
- `IEventDispatcher` - Transport implementation interface

### Membership
- `IGroupMembershipResolver` - Plugin for resolving conversation/group members

## Architecture

```
┌─────────────────┐    ┌──────────────────────┐    ┌─────────────────────┐
│  Your Services  │───▶│  IRealtimePublisher  │───▶│  IEventDispatcher   │
│  (Content, Chat)│    │  (publish events)    │    │  (route to targets) │
└─────────────────┘    └──────────────────────┘    └─────────────────────┘
                                                              │
                              ┌────────────────────────────────┼────────────────┐
                              ▼                                ▼                ▼
                    ┌─────────────────┐           ┌─────────────────┐  ┌─────────────────┐
                    │ SignalR Transport│           │ InMemory        │  │ WebSocket       │
                    │ (production)     │           │ (testing)       │  │ (future)        │
                    └─────────────────┘           └─────────────────┘  └─────────────────┘
```

## Event Types

| Category | Events |
|----------|--------|
| Activity | `activity.created` |
| Inbox | `inbox.item.created`, `inbox.item.updated`, `inbox.item.read` |
| Content | `post.created`, `post.updated`, `post.deleted`, `comment.*`, `reaction.*` |
| Chat | `message.received`, `message.edited`, `message.deleted` |
| Presence | `presence.online`, `presence.offline`, `presence.away` |
| Typing | `typing.started`, `typing.stopped` |

## Usage

Services inject `IRealtimePublisher` to push events:

```csharp
await _publisher.SendToProfileAsync(
    tenantId,
    targetProfile,
    RealtimeEventTypes.MessageReceived,
    messagePayload);
```

## Dependencies

None. This is a pure abstractions library.
