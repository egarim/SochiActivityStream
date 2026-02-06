# Chat Service — Implementation Plan

**Goal:** Build a pluggable real-time messaging system supporting direct (1:1) and group conversations with read receipts, typing indicators, and unread counts.

---

## Executive Summary

| Layer | Project | Purpose | Dependencies |
|-------|---------|---------|--------------|
| Abstractions | `Chat.Abstractions` | Pure interfaces, DTOs | None |
| Core | `Chat.Core` | Business logic, validation | Chat.Abstractions |
| Store | `Chat.Store.InMemory` | Reference implementation | Chat.Abstractions |

**Key Design Principle:** Chat Service is transport-agnostic. It handles conversation/message CRUD and state. Real-time delivery is delegated to `IRealtimePublisher` (from Realtime.Abstractions).

---

## 1) Core Concepts

### 1.1 Conversation Types

| Type | Description | Participants |
|------|-------------|--------------|
| **Direct** | 1:1 private chat | Exactly 2 |
| **Group** | Multi-user chat | 2+ (configurable max) |

### 1.2 Message Flow

```
┌──────────────┐     ┌──────────────┐     ┌──────────────────┐
│   Client     │────▶│ ChatService  │────▶│ IMessageStore    │
│  (send msg)  │     │              │     │ (persist)        │
└──────────────┘     └──────┬───────┘     └──────────────────┘
                           │
                           ▼
                    ┌──────────────────┐
                    │ IRealtimePublisher│
                    │ (push to clients) │
                    └──────────────────┘
```

### 1.3 Key Features

- **Conversations:** Create, list, leave, add/remove participants
- **Messages:** Send, edit, delete, reply-to (threading)
- **Read Receipts:** Track last-read message per participant
- **Unread Counts:** Denormalized per-participant counts
- **Soft Delete:** Messages hidden from deleter, visible to others until mutual delete
- **Typing Indicators:** Via Realtime Hub (not persisted)

### 1.4 Multi-Tenant Isolation

All entities scoped by `TenantId`.

---

## 2) Abstractions Layer (`Chat.Abstractions`)

### 2.1 Project Structure

```
src/Chat.Abstractions/
├── Chat.Abstractions.csproj
├── README.md
│
├── Conversations/
│   ├── ConversationDto.cs
│   ├── ConversationType.cs
│   ├── ConversationParticipantDto.cs
│   ├── ParticipantRole.cs
│   ├── CreateConversationRequest.cs
│   ├── UpdateConversationRequest.cs
│   └── ConversationQuery.cs
│
├── Messages/
│   ├── MessageDto.cs
│   ├── SendMessageRequest.cs
│   ├── EditMessageRequest.cs
│   ├── DeleteMessageRequest.cs
│   └── MessageQuery.cs
│
├── ReadReceipts/
│   ├── ReadReceiptDto.cs
│   └── MarkReadRequest.cs
│
├── Common/
│   ├── EntityRefDto.cs
│   ├── ChatPageResult.cs
│   └── IIdGenerator.cs
│
├── Services/
│   ├── IChatService.cs
│   └── IChatNotifier.cs
│
├── Stores/
│   ├── IConversationStore.cs
│   └── IMessageStore.cs
│
└── Validation/
    ├── ChatValidationError.cs
    └── ChatValidationException.cs
```

### 2.2 DTOs

#### 2.2.1 ConversationDto

```csharp
/// <summary>
/// Represents a chat conversation (direct or group).
/// </summary>
public sealed class ConversationDto
{
    /// <summary>Unique identifier.</summary>
    public string? Id { get; set; }

    /// <summary>Tenant isolation.</summary>
    public required string TenantId { get; set; }

    /// <summary>Direct (1:1) or Group.</summary>
    public ConversationType Type { get; set; }

    /// <summary>Display title (group chats only, null for direct).</summary>
    public string? Title { get; set; }

    /// <summary>Group avatar URL (optional).</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>Conversation participants.</summary>
    public List<ConversationParticipantDto> Participants { get; set; } = [];

    /// <summary>Most recent message (for conversation list display).</summary>
    public MessageDto? LastMessage { get; set; }

    /// <summary>When the conversation was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Last activity (message sent, participant change).</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Whether the conversation is archived by the viewer.</summary>
    public bool IsArchived { get; set; }

    /// <summary>Whether the conversation is muted by the viewer.</summary>
    public bool IsMuted { get; set; }

    // Per-viewer fields (populated based on viewer context)
    
    /// <summary>Unread message count for the current viewer.</summary>
    public int UnreadCount { get; set; }
}
```

#### 2.2.2 ConversationType

```csharp
public enum ConversationType
{
    /// <summary>1:1 private conversation.</summary>
    Direct = 0,

    /// <summary>Multi-user group conversation.</summary>
    Group = 1
}
```

#### 2.2.3 ConversationParticipantDto

