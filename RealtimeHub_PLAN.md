# Realtime Hub Service — Implementation Plan

**Goal:** Build a pluggable real-time event delivery system that connects clients to server-pushed events (notifications, messages, presence, typing indicators) with transport-agnostic abstractions.

---

## Executive Summary

| Layer | Project | Purpose | Dependencies |
|-------|---------|---------|--------------|
| Abstractions | `Realtime.Abstractions` | Pure interfaces, DTOs, event definitions | None |
| Core | `Realtime.Core` | Connection orchestration, presence tracking | Realtime.Abstractions |
| Transport | `Realtime.Transport.InMemory` | In-memory reference (for tests) | Realtime.Abstractions |
| Transport | `Realtime.Transport.SignalR` | ASP.NET SignalR implementation | Realtime.Abstractions, SignalR |

**Key Design Principle:** The abstractions layer defines contracts that ANY transport can implement (SignalR, raw WebSockets, Server-Sent Events, long-polling, etc.). Services push events through `IRealtimePublisher`; transport implementations handle delivery.

---

## 1) Core Concepts

### 1.1 Event Flow Architecture

```
┌─────────────────┐    ┌──────────────────────┐    ┌─────────────────────┐
│  Your Services  │───▶│  IRealtimePublisher  │───▶│  IEventDispatcher   │
│  (Content, Chat)│    │  (publish events)    │    │  (route to targets) │
└─────────────────┘    └──────────────────────┘    └─────────────────────┘
                                                              │
                                                              ▼
                              ┌────────────────────────────────────────────────┐
                              │              IConnectionManager                │
                              │  (maps profiles → connections, tracks presence)│
                              └────────────────────────────────────────────────┘
                                                              │
                              ┌────────────────────────────────┼────────────────┐
                              ▼                                ▼                ▼
                    ┌─────────────────┐           ┌─────────────────┐  ┌─────────────────┐
                    │ SignalR Transport│           │ WebSocket Raw   │  │ SSE Transport   │
                    │ (Realtime.SignalR)│          │ (future)        │  │ (future)        │
                    └─────────────────┘           └─────────────────┘  └─────────────────┘
```

### 1.2 Event Categories

| Category | Events | Description |
|----------|--------|-------------|
| **Activity** | `activity.created` | New activity in user's feed |
| **Inbox** | `inbox.item.created`, `inbox.item.updated`, `inbox.item.read` | Notification changes |
| **Content** | `post.created`, `post.updated`, `post.deleted`, `comment.created`, `reaction.added` | Content changes |
| **Chat** | `message.received`, `message.edited`, `message.deleted` | Direct/group messages |
| **Presence** | `presence.online`, `presence.offline`, `presence.away` | User online status |
| **Typing** | `typing.started`, `typing.stopped` | Typing indicators |

### 1.3 Target Types

Events can be targeted to:
- **Profile** — Single user (all their connections)
- **Profiles** — Multiple specific users
- **Conversation** — All participants in a chat
- **Group** — All members of a group
- **Tenant** — Broadcast to all connected users in tenant
- **Connection** — Specific connection (for acks)

### 1.4 Multi-Tenant Isolation

All operations are tenant-scoped. Connections are isolated by tenant to prevent cross-tenant leakage.

---

## 2) Abstractions Layer (`Realtime.Abstractions`)

### 2.1 Project Structure

