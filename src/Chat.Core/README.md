# Chat.Core

Core business logic for the Chat Service.

## Components

- **ChatService** — Main service implementation
- **ChatValidator** — Request validation
- **ChatNormalizer** — Text normalization for messages
- **UlidIdGenerator** — ULID-based ID generation

## Notifiers

- **NullChatNotifier** — No-op implementation for testing without real-time

## Usage

```csharp
var service = new ChatService(
    conversationStore,
    messageStore,
    new NullChatNotifier(),
    new UlidIdGenerator()
);

// Create a direct (1:1) conversation
var conv = await service.GetOrCreateDirectConversationAsync(
    tenantId: "tenant1",
    user1: EntityRefDto.Profile("user1", "Alice"),
    user2: EntityRefDto.Profile("user2", "Bob")
);

// Send a message
var msg = await service.SendMessageAsync(new SendMessageRequest
{
    TenantId = "tenant1",
    ConversationId = conv.Id!,
    Sender = EntityRefDto.Profile("user1", "Alice"),
    Body = "Hello!"
});
```

## Configuration

```csharp
var options = new ChatServiceOptions
{
    MaxMessageBodyLength = 10_000,
    MaxTitleLength = 100,
    MaxGroupParticipants = 100,
    EditWindowDuration = TimeSpan.FromMinutes(15)
};
```