```csharp
/// <summary>
/// A participant in a conversation.
/// </summary>
public sealed class ConversationParticipantDto
{
    /// <summary>The participant's profile.</summary>
    public required EntityRefDto Profile { get; set; }

    /// <summary>Role in the conversation (member, admin).</summary>
    public ParticipantRole Role { get; set; }

    /// <summary>When the participant joined.</summary>
    public DateTimeOffset JoinedAt { get; set; }

    /// <summary>Last time the participant read messages.</summary>
    public DateTimeOffset? LastReadAt { get; set; }

    /// <summary>ID of the last message the participant read.</summary>
    public string? LastReadMessageId { get; set; }

    /// <summary>Whether this participant has left the conversation.</summary>
    public bool HasLeft { get; set; }

    /// <summary>When the participant left (if applicable).</summary>
    public DateTimeOffset? LeftAt { get; set; }
}
```

#### 2.2.4 ParticipantRole

```csharp
public enum ParticipantRole
{
    /// <summary>Regular participant.</summary>
    Member = 0,

    /// <summary>Can manage participants and settings.</summary>
    Admin = 1,

    /// <summary>Creator of the conversation (cannot be removed).</summary>
    Owner = 2
}
```

#### 2.2.5 MessageDto

```csharp
/// <summary>
/// A message within a conversation.
/// </summary>
public sealed class MessageDto
{
    /// <summary>Unique identifier.</summary>
    public string? Id { get; set; }

    /// <summary>Tenant isolation.</summary>
    public required string TenantId { get; set; }

    /// <summary>Parent conversation ID.</summary>
    public required string ConversationId { get; set; }

    /// <summary>Message author.</summary>
    public required EntityRefDto Sender { get; set; }

    /// <summary>Message content (text).</summary>
    public required string Body { get; set; }

    /// <summary>Attached media (images, files).</summary>
    public List<MediaRefDto>? Media { get; set; }

    /// <summary>Message this is replying to (quote reply).</summary>
    public string? ReplyToMessageId { get; set; }

    /// <summary>Populated ReplyTo message (for display).</summary>
    public MessageDto? ReplyTo { get; set; }

    /// <summary>When the message was sent.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>When the message was last edited.</summary>
    public DateTimeOffset? EditedAt { get; set; }

    /// <summary>Whether the message has been deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Who deleted this message (for partial delete tracking).</summary>
    public List<string>? DeletedByProfileIds { get; set; }

    /// <summary>System message type (null for regular messages).</summary>
    public SystemMessageType? SystemMessageType { get; set; }
}
```

#### 2.2.6 SystemMessageType

```csharp
/// <summary>
/// Types of system-generated messages.
/// </summary>
public enum SystemMessageType
{
    /// <summary>Participant joined the conversation.</summary>
    ParticipantJoined,

    /// <summary>Participant left the conversation.</summary>
    ParticipantLeft,

    /// <summary>Participant was removed.</summary>
    ParticipantRemoved,

    /// <summary>Conversation title changed.</summary>
    TitleChanged,

    /// <summary>Conversation avatar changed.</summary>
    AvatarChanged
}
```

#### 2.2.7 MediaRefDto

```csharp
/// <summary>
/// Reference to attached media.
/// </summary>
public sealed class MediaRefDto
{
    /// <summary>Media ID (from Media Service).</summary>
    public required string Id { get; set; }

    /// <summary>Media type.</summary>
    public required MediaType Type { get; set; }

    /// <summary>URL to access the media.</summary>
    public required string Url { get; set; }

    /// <summary>Thumbnail URL (for images/videos).</summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>Original filename.</summary>
    public string? FileName { get; set; }

    /// <summary>File size in bytes.</summary>
    public long? SizeBytes { get; set; }

    /// <summary>MIME type.</summary>
    public string? ContentType { get; set; }
}

public enum MediaType
{
    Image = 1,
    Video = 2,
    Audio = 3,
    File = 4
}
```

#### 2.2.8 ReadReceiptDto

```csharp
/// <summary>
/// Read receipt for a participant.
/// </summary>
public sealed class ReadReceiptDto
{
    /// <summary>The participant who read.</summary>
    public required EntityRefDto Profile { get; set; }

    /// <summary>Last message ID they read up to.</summary>
    public required string LastReadMessageId { get; set; }

    /// <summary>When they read it.</summary>
    public DateTimeOffset ReadAt { get; set; }
}
```

### 2.3 Request Types

#### 2.3.1 Conversation Requests

