# Chat.Store.InMemory

In-memory implementation of chat stores for testing and development.

## Components

- **InMemoryConversationStore** — Thread-safe conversation storage
- **InMemoryMessageStore** — Thread-safe message storage

## Usage

```csharp
var conversationStore = new InMemoryConversationStore();
var messageStore = new InMemoryMessageStore();

var service = new ChatService(
    conversationStore,
    messageStore,
    new NullChatNotifier(),
    new UlidIdGenerator()
);
```

## Features

- Thread-safe with ReaderWriterLockSlim
- Deep cloning for immutability
- Composite keys for multi-tenant isolation
- Cursor-based pagination
- Per-user settings for archive/mute