```
src/Realtime.Abstractions/
├── Realtime.Abstractions.csproj
├── README.md
│
├── Events/
│   ├── RealtimeEvent.cs           # Base event envelope
│   ├── RealtimeEventTypes.cs      # Event type constants
│   └── PresenceStatus.cs          # Enum: Online, Offline, Away
│
├── Targeting/
│   ├── EventTarget.cs             # Target specification
│   ├── TargetKind.cs              # Enum: Profile, Profiles, Conversation, Group, Tenant
│   └── EntityRefDto.cs            # Entity reference (reuse pattern)
│
├── Connection/
│   ├── ConnectionInfo.cs          # Connection metadata
│   ├── IConnectionManager.cs      # Connection tracking interface
│   └── IPresenceTracker.cs        # Online/offline tracking interface
│
├── Publishing/
│   ├── IRealtimePublisher.cs      # Main publish interface (services use this)
│   └── IEventDispatcher.cs        # Transport-agnostic dispatch interface
│
├── Transport/
│   ├── IRealtimeTransport.cs      # Transport implementation interface
│   └── TransportCapabilities.cs   # What the transport supports
│
└── Validation/
    ├── RealtimeValidationError.cs
    └── RealtimeValidationException.cs
```

### 2.2 Core DTOs

#### 2.2.1 RealtimeEvent (Event Envelope)

```csharp
/// <summary>
/// Envelope for all real-time events.
/// </summary>
public sealed class RealtimeEvent
{
    /// <summary>Unique event ID (for deduplication/ack).</summary>
    public string? Id { get; set; }
    
    /// <summary>Tenant isolation.</summary>
    public required string TenantId { get; set; }
    
    /// <summary>Event type (e.g., "message.received").</summary>
    public required string Type { get; set; }
    
    /// <summary>Event payload (serialized as JSON).</summary>
    public required object Payload { get; set; }
    
    /// <summary>Target for delivery.</summary>
    public required EventTarget Target { get; set; }
    
    /// <summary>When the event was created.</summary>
    public DateTimeOffset Timestamp { get; set; }
    
    /// <summary>Optional correlation ID for tracing.</summary>
    public string? CorrelationId { get; set; }
}
```

#### 2.2.2 EventTarget (Delivery Target)

```csharp
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
    
    // Factory methods for convenience
    public static EventTarget ToProfile(EntityRefDto profile) => 
        new() { Kind = TargetKind.Profile, Profiles = [profile] };
    
    public static EventTarget ToProfiles(IEnumerable<EntityRefDto> profiles) => 
        new() { Kind = TargetKind.Profiles, Profiles = profiles.ToList() };
    
    public static EventTarget ToConversation(string conversationId) => 
        new() { Kind = TargetKind.Conversation, ConversationId = conversationId };
    
    public static EventTarget ToGroup(string groupId) => 
        new() { Kind = TargetKind.Group, GroupId = groupId };
    
    public static EventTarget ToTenant() => 
        new() { Kind = TargetKind.Tenant };
    
    public static EventTarget ToConnection(string connectionId) => 
        new() { Kind = TargetKind.Connection, ConnectionId = connectionId };
}
```

#### 2.2.3 TargetKind Enum

```csharp
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
```

#### 2.2.4 ConnectionInfo

```csharp
/// <summary>
/// Metadata about an active connection.
/// </summary>
public sealed class ConnectionInfo
{
    /// <summary>Unique connection identifier.</summary>
    public required string ConnectionId { get; set; }
    
    /// <summary>Tenant this connection belongs to.</summary>
    public required string TenantId { get; set; }
    
    /// <summary>The connected profile.</summary>
    public required EntityRefDto Profile { get; set; }
    
    /// <summary>When the connection was established.</summary>
    public DateTimeOffset ConnectedAt { get; set; }
    
    /// <summary>Last activity timestamp (for idle detection).</summary>
    public DateTimeOffset LastActivityAt { get; set; }
    
    /// <summary>Transport type (SignalR, WebSocket, etc.).</summary>
    public string? TransportType { get; set; }
    
    /// <summary>Client-provided metadata (device, platform, etc.).</summary>
    public Dictionary<string, string>? Metadata { get; set; }
}
```

#### 2.2.5 PresenceStatus

```csharp
public enum PresenceStatus
{
    /// <summary>User has at least one active connection.</summary>
    Online,
    
    /// <summary>User has no active connections.</summary>
    Offline,
    
    /// <summary>User is connected but idle.</summary>
    Away
}
```

### 2.3 Core Interfaces

#### 2.3.1 IRealtimePublisher (Services Use This)