```csharp
/// <summary>
/// Request to create a new conversation.
/// </summary>
public sealed class CreateConversationRequest
{
    /// <summary>Tenant isolation.</summary>
    public required string TenantId { get; set; }

    /// <summary>User creating the conversation.</summary>
    public required EntityRefDto Creator { get; set; }

    /// <summary>Direct or Group.</summary>
    public ConversationType Type { get; set; }

    /// <summary>Initial participants (including creator for group).</summary>
    public required List<EntityRefDto> Participants { get; set; }

    /// <summary>Group title (required for group, ignored for direct).</summary>
    public string? Title { get; set; }
}

/// <summary>
/// Request to update conversation settings.
/// </summary>
public sealed class UpdateConversationRequest
{
    public required string TenantId { get; set; }
    public required string ConversationId { get; set; }
    public required EntityRefDto Actor { get; set; }
    public string? Title { get; set; }
    public string? AvatarUrl { get; set; }
}

/// <summary>
/// Request to add a participant.
/// </summary>
public sealed class AddParticipantRequest
{
    public required string TenantId { get; set; }
    public required string ConversationId { get; set; }
    public required EntityRefDto Actor { get; set; }
    public required EntityRefDto NewParticipant { get; set; }
}

/// <summary>
/// Request to remove a participant.
/// </summary>
public sealed class RemoveParticipantRequest
{
    public required string TenantId { get; set; }
    public required string ConversationId { get; set; }
    public required EntityRefDto Actor { get; set; }
    public required EntityRefDto Participant { get; set; }
}
```

#### 2.3.2 Message Requests

```csharp
/// <summary>
/// Request to send a message.
/// </summary>
public sealed class SendMessageRequest
{
    public required string TenantId { get; set; }
    public required string ConversationId { get; set; }
    public required EntityRefDto Sender { get; set; }
    public required string Body { get; set; }
    public List<MediaRefDto>? Media { get; set; }
    public string? ReplyToMessageId { get; set; }
}

/// <summary>
/// Request to edit a message.
/// </summary>
public sealed class EditMessageRequest
{
    public required string TenantId { get; set; }
    public required string ConversationId { get; set; }
    public required string MessageId { get; set; }
    public required EntityRefDto Actor { get; set; }
    public required string Body { get; set; }
}

/// <summary>
/// Request to delete a message.
/// </summary>
public sealed class DeleteMessageRequest
{
    public required string TenantId { get; set; }
    public required string ConversationId { get; set; }
    public required string MessageId { get; set; }
    public required EntityRefDto Actor { get; set; }
    
    /// <summary>If true, delete for everyone (sender only). Otherwise, delete for self.</summary>
    public bool DeleteForEveryone { get; set; }
}

/// <summary>
/// Request to mark messages as read.
/// </summary>
public sealed class MarkReadRequest
{
    public required string TenantId { get; set; }
    public required string ConversationId { get; set; }
    public required EntityRefDto Profile { get; set; }
    public required string MessageId { get; set; }
}
```

### 2.4 Query Types

```csharp
/// <summary>
/// Query for listing conversations.
/// </summary>
public sealed class ConversationQuery
{
    public required string TenantId { get; set; }
    
    /// <summary>Filter to conversations this profile is a participant of.</summary>
    public required EntityRefDto Participant { get; set; }
    
    /// <summary>Filter by conversation type.</summary>
    public ConversationType? Type { get; set; }
    
    /// <summary>Include archived conversations.</summary>
    public bool IncludeArchived { get; set; }
    
    /// <summary>Cursor for pagination.</summary>
    public string? Cursor { get; set; }
    
    /// <summary>Page size.</summary>
    public int Limit { get; set; } = 20;
}

/// <summary>
/// Query for listing messages.
/// </summary>
public sealed class MessageQuery
{
    public required string TenantId { get; set; }
    public required string ConversationId { get; set; }
    
    /// <summary>Viewer context (for delete filtering).</summary>
    public required EntityRefDto Viewer { get; set; }
    
    /// <summary>Cursor for pagination (message ID).</summary>
    public string? Cursor { get; set; }
    
    /// <summary>Direction: newer or older than cursor.</summary>
    public MessageQueryDirection Direction { get; set; } = MessageQueryDirection.Older;
    
    /// <summary>Page size.</summary>
    public int Limit { get; set; } = 50;
}

public enum MessageQueryDirection
{
    /// <summary>Get messages older than cursor (scrolling up).</summary>
    Older = 0,
    
    /// <summary>Get messages newer than cursor (new messages).</summary>
    Newer = 1
}
```

### 2.5 Page Result

```csharp
/// <summary>
/// Paginated result for chat queries.
/// </summary>
public sealed class ChatPageResult<T>
{
    public List<T> Items { get; set; } = [];
    public string? NextCursor { get; set; }
    public bool HasMore { get; set; }
}
```

### 2.6 Interfaces

#### 2.6.1 IChatService

