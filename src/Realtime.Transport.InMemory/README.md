# Realtime.Transport.InMemory

In-memory transport implementation for testing and development. Provides thread-safe connection management and event collection for test assertions.

## Key Types

### InMemoryConnectionManager

Thread-safe connection store using `ConcurrentDictionary`:

```csharp
var connectionManager = new InMemoryConnectionManager();

// Register a connection
await connectionManager.AddConnectionAsync(
    connectionId: "conn1",
    tenantId: "tenant1",
    profile: EntityRefDto.Profile("user123"));

// Query connections
var connections = await connectionManager.GetConnectionsForProfileAsync(
    "tenant1",
    EntityRefDto.Profile("user123"));

// Update activity
await connectionManager.TouchConnectionAsync("conn1");

// Remove connection
await connectionManager.RemoveConnectionAsync("conn1");
```

Fires events for presence integration:
- `ConnectionAdded` - when a new connection is registered
- `ConnectionRemoved` - when a connection is removed

### InMemoryEventDispatcher

Collects dispatched events for test assertions:

```csharp
var dispatcher = new InMemoryEventDispatcher(connectionManager);

// ... publish events via RealtimePublisher ...

// Assert on dispatched events
Assert.Single(dispatcher.DispatchedEvents);
Assert.Equal("message.received", dispatcher.DispatchedEvents[0].Event.Type);

// Query by event type
var messageEvents = dispatcher.GetEventsByType(RealtimeEventTypes.MessageReceived);

// Query by connection
var userEvents = dispatcher.GetEventsForConnection("conn1");

// Clear for next test
dispatcher.Clear();
```

## Usage in Tests

```csharp
public class MyServiceTests
{
    private InMemoryConnectionManager _connections;
    private InMemoryEventDispatcher _dispatcher;
    private RealtimePublisher _publisher;

    public MyServiceTests()
    {
        _connections = new InMemoryConnectionManager();
        _dispatcher = new InMemoryEventDispatcher(_connections);
        _publisher = new RealtimePublisher(
            _dispatcher,
            _connections,
            new NullGroupMembershipResolver(),
            new UlidIdGenerator());
    }

    [Fact]
    public async Task SendToProfile_DeliversToConnection()
    {
        // Arrange
        await _connections.AddConnectionAsync("conn1", "t1", EntityRefDto.Profile("user1"));

        // Act
        await _publisher.SendToProfileAsync("t1", EntityRefDto.Profile("user1"), "test.event", new { data = 123 });

        // Assert
        Assert.Single(_dispatcher.DispatchedEvents);
        Assert.Contains("conn1", _dispatcher.DispatchedEvents[0].ConnectionIds);
    }
}
```

## Dependencies

- `Realtime.Abstractions` - Interfaces and DTOs