```csharp
/// <summary>
/// Main interface for publishing real-time events.
/// Services (Content, Chat, Inbox) inject this to push events.
/// </summary>
public interface IRealtimePublisher
{
    /// <summary>
    /// Publishes an event to the specified target.
    /// </summary>
    Task PublishAsync(RealtimeEvent evt, CancellationToken ct = default);
    
    /// <summary>
    /// Publishes multiple events atomically.
    /// </summary>
    Task PublishBatchAsync(IEnumerable<RealtimeEvent> events, CancellationToken ct = default);
    
    /// <summary>
    /// Convenience: Send to a single profile.
    /// </summary>
    Task SendToProfileAsync(
        string tenantId,
        EntityRefDto profile,
        string eventType,
        object payload,
        CancellationToken ct = default);
    
    /// <summary>
    /// Convenience: Send to multiple profiles.
    /// </summary>
    Task SendToProfilesAsync(
        string tenantId,
        IEnumerable<EntityRefDto> profiles,
        string eventType,
        object payload,
        CancellationToken ct = default);
    
    /// <summary>
    /// Convenience: Send to all conversation participants.
    /// </summary>
    Task SendToConversationAsync(
        string tenantId,
        string conversationId,
        string eventType,
        object payload,
        CancellationToken ct = default);
    
    /// <summary>
    /// Convenience: Send to all group members.
    /// </summary>
    Task SendToGroupAsync(
        string tenantId,
        string groupId,
        string eventType,
        object payload,
        CancellationToken ct = default);
}
```

#### 2.3.2 IConnectionManager

```csharp
/// <summary>
/// Manages connection lifecycle and mapping.
/// </summary>
public interface IConnectionManager
{
    /// <summary>
    /// Registers a new connection.
    /// </summary>
    Task<ConnectionInfo> AddConnectionAsync(
        string connectionId,
        string tenantId,
        EntityRefDto profile,
        Dictionary<string, string>? metadata = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Removes a connection (disconnect).
    /// </summary>
    Task RemoveConnectionAsync(string connectionId, CancellationToken ct = default);
    
    /// <summary>
    /// Updates last activity (for idle detection).
    /// </summary>
    Task TouchConnectionAsync(string connectionId, CancellationToken ct = default);
    
    /// <summary>
    /// Gets all connections for a profile.
    /// </summary>
    Task<IReadOnlyList<ConnectionInfo>> GetConnectionsForProfileAsync(
        string tenantId,
        EntityRefDto profile,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets connection info by ID.
    /// </summary>
    Task<ConnectionInfo?> GetConnectionAsync(string connectionId, CancellationToken ct = default);
    
    /// <summary>
    /// Gets all connections in a tenant.
    /// </summary>
    Task<IReadOnlyList<ConnectionInfo>> GetTenantConnectionsAsync(
        string tenantId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets connection count for a profile.
    /// </summary>
    Task<int> GetConnectionCountAsync(string tenantId, EntityRefDto profile, CancellationToken ct = default);
}
```

#### 2.3.3 IPresenceTracker

```csharp
/// <summary>
/// Tracks user online/offline presence.
/// </summary>
public interface IPresenceTracker
{
    /// <summary>
    /// Gets the presence status for a profile.
    /// </summary>
    Task<PresenceStatus> GetPresenceAsync(
        string tenantId,
        EntityRefDto profile,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets presence for multiple profiles.
    /// </summary>
    Task<IReadOnlyDictionary<string, PresenceStatus>> GetPresenceBatchAsync(
        string tenantId,
        IEnumerable<EntityRefDto> profiles,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets all online profiles in a tenant.
    /// </summary>
    Task<IReadOnlyList<EntityRefDto>> GetOnlineProfilesAsync(
        string tenantId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Subscribes to presence changes (for pushing updates).
    /// </summary>
    event EventHandler<PresenceChangedEventArgs>? PresenceChanged;
}

public sealed class PresenceChangedEventArgs : EventArgs
{
    public required string TenantId { get; set; }
    public required EntityRefDto Profile { get; set; }
    public required PresenceStatus OldStatus { get; set; }
    public required PresenceStatus NewStatus { get; set; }
}
```