```csharp
/// <summary>
/// Main chat service interface.
/// </summary>
public interface IChatService
{
    // ─────────────────────────────────────────────────────────────────
    // Conversations
    // ─────────────────────────────────────────────────────────────────
    
    /// <summary>
    /// Gets or creates a direct (1:1) conversation between two users.
    /// Returns existing conversation if one exists.
    /// </summary>
    Task<ConversationDto> GetOrCreateDirectConversationAsync(
        string tenantId,
        EntityRefDto user1,
        EntityRefDto user2,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a new group conversation.
    /// </summary>
    Task<ConversationDto> CreateGroupConversationAsync(
        CreateConversationRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a conversation by ID.
    /// </summary>
    Task<ConversationDto?> GetConversationAsync(
        string tenantId,
        string conversationId,
        EntityRefDto viewer,
        CancellationToken ct = default);

    /// <summary>
    /// Lists conversations for a participant.
    /// </summary>
    Task<ChatPageResult<ConversationDto>> GetConversationsAsync(
        ConversationQuery query,
        CancellationToken ct = default);

    /// <summary>
    /// Updates conversation settings (title, avatar).
    /// </summary>
    Task<ConversationDto> UpdateConversationAsync(
        UpdateConversationRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Adds a participant to a group conversation.
    /// </summary>
    Task AddParticipantAsync(
        AddParticipantRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Removes a participant from a group conversation.
    /// </summary>
    Task RemoveParticipantAsync(
        RemoveParticipantRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Current user leaves a group conversation.
    /// </summary>
    Task LeaveConversationAsync(
        string tenantId,
        string conversationId,
        EntityRefDto participant,
        CancellationToken ct = default);

    /// <summary>
    /// Archives/unarchives a conversation for a user.
    /// </summary>
    Task SetArchivedAsync(
        string tenantId,
        string conversationId,
        EntityRefDto participant,
        bool archived,
        CancellationToken ct = default);

    /// <summary>
    /// Mutes/unmutes a conversation for a user.
    /// </summary>
    Task SetMutedAsync(
        string tenantId,
        string conversationId,
        EntityRefDto participant,
        bool muted,
        CancellationToken ct = default);

    // ─────────────────────────────────────────────────────────────────
    // Messages
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sends a message to a conversation.
    /// </summary>
    Task<MessageDto> SendMessageAsync(
        SendMessageRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Edits an existing message (sender only).
    /// </summary>
    Task<MessageDto> EditMessageAsync(
        EditMessageRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a message.
    /// </summary>
    Task DeleteMessageAsync(
        DeleteMessageRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Gets messages in a conversation.
    /// </summary>
    Task<ChatPageResult<MessageDto>> GetMessagesAsync(
        MessageQuery query,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a specific message by ID.
    /// </summary>
    Task<MessageDto?> GetMessageAsync(
        string tenantId,
        string conversationId,
        string messageId,
        EntityRefDto viewer,
        CancellationToken ct = default);

    // ─────────────────────────────────────────────────────────────────
    // Read Receipts
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Marks messages as read up to a given message.
    /// </summary>
    Task MarkReadAsync(
        MarkReadRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Gets total unread count across all conversations for a user.
    /// </summary>
    Task<int> GetTotalUnreadCountAsync(
        string tenantId,
        EntityRefDto participant,
        CancellationToken ct = default);

    /// <summary>
    /// Gets read receipts for a message.
    /// </summary>
    Task<IReadOnlyList<ReadReceiptDto>> GetReadReceiptsAsync(
        string tenantId,
        string conversationId,
        string messageId,
        CancellationToken ct = default);
}
```

#### 2.6.2 IChatNotifier

```csharp
/// <summary>
/// Interface for pushing real-time chat events.
/// Implemented by integrating with IRealtimePublisher.
/// </summary>
public interface IChatNotifier
{
    /// <summary>
    /// Notifies participants of a new message.
    /// </summary>
    Task NotifyMessageReceivedAsync(
        ConversationDto conversation,
        MessageDto message,
        CancellationToken ct = default);

    /// <summary>
    /// Notifies participants of a message edit.
    /// </summary>
    Task NotifyMessageEditedAsync(
        ConversationDto conversation,
        MessageDto message,
        CancellationToken ct = default);

    /// <summary>
    /// Notifies participants of a message deletion.
    /// </summary>
    Task NotifyMessageDeletedAsync(
        string tenantId,
        string conversationId,
        string messageId,
        CancellationToken ct = default);

    /// <summary>
    /// Notifies of read receipt update.
    /// </summary>
    Task NotifyReadReceiptAsync(
        string tenantId,
        string conversationId,
        ReadReceiptDto receipt,
        CancellationToken ct = default);

    /// <summary>
    /// Notifies of typing indicator.
    /// </summary>
    Task NotifyTypingAsync(
        string tenantId,
        string conversationId,
        EntityRefDto profile,
        bool isTyping,
        CancellationToken ct = default);

    /// <summary>
    /// Notifies of conversation update (title, avatar, participants).
    /// </summary>
    Task NotifyConversationUpdatedAsync(
        ConversationDto conversation,
        CancellationToken ct = default);
}
```

#### 2.6.3 IConversationStore