#### 2.3.4 IEventDispatcher (Transport Uses This)

```csharp
/// <summary>
/// Internal interface for dispatching events to connections.
/// Transport implementations provide this.
/// </summary>
public interface IEventDispatcher
{
    /// <summary>
    /// Sends an event to specific connections.
    /// </summary>
    Task DispatchToConnectionsAsync(
        IEnumerable<string> connectionIds,
        RealtimeEvent evt,
        CancellationToken ct = default);
    
    /// <summary>
    /// Sends an event to a SignalR group (or equivalent).
    /// </summary>
    Task DispatchToGroupAsync(
        string groupName,
        RealtimeEvent evt,
        CancellationToken ct = default);
    
    /// <summary>
    /// Broadcast to all connections in tenant.
    /// </summary>
    Task DispatchToAllAsync(
        string tenantId,
        RealtimeEvent evt,
        CancellationToken ct = default);
}
```

#### 2.3.5 IGroupMembershipResolver

```csharp
/// <summary>
/// Resolves members for conversation/group targets.
/// Implement this to integrate with Chat/Groups services.
/// </summary>
public interface IGroupMembershipResolver
{
    /// <summary>
    /// Gets all profile IDs in a conversation.
    /// </summary>
    Task<IReadOnlyList<EntityRefDto>> GetConversationMembersAsync(
        string tenantId,
        string conversationId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets all profile IDs in a group.
    /// </summary>
    Task<IReadOnlyList<EntityRefDto>> GetGroupMembersAsync(
        string tenantId,
        string groupId,
        CancellationToken ct = default);
}
```

### 2.4 Event Type Constants

```csharp
/// <summary>
/// Standard event type constants.
/// </summary>
public static class RealtimeEventTypes
{
    // Activity events
    public const string ActivityCreated = "activity.created";
    
    // Inbox events
    public const string InboxItemCreated = "inbox.item.created";
    public const string InboxItemUpdated = "inbox.item.updated";
    public const string InboxItemRead = "inbox.item.read";
    
    // Content events
    public const string PostCreated = "post.created";
    public const string PostUpdated = "post.updated";
    public const string PostDeleted = "post.deleted";
    public const string CommentCreated = "comment.created";
    public const string CommentUpdated = "comment.updated";
    public const string CommentDeleted = "comment.deleted";
    public const string ReactionAdded = "reaction.added";
    public const string ReactionRemoved = "reaction.removed";
    
    // Chat events
    public const string MessageReceived = "message.received";
    public const string MessageEdited = "message.edited";
    public const string MessageDeleted = "message.deleted";
    public const string ConversationCreated = "conversation.created";
    
    // Presence events
    public const string PresenceOnline = "presence.online";
    public const string PresenceOffline = "presence.offline";
    public const string PresenceAway = "presence.away";
    
    // Typing events
    public const string TypingStarted = "typing.started";
    public const string TypingStopped = "typing.stopped";
}
```

### 2.5 Validation

```csharp
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
    ConnectionIdRequired
}

public sealed class RealtimeValidationException : Exception
{
    public RealtimeValidationError Error { get; }
    
    public RealtimeValidationException(RealtimeValidationError error)
        : base($"Realtime validation failed: {error}")
    {
        Error = error;
    }
}
```

---

## 3) Core Layer (`Realtime.Core`)

### 3.1 Project Structure

```
src/Realtime.Core/
├── Realtime.Core.csproj
├── README.md
│
├── RealtimePublisher.cs           # Main publisher implementation
├── ConnectionManagerCore.cs       # Connection orchestration
├── PresenceTrackerCore.cs         # Presence logic with events
├── EventValidator.cs              # Event validation
├── RealtimeServiceOptions.cs      # Configuration
└── NullGroupMembershipResolver.cs # Default no-op resolver
```