```csharp
/// <summary>
/// Storage interface for conversations.
/// </summary>
public interface IConversationStore
{
    Task<ConversationDto> UpsertAsync(
        ConversationDto conversation,
        CancellationToken ct = default);

    Task<ConversationDto?> GetByIdAsync(
        string tenantId,
        string conversationId,
        CancellationToken ct = default);

    /// <summary>
    /// Finds existing direct conversation between two users.
    /// </summary>
    Task<ConversationDto?> FindDirectConversationAsync(
        string tenantId,
        string profileId1,
        string profileId2,
        CancellationToken ct = default);

    Task<ChatPageResult<ConversationDto>> QueryAsync(
        ConversationQuery query,
        CancellationToken ct = default);

    Task DeleteAsync(
        string tenantId,
        string conversationId,
        CancellationToken ct = default);

    // Participant management
    Task UpdateParticipantAsync(
        string tenantId,
        string conversationId,
        ConversationParticipantDto participant,
        CancellationToken ct = default);

    Task<ConversationParticipantDto?> GetParticipantAsync(
        string tenantId,
        string conversationId,
        string profileId,
        CancellationToken ct = default);

    // Per-user settings
    Task SetUserSettingsAsync(
        string tenantId,
        string conversationId,
        string profileId,
        bool? isArchived,
        bool? isMuted,
        CancellationToken ct = default);
}
```

#### 2.6.4 IMessageStore

```csharp
/// <summary>
/// Storage interface for messages.
/// </summary>
public interface IMessageStore
{
    Task<MessageDto> UpsertAsync(
        MessageDto message,
        CancellationToken ct = default);

    Task<MessageDto?> GetByIdAsync(
        string tenantId,
        string conversationId,
        string messageId,
        CancellationToken ct = default);

    Task<ChatPageResult<MessageDto>> QueryAsync(
        MessageQuery query,
        CancellationToken ct = default);

    /// <summary>
    /// Soft delete (add profile to DeletedByProfileIds).
    /// </summary>
    Task SoftDeleteAsync(
        string tenantId,
        string conversationId,
        string messageId,
        string profileId,
        CancellationToken ct = default);

    /// <summary>
    /// Hard delete (for delete-for-everyone).
    /// </summary>
    Task HardDeleteAsync(
        string tenantId,
        string conversationId,
        string messageId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the latest message in a conversation.
    /// </summary>
    Task<MessageDto?> GetLatestMessageAsync(
        string tenantId,
        string conversationId,
        CancellationToken ct = default);

    /// <summary>
    /// Counts messages after a given message ID (for unread count).
    /// </summary>
    Task<int> CountMessagesAfterAsync(
        string tenantId,
        string conversationId,
        string? afterMessageId,
        string excludeProfileId,
        CancellationToken ct = default);
}
```

### 2.7 Validation

```csharp
public enum ChatValidationError
{
    None,
    TenantIdRequired,
    ConversationIdRequired,
    MessageIdRequired,
    SenderRequired,
    BodyRequired,
    BodyTooLong,
    ParticipantsRequired,
    TooFewParticipants,
    TooManyParticipants,
    TitleRequired,
    TitleTooLong,
    NotParticipant,
    NotAuthorized,
    ConversationNotFound,
    MessageNotFound,
    CannotEditOthersMessage,
    CannotDeleteForEveryoneOthersMessage,
    DirectConversationCannotAddParticipants,
    DirectConversationCannotRemoveParticipants,
    CannotRemoveOwner,
    InvalidReplyToMessage
}

public sealed class ChatValidationException : Exception
{
    public ChatValidationError Error { get; }

    public ChatValidationException(ChatValidationError error)
        : base($"Chat validation failed: {error}")
    {
        Error = error;
    }
}
```

### 2.8 EntityRefDto

```csharp
/// <summary>
/// Reference to a profile/user entity.
/// </summary>
public sealed class EntityRefDto
{
    public required string Id { get; set; }
    public required string Type { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }

    public static EntityRefDto Profile(string id, string? displayName = null) =>
        new() { Id = id, Type = "profile", DisplayName = displayName };

    public override bool Equals(object? obj) =>
        obj is EntityRefDto other && Id == other.Id && Type == other.Type;

    public override int GetHashCode() => HashCode.Combine(Id, Type);
}
```

---

## 3) Core Layer (`Chat.Core`)

### 3.1 Project Structure

```
src/Chat.Core/
├── Chat.Core.csproj
├── README.md
│
├── ChatService.cs              # Main service implementation
├── ChatValidator.cs            # Request validation
├── ChatNormalizer.cs           # Text normalization
├── ChatServiceOptions.cs       # Configuration
├── UlidIdGenerator.cs          # ID generation
│
├── Notifiers/
│   ├── RealtimeChatNotifier.cs # IRealtimePublisher integration
│   └── NullChatNotifier.cs     # No-op for testing
│
└── Helpers/
    └── UnreadCountHelper.cs    # Unread calculation logic
```

### 3.2 ChatServiceOptions

```csharp
public sealed class ChatServiceOptions
{
    /// <summary>Maximum message body length.</summary>
    public int MaxMessageBodyLength { get; set; } = 10_000;

    /// <summary>Maximum group title length.</summary>
    public int MaxTitleLength { get; set; } = 100;

    /// <summary>Maximum participants in a group.</summary>
    public int MaxGroupParticipants { get; set; } = 100;

    /// <summary>Minimum participants to create a group.</summary>
    public int MinGroupParticipants { get; set; } = 2;

    /// <summary>Default page size for queries.</summary>
    public int DefaultPageSize { get; set; } = 50;

    /// <summary>Maximum page size.</summary>
    public int MaxPageSize { get; set; } = 100;

    /// <summary>Time window for editing messages (0 = unlimited).</summary>
    public TimeSpan EditWindowDuration { get; set; } = TimeSpan.Zero;
}
```

### 3.3 ChatService Implementation (Key Methods)

```csharp
public sealed class ChatService : IChatService
{
    private readonly IConversationStore _conversationStore;
    private readonly IMessageStore _messageStore;
    private readonly IChatNotifier _notifier;
    private readonly IIdGenerator _idGenerator;
    private readonly ChatServiceOptions _options;

    public ChatService(
        IConversationStore conversationStore,
        IMessageStore messageStore,
        IChatNotifier notifier,
        IIdGenerator idGenerator,
        ChatServiceOptions? options = null)
    {
        _conversationStore = conversationStore;
        _messageStore = messageStore;
        _notifier = notifier;
        _idGenerator = idGenerator;
        _options = options ?? new ChatServiceOptions();
    }

    public async Task<ConversationDto> GetOrCreateDirectConversationAsync(
        string tenantId,
        EntityRefDto user1,
        EntityRefDto user2,
        CancellationToken ct = default)
    {
        // Check for existing
        var existing = await _conversationStore.FindDirectConversationAsync(
            tenantId, user1.Id, user2.Id, ct);
        
        if (existing != null)
            return existing;

        // Create new
        var now = DateTimeOffset.UtcNow;
        var conversation = new ConversationDto
        {
            Id = _idGenerator.NewId(),
            TenantId = tenantId,
            Type = ConversationType.Direct,
            Participants =
            [
                new() { Profile = user1, Role = ParticipantRole.Member, JoinedAt = now },
                new() { Profile = user2, Role = ParticipantRole.Member, JoinedAt = now }
            ],
            CreatedAt = now,
            UpdatedAt = now
        };

        return await _conversationStore.UpsertAsync(conversation, ct);
    }

    public async Task<MessageDto> SendMessageAsync(
        SendMessageRequest request,
        CancellationToken ct = default)
    {
        ChatValidator.ValidateSendMessage(request, _options);

        // Verify sender is participant
        var conversation = await GetAndVerifyParticipantAsync(
            request.TenantId, request.ConversationId, request.Sender, ct);

        var now = DateTimeOffset.UtcNow;
        var message = new MessageDto
        {
            Id = _idGenerator.NewId(),
            TenantId = request.TenantId,
            ConversationId = request.ConversationId,
            Sender = request.Sender,
            Body = ChatNormalizer.NormalizeBody(request.Body),
            Media = request.Media,
            ReplyToMessageId = request.ReplyToMessageId,
            CreatedAt = now
        };

        // Validate reply-to if specified
        if (request.ReplyToMessageId != null)
        {
            var replyTo = await _messageStore.GetByIdAsync(
                request.TenantId, request.ConversationId, request.ReplyToMessageId, ct);
            if (replyTo == null)
                throw new ChatValidationException(ChatValidationError.InvalidReplyToMessage);
        }

        // Save message
        message = await _messageStore.UpsertAsync(message, ct);

        // Update conversation's UpdatedAt and LastMessage
        conversation.UpdatedAt = now;
        conversation.LastMessage = message;
        await _conversationStore.UpsertAsync(conversation, ct);

        // Mark as read for sender
        await MarkReadAsync(new MarkReadRequest
        {
            TenantId = request.TenantId,
            ConversationId = request.ConversationId,
            Profile = request.Sender,
            MessageId = message.Id!
        }, ct);

        // Notify other participants
        await _notifier.NotifyMessageReceivedAsync(conversation, message, ct);

        return message;
    }

    // ... other methods
}
```

### 3.4 RealtimeChatNotifier

```csharp
/// <summary>
/// Integrates with IRealtimePublisher to push chat events.
/// </summary>
public sealed class RealtimeChatNotifier : IChatNotifier
{
    private readonly IRealtimePublisher _publisher;

    public RealtimeChatNotifier(IRealtimePublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task NotifyMessageReceivedAsync(
        ConversationDto conversation,
        MessageDto message,
        CancellationToken ct = default)
    {
        // Get participant profiles (excluding sender)
        var recipients = conversation.Participants
            .Where(p => p.Profile.Id != message.Sender.Id && !p.HasLeft)
            .Select(p => p.Profile)
            .ToList();

        if (recipients.Count > 0)
        {
            await _publisher.SendToProfilesAsync(
                conversation.TenantId,
                recipients,
                "message.received",
                new { Conversation = conversation, Message = message },
                ct);
        }
    }

    public async Task NotifyTypingAsync(
        string tenantId,
        string conversationId,
        EntityRefDto profile,
        bool isTyping,
        CancellationToken ct = default)
    {
        await _publisher.SendToConversationAsync(
            tenantId,
            conversationId,
            isTyping ? "typing.started" : "typing.stopped",
            new { ConversationId = conversationId, Profile = profile },
            ct);
    }

    // ... other methods
}
```