### 3.2 RealtimePublisher Implementation

```csharp
public sealed class RealtimePublisher : IRealtimePublisher
{
    private readonly IEventDispatcher _dispatcher;
    private readonly IConnectionManager _connections;
    private readonly IGroupMembershipResolver _membershipResolver;
    private readonly IIdGenerator _idGenerator;
    
    public RealtimePublisher(
        IEventDispatcher dispatcher,
        IConnectionManager connections,
        IGroupMembershipResolver membershipResolver,
        IIdGenerator idGenerator)
    {
        _dispatcher = dispatcher;
        _connections = connections;
        _membershipResolver = membershipResolver;
        _idGenerator = idGenerator;
    }
    
    public async Task PublishAsync(RealtimeEvent evt, CancellationToken ct = default)
    {
        EventValidator.Validate(evt);
        
        evt.Id ??= _idGenerator.NewId();
        evt.Timestamp = DateTimeOffset.UtcNow;
        
        // Resolve target to connection IDs
        var connectionIds = await ResolveTargetConnectionsAsync(evt.TenantId, evt.Target, ct);
        
        if (connectionIds.Count > 0)
        {
            await _dispatcher.DispatchToConnectionsAsync(connectionIds, evt, ct);
        }
    }
    
    private async Task<IReadOnlyList<string>> ResolveTargetConnectionsAsync(
        string tenantId,
        EventTarget target,
        CancellationToken ct)
    {
        return target.Kind switch
        {
            TargetKind.Profile => await GetProfileConnections(tenantId, target.Profiles!.First(), ct),
            TargetKind.Profiles => await GetMultiProfileConnections(tenantId, target.Profiles!, ct),
            TargetKind.Conversation => await GetConversationConnections(tenantId, target.ConversationId!, ct),
            TargetKind.Group => await GetGroupConnections(tenantId, target.GroupId!, ct),
            TargetKind.Tenant => await GetTenantConnections(tenantId, ct),
            TargetKind.Connection => [target.ConnectionId!],
            _ => []
        };
    }
    
    // Convenience methods delegate to PublishAsync...
}
```

### 3.3 ConnectionManagerCore

Wraps underlying store (from transport) and adds:
- Last activity tracking
- Connection metadata
- Tenant isolation validation
- Connection count limits (optional)

### 3.4 PresenceTrackerCore

Listens to connection add/remove and:
- Tracks first connection (Online) / last disconnect (Offline)
- Manages idle timeout → Away status
- Fires `PresenceChanged` events
- Optionally publishes presence events via `IRealtimePublisher`

---

## 4) In-Memory Transport (`Realtime.Transport.InMemory`)

### 4.1 Project Structure

```
src/Realtime.Transport.InMemory/
├── Realtime.Transport.InMemory.csproj
├── README.md
│
├── InMemoryConnectionStore.cs     # ConcurrentDictionary-based
├── InMemoryEventDispatcher.cs     # Collects events for testing
└── InMemoryRealtimeTransport.cs   # Bundles store + dispatcher
```

### 4.2 Purpose

- **Testing:** Verify event flow without real transport
- **Reference Implementation:** Shows what transports must implement
- **Event Collection:** Test assertions on published events

```csharp
public sealed class InMemoryEventDispatcher : IEventDispatcher
{
    private readonly ConcurrentQueue<(IEnumerable<string> Connections, RealtimeEvent Event)> _dispatched = new();
    
    public IReadOnlyList<(IEnumerable<string> Connections, RealtimeEvent Event)> DispatchedEvents => 
        _dispatched.ToList();
    
    public void Clear() => _dispatched.Clear();
    
    public Task DispatchToConnectionsAsync(
        IEnumerable<string> connectionIds,
        RealtimeEvent evt,
        CancellationToken ct = default)
    {
        _dispatched.Enqueue((connectionIds.ToList(), evt));
        return Task.CompletedTask;
    }
    
    // ... other dispatch methods
}
```

---

## 5) SignalR Transport (`Realtime.Transport.SignalR`) — Future

### 5.1 Project Structure

```
src/Realtime.Transport.SignalR/
├── Realtime.Transport.SignalR.csproj
├── README.md
│
├── SignalREventDispatcher.cs      # IEventDispatcher → IHubContext
├── SignalRConnectionManager.cs    # IConnectionManager → SignalR groups
├── RealtimeHub.cs                 # SignalR Hub implementation
└── ServiceCollectionExtensions.cs # DI registration
```

### 5.2 Hub Implementation (Outline)

```csharp
public class RealtimeHub : Hub
{
    private readonly IConnectionManager _connections;
    private readonly IPresenceTracker _presence;
    
    public override async Task OnConnectedAsync()
    {
        var tenantId = Context.GetHttpContext()?.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        var profileId = Context.User?.FindFirst("sub")?.Value;
        
        // Register connection
        await _connections.AddConnectionAsync(Context.ConnectionId, tenantId, profile);
        
        // Join tenant group for broadcasts
        await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant:{tenantId}");
        
        await base.OnConnectedAsync();
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await _connections.RemoveConnectionAsync(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
    
    // Client can call this to signal typing
    public async Task StartTyping(string conversationId)
    {
        // Publish typing.started to conversation
    }
    
    public async Task StopTyping(string conversationId)
    {
        // Publish typing.stopped to conversation
    }
}
```

---

## 6) Tests (`Realtime.Tests`)

### 6.1 Project Structure

```
tests/Realtime.Tests/
├── Realtime.Tests.csproj
├── PublisherTests.cs              # Event publishing scenarios
├── ConnectionManagerTests.cs      # Connection lifecycle
├── PresenceTrackerTests.cs        # Online/offline tracking
├── TargetResolutionTests.cs       # Target → connections mapping
└── ValidationTests.cs             # Input validation
```

### 6.2 Test Categories

1. **Publishing Tests**
   - Publish to single profile → correct connections receive event
   - Publish to multiple profiles → all connections receive event
   - Publish to conversation → members' connections receive event
   - Publish batch → all events dispatched atomically
   - Publish to disconnected profile → no error, zero dispatches

2. **Connection Tests**
   - Add connection → registered with correct metadata
   - Remove connection → no longer receives events
   - Multiple connections per profile → all receive events
   - Touch connection → last activity updated
   - Get connections by profile → returns correct list

3. **Presence Tests**
   - First connection → status becomes Online
   - All connections disconnect → status becomes Offline
   - Idle timeout → status becomes Away
   - PresenceChanged event fires correctly
   - Get presence batch → correct statuses

4. **Validation Tests**
   - Missing tenant → error
   - Missing event type → error
   - Invalid target → error

---

## 7) Directory Structure Summary

```
src/
├── Realtime.Abstractions/
│   ├── Realtime.Abstractions.csproj
│   ├── README.md
│   │
│   ├── Events/
│   │   ├── RealtimeEvent.cs
│   │   ├── RealtimeEventTypes.cs
│   │   └── PresenceStatus.cs
│   │
│   ├── Targeting/
│   │   ├── EventTarget.cs
│   │   ├── TargetKind.cs
│   │   └── EntityRefDto.cs
│   │
│   ├── Connection/
│   │   ├── ConnectionInfo.cs
│   │   ├── IConnectionManager.cs
│   │   └── IPresenceTracker.cs
│   │
│   ├── Publishing/
│   │   ├── IRealtimePublisher.cs
│   │   └── IEventDispatcher.cs
│   │
│   ├── Membership/
│   │   └── IGroupMembershipResolver.cs
│   │
│   └── Validation/
│       ├── RealtimeValidationError.cs
│       └── RealtimeValidationException.cs
│
├── Realtime.Core/
│   ├── Realtime.Core.csproj
│   ├── README.md
│   ├── RealtimePublisher.cs
│   ├── ConnectionManagerDecorator.cs
│   ├── PresenceTrackerCore.cs
│   ├── EventValidator.cs
│   ├── RealtimeServiceOptions.cs
│   ├── NullGroupMembershipResolver.cs
│   └── UlidIdGenerator.cs
│
├── Realtime.Transport.InMemory/
│   ├── Realtime.Transport.InMemory.csproj
│   ├── README.md
│   ├── InMemoryConnectionStore.cs
│   └── InMemoryEventDispatcher.cs
│
└── Realtime.Transport.SignalR/      # Phase 2 (optional)
    ├── Realtime.Transport.SignalR.csproj
    ├── README.md
    ├── SignalREventDispatcher.cs
    ├── SignalRConnectionManager.cs
    ├── RealtimeHub.cs
    └── ServiceCollectionExtensions.cs

tests/
└── Realtime.Tests/
    ├── Realtime.Tests.csproj
    ├── PublisherTests.cs
    ├── ConnectionManagerTests.cs
    ├── PresenceTrackerTests.cs
    ├── TargetResolutionTests.cs
    └── ValidationTests.cs
```