---

## 4) In-Memory Store (`Chat.Store.InMemory`)

### 4.1 Project Structure

```
src/Chat.Store.InMemory/
├── Chat.Store.InMemory.csproj
├── README.md
├── InMemoryConversationStore.cs
└── InMemoryMessageStore.cs
```

### 4.2 InMemoryConversationStore

```csharp
public sealed class InMemoryConversationStore : IConversationStore
{
    private readonly ConcurrentDictionary<string, ConversationDto> _conversations = new();
    private readonly ReaderWriterLockSlim _lock = new();

    // Use composite keys: "{tenantId}|{conversationId}"
    private static string Key(string tenantId, string id) => $"{tenantId}|{id}";

    public Task<ConversationDto?> FindDirectConversationAsync(
        string tenantId,
        string profileId1,
        string profileId2,
        CancellationToken ct = default)
    {
        _lock.EnterReadLock();
        try
        {
            var match = _conversations.Values
                .Where(c => c.TenantId == tenantId && c.Type == ConversationType.Direct)
                .FirstOrDefault(c =>
                {
                    var ids = c.Participants.Select(p => p.Profile.Id).ToHashSet();
                    return ids.Contains(profileId1) && ids.Contains(profileId2);
                });

            return Task.FromResult(match is null ? null : Clone(match));
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    // ... other methods with cloning for immutability
}
```

### 4.3 InMemoryMessageStore

```csharp
public sealed class InMemoryMessageStore : IMessageStore
{
    private readonly ConcurrentDictionary<string, MessageDto> _messages = new();
    private readonly ReaderWriterLockSlim _lock = new();

    // Key: "{tenantId}|{conversationId}|{messageId}"
    private static string Key(string tenantId, string convId, string msgId) =>
        $"{tenantId}|{convId}|{msgId}";

    public Task<ChatPageResult<MessageDto>> QueryAsync(
        MessageQuery query,
        CancellationToken ct = default)
    {
        _lock.EnterReadLock();
        try
        {
            var messages = _messages.Values
                .Where(m => m.TenantId == query.TenantId &&
                           m.ConversationId == query.ConversationId &&
                           !m.IsDeleted &&
                           !(m.DeletedByProfileIds?.Contains(query.Viewer.Id) ?? false))
                .OrderByDescending(m => m.CreatedAt)
                .ToList();

            // Apply cursor and direction...
            // Return paginated result

            return Task.FromResult(new ChatPageResult<MessageDto>
            {
                Items = messages.Take(query.Limit).Select(Clone).ToList(),
                HasMore = messages.Count > query.Limit
            });
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    // ... other methods
}
```

---

## 5) Tests (`Chat.Tests`)

### 5.1 Project Structure

```
tests/Chat.Tests/
├── Chat.Tests.csproj
├── ConversationTests.cs      # Create, get, update, participants
├── MessageTests.cs           # Send, edit, delete, query
├── ReadReceiptTests.cs       # Mark read, unread counts
├── DirectConversationTests.cs # 1:1 specific behavior
├── GroupConversationTests.cs  # Group-specific behavior
└── ValidationTests.cs        # Input validation
```

### 5.2 Test Categories

1. **Conversation Tests**
   - Create direct conversation → returns conversation
   - Get or create direct → returns existing if exists
   - Create group conversation → returns with all participants
   - Add participant to group → participant added
   - Remove participant from group → participant marked as left
   - Leave conversation → current user marked as left
   - Cannot add/remove participants in direct conversation

2. **Message Tests**
   - Send message → message saved with ID and timestamp
   - Send message → conversation UpdatedAt updated
   - Send message → notifier called for other participants
   - Edit message → body updated, EditedAt set
   - Edit message → only sender can edit
   - Delete for self → message hidden from deleter only
   - Delete for everyone → message marked as deleted
   - Delete for everyone → only sender can
   - Reply to message → ReplyToMessageId set
   - Query messages → paginated, ordered by time

3. **Read Receipt Tests**
   - Mark read → LastReadMessageId updated
   - Mark read → unread count updated
   - Get unread count → correct count returned
   - Total unread → sum across all conversations

4. **Validation Tests**
   - Empty body → error
   - Body too long → error
   - Not participant → error
   - Edit window expired → error (if configured)

---

## 6) Directory Structure Summary

```
src/
├── Chat.Abstractions/
│   ├── Chat.Abstractions.csproj
│   ├── README.md
│   ├── Conversations/
│   │   ├── ConversationDto.cs
│   │   ├── ConversationType.cs
│   │   ├── ConversationParticipantDto.cs
│   │   ├── ParticipantRole.cs
│   │   ├── CreateConversationRequest.cs
│   │   ├── UpdateConversationRequest.cs
│   │   ├── AddParticipantRequest.cs
│   │   ├── RemoveParticipantRequest.cs
│   │   └── ConversationQuery.cs
│   ├── Messages/
│   │   ├── MessageDto.cs
│   │   ├── MediaRefDto.cs
│   │   ├── MediaType.cs
│   │   ├── SystemMessageType.cs
│   │   ├── SendMessageRequest.cs
│   │   ├── EditMessageRequest.cs
│   │   ├── DeleteMessageRequest.cs
│   │   └── MessageQuery.cs
│   ├── ReadReceipts/
│   │   ├── ReadReceiptDto.cs
│   │   └── MarkReadRequest.cs
│   ├── Common/
│   │   ├── EntityRefDto.cs
│   │   ├── ChatPageResult.cs
│   │   └── IIdGenerator.cs
│   ├── Services/
│   │   ├── IChatService.cs
│   │   └── IChatNotifier.cs
│   ├── Stores/
│   │   ├── IConversationStore.cs
│   │   └── IMessageStore.cs
│   └── Validation/
│       ├── ChatValidationError.cs
│       └── ChatValidationException.cs
│
├── Chat.Core/
│   ├── Chat.Core.csproj
│   ├── README.md
│   ├── ChatService.cs
│   ├── ChatValidator.cs
│   ├── ChatNormalizer.cs
│   ├── ChatServiceOptions.cs
│   ├── UlidIdGenerator.cs
│   └── Notifiers/
│       ├── RealtimeChatNotifier.cs
│       └── NullChatNotifier.cs
│
└── Chat.Store.InMemory/
    ├── Chat.Store.InMemory.csproj
    ├── README.md
    ├── InMemoryConversationStore.cs
    └── InMemoryMessageStore.cs

tests/
└── Chat.Tests/
    ├── Chat.Tests.csproj
    ├── ConversationTests.cs
    ├── MessageTests.cs
    ├── ReadReceiptTests.cs
    ├── DirectConversationTests.cs
    ├── GroupConversationTests.cs
    └── ValidationTests.cs
```

---

## 7) Implementation Order

| Step | Task | Est. Time |
|------|------|-----------|
| 1 | Create `Chat.Abstractions` with all DTOs, interfaces | 0.5 day |
| 2 | Create `Chat.Core` with ChatService, validation, notifier | 1 day |
| 3 | Create `Chat.Store.InMemory` | 0.5 day |
| 4 | Create `Chat.Tests` with comprehensive tests | 0.5 day |
| 5 | Build and run tests | immediate |

**Total: ~2.5 days**

---

## 8) Integration Points

### 8.1 Realtime Hub Integration

The `RealtimeChatNotifier` uses `IRealtimePublisher` from Realtime.Abstractions:

```csharp
// Registration in DI
services.AddSingleton<IChatNotifier>(sp =>
    new RealtimeChatNotifier(sp.GetRequiredService<IRealtimePublisher>()));
```

### 8.2 IGroupMembershipResolver Integration

Chat Service can provide conversation membership to Realtime:

```csharp
public class ChatMembershipResolver : IGroupMembershipResolver
{
    private readonly IConversationStore _conversations;

    public async Task<IReadOnlyList<EntityRefDto>> GetConversationMembersAsync(
        string tenantId,
        string conversationId,
        CancellationToken ct = default)
    {
        var conv = await _conversations.GetByIdAsync(tenantId, conversationId, ct);
        if (conv == null) return [];

        return conv.Participants
            .Where(p => !p.HasLeft)
            .Select(p => p.Profile)
            .ToList();
    }
}
```

### 8.3 Inbox Integration (Future)

Chat Service can create inbox notifications for:
- New messages when user is offline
- Mentions in group chats
- Conversation invites

---

## 9) Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| **Direct conversation deduplication** | `FindDirectConversationAsync` ensures only one conversation exists between two users |
| **Soft delete with per-user tracking** | `DeletedByProfileIds` allows "delete for me" without affecting others |
| **Conversation-level unread counts** | Calculated from `LastReadMessageId` vs message store count |
| **Separate Stores** | `IConversationStore` and `IMessageStore` allow independent scaling/optimization |
| **IChatNotifier abstraction** | Decouples from Realtime implementation; can be replaced with queue-based delivery |
| **System messages** | `SystemMessageType` enum allows displaying "X joined the conversation" inline |

---

## 10) Future Enhancements

- **Message reactions** — Emoji reactions to individual messages
- **Forwarding** — Forward messages to other conversations
- **Pinned messages** — Pin important messages in conversation
- **Search** — Full-text search within conversations
- **Message retention** — Auto-delete old messages
- **Encryption** — End-to-end encryption support
- **File attachments** — Direct integration with Media Service
- **Delivery receipts** — Track message delivery (not just read)