---

## 8) Implementation Order

### Phase 1: Core Abstractions & In-Memory (MVP)

| Step | Task | Est. Time |
|------|------|-----------|
| 1 | Create `Realtime.Abstractions` project with all DTOs and interfaces | 0.5 day |
| 2 | Create `Realtime.Core` with `RealtimePublisher`, `PresenceTrackerCore` | 0.5 day |
| 3 | Create `Realtime.Transport.InMemory` for testing | 0.25 day |
| 4 | Create `Realtime.Tests` with comprehensive tests | 0.25 day |

### Phase 2: SignalR Transport (Production)

| Step | Task | Est. Time |
|------|------|-----------|
| 5 | Create `Realtime.Transport.SignalR` with Hub | 0.5 day |
| 6 | Integration tests with actual SignalR | 0.5 day |

---

## 9) Integration with Existing Services

Once Realtime Hub is implemented, services integrate like this:

```csharp
// In ContentService after creating a post:
public async Task<PostDto> CreatePostAsync(CreatePostRequest request, CancellationToken ct)
{
    var post = // ... create post ...
    
    // Push realtime event to author's followers
    var followers = await _relationshipService.GetFollowersAsync(request.TenantId, request.Author, ct);
    
    await _realtimePublisher.SendToProfilesAsync(
        request.TenantId,
        followers,
        RealtimeEventTypes.PostCreated,
        post,
        ct);
    
    return post;
}
```

```csharp
// In ChatService after sending a message:
public async Task<MessageDto> SendMessageAsync(SendMessageRequest request, CancellationToken ct)
{
    var message = // ... create message ...
    
    // Push to all conversation participants
    await _realtimePublisher.SendToConversationAsync(
        request.TenantId,
        request.ConversationId,
        RealtimeEventTypes.MessageReceived,
        message,
        ct);
    
    return message;
}
```

---

## 10) Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| **Separate Abstractions from Transport** | Services only depend on `IRealtimePublisher`, not SignalR. Can swap transports. |
| **Event Envelope Pattern** | Consistent structure for all events. Easy to serialize, log, replay. |
| **Target Resolution in Core** | Core knows how to resolve profiles to connections; transport just delivers. |
| **Presence as Separate Interface** | Can be implemented differently (Redis, DB) without affecting publisher. |
| **In-Memory Transport for Tests** | Fast, deterministic testing without real WebSocket connections. |
| **IGroupMembershipResolver Plugin** | Decouples from Chat/Groups services; implement when those exist. |

---

## 11) Future Enhancements

- **Event Persistence:** Store events for offline delivery (queue + replay)
- **Acknowledgments:** Client acks, retry logic
- **Rate Limiting:** Prevent event flood
- **Batching:** Batch multiple events into single WebSocket frame
- **Filtering:** Client subscribes to specific event types
- **Metrics:** Connection counts, event rates, latency
