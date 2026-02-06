# Social Network Extensions — C# Library Plan for an LLM Agent Programmer

**Goal:** Build the **missing subsystems** to complete a Facebook-like social network demo using the existing Activity Stream, Relationship Service, Identity, and Inbox libraries.

This plan covers the ~20% missing functionality identified in the gap analysis.

---

## Executive Summary

| Priority | Subsystem | Complexity | Depends On |
|----------|-----------|------------|------------|
| P0 | **Content Service** (Posts, Comments, Reactions) | Medium | ActivityStream, Identity |
| P1 | **Friendship Service** (Mutual friends) | Low | RelationshipService |
| P2 | **Media Service** (File references) | Low | None (external storage) |
| P3 | **Chat Service** (Direct messaging) | High | Identity, Inbox patterns |
| P4 | **Groups Service** (Communities) | Medium | Identity, RelationshipService |
| P5 | **Realtime Hub** (SignalR push) | Medium | All services |
| P6 | **Search Service** (Full-text) | Medium | External (Elasticsearch) |

**Recommended MVP order:** P0 → P1 → P2 → P5 → P3

---

## 0) Definition of Done (Full Social Network MVP)

### 0.1 New Projects

```
src/
├── Content.Abstractions/          # Posts, Comments, Reactions DTOs + interfaces
├── Content.Core/                  # Content service implementation
├── Content.Store.InMemory/        # Reference store
├── Friendship.Abstractions/       # Mutual friendship DTOs + interfaces
├── Friendship.Core/               # Friendship service (wraps RelationshipService)
├── Media.Abstractions/            # Media reference DTOs + interfaces
├── Media.Core/                    # Media service (metadata, no blob storage)
├── Chat.Abstractions/             # Conversations, Messages DTOs + interfaces
├── Chat.Core/                     # Chat service implementation
├── Chat.Store.InMemory/           # Reference store
├── Groups.Abstractions/           # Groups, Membership DTOs + interfaces
├── Groups.Core/                   # Groups service implementation
├── Groups.Store.InMemory/         # Reference store
├── Realtime.Abstractions/         # Hub interfaces + events
├── Realtime.SignalR/              # SignalR implementation
└── SocialNetwork.Demo/            # Demo API wiring everything together

tests/
├── Content.Tests/
├── Friendship.Tests/
├── Media.Tests/
├── Chat.Tests/
├── Groups.Tests/
└── Realtime.Tests/
```

### 0.2 Success Criteria

- All tests green
- Demo API can: sign up, create profile, post, comment, like, follow, friend request, chat, join groups
- In-memory stores for all new services
- Zero framework dependencies in Abstractions/Core (except Realtime.SignalR)

---

# P0: Content Service (Posts, Comments, Reactions)

## 1) Core Concepts

### 1.1 Content as First-Class Entities
While activities track "what happened," content tracks "what exists":
- **Post**: authored content with body, optional media refs
- **Comment**: reply to a post or another comment (threading)
- **Reaction**: lightweight response (Like, Love, Haha, Wow, Sad, Angry)

### 1.2 Relationship to Activity Stream
- When a **Post is created** → publish `post.created` activity
- When a **Comment is added** → publish `post.commented` activity (target = post)
- When a **Reaction is added** → publish `post.reacted` activity (target = post)

Content Service owns the entities; Activity Stream tracks the events.

### 1.3 Aggregation Counts
Content Service maintains denormalized counts:
- `Post.CommentCount`
- `Post.ReactionCounts` (dictionary by reaction type)

---

## 2) DTOs

### 2.1 PostDto

```csharp
public sealed class PostDto
{
    public string? Id { get; set; }
    public required string TenantId { get; set; }
    public required EntityRefDto Author { get; set; }
    public required string Body { get; set; }
    public List<MediaRefDto>? Media { get; set; }
    public ContentVisibility Visibility { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    
    // Denormalized counts
    public int CommentCount { get; set; }
    public Dictionary<ReactionType, int> ReactionCounts { get; set; } = new();
    
    // User's own reaction (populated per-viewer)
    public ReactionType? ViewerReaction { get; set; }
}
```

### 2.2 CommentDto

```csharp
public sealed class CommentDto
{
    public string? Id { get; set; }
    public required string TenantId { get; set; }
    public required EntityRefDto Author { get; set; }
    public required string PostId { get; set; }
    public string? ParentCommentId { get; set; }  // For threading
    public required string Body { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    
    // Denormalized
    public int ReplyCount { get; set; }
    public Dictionary<ReactionType, int> ReactionCounts { get; set; } = new();
}
```

### 2.3 ReactionDto

```csharp
public sealed class ReactionDto
{
    public string? Id { get; set; }
    public required string TenantId { get; set; }
    public required EntityRefDto Actor { get; set; }
    public required string TargetId { get; set; }       // PostId or CommentId
    public required ReactionTargetKind TargetKind { get; set; }
    public required ReactionType Type { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public enum ReactionTargetKind
ca{
    Post = 0,
    Comment = 1
}

public enum ReactionType
{
    Like = 1,
    Love = 2,
    Haha = 3,
    Wow = 4,
    Sad = 5,
    Angry = 6
}
```

### 2.4 ContentVisibility

```csharp
public enum ContentVisibility
{
    Public = 0,      // Anyone can see
    Friends = 1,     // Only friends
    Private = 2      // Only author
}
```

### 2.5 MediaRefDto

```csharp
public sealed class MediaRefDto
{
    public required string Id { get; set; }
    public required MediaType Type { get; set; }
    public required string Url { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? AltText { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
}

public enum MediaType
{
    Image = 1,
    Video = 2,
    Link = 3
}
```

---

## 3) Interfaces

### 3.1 IContentService

```csharp
public interface IContentService
{
    // Posts
    Task<PostDto> CreatePostAsync(CreatePostRequest request, CancellationToken ct = default);
    Task<PostDto?> GetPostAsync(string tenantId, string postId, EntityRefDto? viewer = null, CancellationToken ct = default);
    Task<PostDto> UpdatePostAsync(UpdatePostRequest request, CancellationToken ct = default);
    Task DeletePostAsync(string tenantId, string postId, EntityRefDto actor, CancellationToken ct = default);
    Task<ContentPageResult<PostDto>> GetPostsAsync(PostQuery query, CancellationToken ct = default);
    
    // Comments
    Task<CommentDto> AddCommentAsync(AddCommentRequest request, CancellationToken ct = default);
    Task<CommentDto> UpdateCommentAsync(UpdateCommentRequest request, CancellationToken ct = default);
    Task DeleteCommentAsync(string tenantId, string commentId, EntityRefDto actor, CancellationToken ct = default);
    Task<ContentPageResult<CommentDto>> GetCommentsAsync(CommentQuery query, CancellationToken ct = default);
    
    // Reactions
    Task<ReactionDto> ReactAsync(ReactRequest request, CancellationToken ct = default);
    Task RemoveReactionAsync(string tenantId, string targetId, ReactionTargetKind targetKind, EntityRefDto actor, CancellationToken ct = default);
    Task<ContentPageResult<ReactionDto>> GetReactionsAsync(ReactionQuery query, CancellationToken ct = default);
}
```

### 3.2 IContentStore

```csharp
public interface IContentStore
{
    // Posts
    Task<PostDto> UpsertPostAsync(PostDto post, CancellationToken ct = default);
    Task<PostDto?> GetPostByIdAsync(string tenantId, string postId, CancellationToken ct = default);
    Task DeletePostAsync(string tenantId, string postId, CancellationToken ct = default);
    Task<ContentPageResult<PostDto>> QueryPostsAsync(PostQuery query, CancellationToken ct = default);
    Task IncrementCommentCountAsync(string tenantId, string postId, int delta, CancellationToken ct = default);
    Task UpdateReactionCountAsync(string tenantId, string postId, ReactionType type, int delta, CancellationToken ct = default);
    
    // Comments
    Task<CommentDto> UpsertCommentAsync(CommentDto comment, CancellationToken ct = default);
    Task<CommentDto?> GetCommentByIdAsync(string tenantId, string commentId, CancellationToken ct = default);
    Task DeleteCommentAsync(string tenantId, string commentId, CancellationToken ct = default);
    Task<ContentPageResult<CommentDto>> QueryCommentsAsync(CommentQuery query, CancellationToken ct = default);
    Task<int> CountCommentsAsync(string tenantId, string postId, CancellationToken ct = default);
    
    // Reactions
    Task<ReactionDto> UpsertReactionAsync(ReactionDto reaction, CancellationToken ct = default);
    Task<ReactionDto?> GetReactionAsync(string tenantId, string targetId, ReactionTargetKind targetKind, EntityRefDto actor, CancellationToken ct = default);
    Task DeleteReactionAsync(string tenantId, string targetId, ReactionTargetKind targetKind, EntityRefDto actor, CancellationToken ct = default);
    Task<ContentPageResult<ReactionDto>> QueryReactionsAsync(ReactionQuery query, CancellationToken ct = default);
    Task<Dictionary<ReactionType, int>> GetReactionCountsAsync(string tenantId, string targetId, ReactionTargetKind targetKind, CancellationToken ct = default);
}
```

---

## 4) Request/Response Types

```csharp
public sealed class CreatePostRequest
{
    public required string TenantId { get; set; }
    public required EntityRefDto Author { get; set; }
    public required string Body { get; set; }
    public List<MediaRefDto>? Media { get; set; }
    public ContentVisibility Visibility { get; set; }
}

public sealed class UpdatePostRequest
{
    public required string TenantId { get; set; }
    public required string PostId { get; set; }
    public required EntityRefDto Actor { get; set; }
    public string? Body { get; set; }
    public ContentVisibility? Visibility { get; set; }
}

public sealed class AddCommentRequest
{
    public required string TenantId { get; set; }
    public required EntityRefDto Author { get; set; }
    public required string PostId { get; set; }
    public string? ParentCommentId { get; set; }
    public required string Body { get; set; }
}

public sealed class UpdateCommentRequest
{
    public required string TenantId { get; set; }
    public required string CommentId { get; set; }
    public required EntityRefDto Actor { get; set; }
    public required string Body { get; set; }
}

public sealed class ReactRequest
{
    public required string TenantId { get; set; }
    public required EntityRefDto Actor { get; set; }
    public required string TargetId { get; set; }
    public required ReactionTargetKind TargetKind { get; set; }
    public required ReactionType Type { get; set; }
}

public sealed class PostQuery
{
    public required string TenantId { get; set; }
    public EntityRefDto? Author { get; set; }
    public EntityRefDto? Viewer { get; set; }  // For visibility filtering
    public string? Cursor { get; set; }
    public int Limit { get; set; } = 20;
}

public sealed class CommentQuery
{
    public required string TenantId { get; set; }
    public required string PostId { get; set; }
    public string? ParentCommentId { get; set; }  // null = top-level comments
    public string? Cursor { get; set; }
    public int Limit { get; set; } = 20;
}

public sealed class ReactionQuery
{
    public required string TenantId { get; set; }
    public required string TargetId { get; set; }
    public required ReactionTargetKind TargetKind { get; set; }
    public ReactionType? Type { get; set; }  // Filter by type
    public string? Cursor { get; set; }
    public int Limit { get; set; } = 20;
}

public sealed class ContentPageResult<T>
{
    public List<T> Items { get; set; } = new();
    public string? NextCursor { get; set; }
    public bool HasMore { get; set; }
}
```

---

## 5) Validation Rules

| Field | Rules |
|-------|-------|
| TenantId | Required, max 100 chars |
| PostId | Required, max 100 chars |
| Body (Post) | Required, max 10,000 chars |
| Body (Comment) | Required, max 2,000 chars |
| Media | Max 10 items per post |
| MediaRefDto.Url | Required, valid URL, max 2,000 chars |

---

## 6) Activity Integration

When Content Service performs actions, it should publish activities:

```csharp
// In ContentService.CreatePostAsync
var activity = new ActivityDto
{
    TenantId = request.TenantId,
    TypeKey = "post.created",
    Actor = request.Author,
    Owner = request.Author,
    OccurredAt = DateTimeOffset.UtcNow,
    Visibility = MapVisibility(request.Visibility),
    Summary = $"{request.Author.Display} created a post",
    Payload = new { PostId = post.Id, Preview = post.Body.Truncate(100) }
};
await _activityService.PublishAsync(activity);
```

---

# P1: Friendship Service (Mutual Friends)

## 1) Core Concepts

### 1.1 Friendship vs Follow
- **Follow**: One-way (A follows B, B doesn't follow A)
- **Friendship**: Mutual (A and B are friends = both follow each other with approval)

### 1.2 Friend Request Workflow
1. User A sends friend request to User B
2. Creates a pending `FriendRequest` (not yet using RelationshipService)
3. User B approves → creates two Follow edges (A→B, B→A) with `IsFriend=true` metadata
4. User B denies → request deleted, no edges

### 1.3 Leverage Inbox for Requests
Friend requests use the existing Inbox follow request workflow.

---

## 2) DTOs

### 2.1 FriendshipStatus

```csharp
public enum FriendshipStatus
{
    None = 0,           // No relationship
    RequestSent = 1,    // Current user sent request
    RequestReceived = 2, // Current user received request
    Friends = 3          // Mutual friends
}
```

### 2.2 FriendDto

```csharp
public sealed class FriendDto
{
    public required EntityRefDto Friend { get; set; }
    public DateTimeOffset FriendsSince { get; set; }
    public int MutualFriendCount { get; set; }
}
```

---

## 3) Interface

```csharp
public interface IFriendshipService
{
    Task<FriendshipStatus> GetStatusAsync(string tenantId, EntityRefDto user, EntityRefDto other, CancellationToken ct = default);
    Task SendFriendRequestAsync(string tenantId, EntityRefDto from, EntityRefDto to, CancellationToken ct = default);
    Task AcceptFriendRequestAsync(string tenantId, EntityRefDto user, EntityRefDto requester, CancellationToken ct = default);
    Task DeclineFriendRequestAsync(string tenantId, EntityRefDto user, EntityRefDto requester, CancellationToken ct = default);
    Task UnfriendAsync(string tenantId, EntityRefDto user, EntityRefDto friend, CancellationToken ct = default);
    Task<ContentPageResult<FriendDto>> GetFriendsAsync(FriendsQuery query, CancellationToken ct = default);
    Task<int> GetMutualFriendCountAsync(string tenantId, EntityRefDto user1, EntityRefDto user2, CancellationToken ct = default);
    Task<ContentPageResult<FriendDto>> GetMutualFriendsAsync(MutualFriendsQuery query, CancellationToken ct = default);
}
```

---

## 4) Implementation Strategy

`FriendshipService` wraps `IRelationshipService` and `IInboxNotificationService`:

```csharp
public class FriendshipService : IFriendshipService
{
    private readonly IRelationshipService _relationships;
    private readonly IInboxNotificationService _inbox;
    
    public async Task<FriendshipStatus> GetStatusAsync(...)
    {
        // Check for mutual Follow edges with IsFriend metadata
        var aToB = await _relationships.GetEdgeAsync(tenantId, user, other, RelationshipKind.Follow);
        var bToA = await _relationships.GetEdgeAsync(tenantId, other, user, RelationshipKind.Follow);
        
        if (aToB != null && bToA != null && 
            aToB.Metadata?.GetValueOrDefault("IsFriend") == true &&
            bToA.Metadata?.GetValueOrDefault("IsFriend") == true)
        {
            return FriendshipStatus.Friends;
        }
        
        // Check for pending requests...
    }
}
```

---

# P2: Media Service (File References)

## 1) Core Concepts

### 1.1 Metadata Only
Media Service stores **metadata and references**, not actual files.
- Actual files stored in external blob storage (Azure Blob, S3, etc.)
- Media Service tracks: URL, type, dimensions, upload status

### 1.2 Upload Flow
1. Client requests upload URL
2. Media Service returns signed URL + media record ID
3. Client uploads directly to blob storage
4. Client confirms upload complete
5. Media Service marks record as ready

---

## 2) DTOs

```csharp
public sealed class MediaDto
{
    public string? Id { get; set; }
    public required string TenantId { get; set; }
    public required EntityRefDto Owner { get; set; }
    public required MediaType Type { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }
    public string? Url { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public MediaStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public enum MediaStatus
{
    Pending = 0,    // Upload URL generated, awaiting upload
    Ready = 1,      // Upload complete, file accessible
    Failed = 2,     // Upload failed or timed out
    Deleted = 3     // Soft deleted
}

public sealed class UploadUrlResult
{
    public required string MediaId { get; set; }
    public required string UploadUrl { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
```

---

## 3) Interface

```csharp
public interface IMediaService
{
    Task<UploadUrlResult> RequestUploadUrlAsync(RequestUploadRequest request, CancellationToken ct = default);
    Task<MediaDto> ConfirmUploadAsync(string tenantId, string mediaId, CancellationToken ct = default);
    Task<MediaDto?> GetMediaAsync(string tenantId, string mediaId, CancellationToken ct = default);
    Task DeleteMediaAsync(string tenantId, string mediaId, EntityRefDto actor, CancellationToken ct = default);
}

public interface IMediaStorageProvider
{
    Task<string> GenerateUploadUrlAsync(string mediaId, string contentType, long maxSizeBytes, CancellationToken ct = default);
    Task<string> GetDownloadUrlAsync(string mediaId, CancellationToken ct = default);
    Task<bool> ExistsAsync(string mediaId, CancellationToken ct = default);
    Task DeleteAsync(string mediaId, CancellationToken ct = default);
}
```

---

# P3: Chat Service (Direct Messaging)

## 1) Core Concepts

### 1.1 Conversations
- **Direct**: Two participants (1:1 chat)
- **Group**: Multiple participants (group chat)

### 1.2 Messages
- Text content with optional media
- Read receipts per participant
- Soft delete (hidden from deleter, visible to others until both delete)

### 1.3 Unread Counts
Denormalized per-participant unread count on conversation.

---

## 2) DTOs

### 2.1 ConversationDto

```csharp
public sealed class ConversationDto
{
    public string? Id { get; set; }
    public required string TenantId { get; set; }
    public ConversationType Type { get; set; }
    public string? Title { get; set; }  // For group chats
    public List<ConversationParticipantDto> Participants { get; set; } = new();
    public MessageDto? LastMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    
    // Per-viewer
    public int UnreadCount { get; set; }
}

public enum ConversationType
{
    Direct = 0,
    Group = 1
}

public sealed class ConversationParticipantDto
{
    public required EntityRefDto Profile { get; set; }
    public ParticipantRole Role { get; set; }
    public DateTimeOffset JoinedAt { get; set; }
    public DateTimeOffset? LastReadAt { get; set; }
    public string? LastReadMessageId { get; set; }
}

public enum ParticipantRole
{
    Member = 0,
    Admin = 1
}
```

### 2.2 MessageDto

```csharp
public sealed class MessageDto
{
    public string? Id { get; set; }
    public required string TenantId { get; set; }
    public required string ConversationId { get; set; }
    public required EntityRefDto Sender { get; set; }
    public required string Body { get; set; }
    public List<MediaRefDto>? Media { get; set; }
    public MessageDto? ReplyTo { get; set; }  // Quote reply
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? EditedAt { get; set; }
    public bool IsDeleted { get; set; }
}
```

---

## 3) Interface

```csharp
public interface IChatService
{
    // Conversations
    Task<ConversationDto> GetOrCreateDirectConversationAsync(string tenantId, EntityRefDto user1, EntityRefDto user2, CancellationToken ct = default);
    Task<ConversationDto> CreateGroupConversationAsync(CreateGroupConversationRequest request, CancellationToken ct = default);
    Task<ConversationDto?> GetConversationAsync(string tenantId, string conversationId, EntityRefDto viewer, CancellationToken ct = default);
    Task<ContentPageResult<ConversationDto>> GetConversationsAsync(ConversationsQuery query, CancellationToken ct = default);
    Task AddParticipantAsync(string tenantId, string conversationId, EntityRefDto actor, EntityRefDto newParticipant, CancellationToken ct = default);
    Task RemoveParticipantAsync(string tenantId, string conversationId, EntityRefDto actor, EntityRefDto participant, CancellationToken ct = default);
    Task LeaveConversationAsync(string tenantId, string conversationId, EntityRefDto participant, CancellationToken ct = default);
    
    // Messages
    Task<MessageDto> SendMessageAsync(SendMessageRequest request, CancellationToken ct = default);
    Task<MessageDto> EditMessageAsync(EditMessageRequest request, CancellationToken ct = default);
    Task DeleteMessageAsync(string tenantId, string conversationId, string messageId, EntityRefDto actor, CancellationToken ct = default);
    Task<ContentPageResult<MessageDto>> GetMessagesAsync(MessagesQuery query, CancellationToken ct = default);
    
    // Read receipts
    Task MarkReadAsync(string tenantId, string conversationId, EntityRefDto participant, string messageId, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(string tenantId, EntityRefDto participant, CancellationToken ct = default);
}
```

---

# P4: Groups Service (Communities)

## 1) Core Concepts

### 1.1 Group Types
- **Public**: Anyone can see and join
- **Private**: Visible but requires approval to join
- **Secret**: Hidden from search, invite-only

### 1.2 Group Membership
- Owner, Admin, Moderator, Member roles
- Pending status for approval-required groups

### 1.3 Group Feed
Groups have their own activity feed (posts visible to members only).

---

## 2) DTOs

```csharp
public sealed class GroupDto
{
    public string? Id { get; set; }
    public required string TenantId { get; set; }
    public required string Name { get; set; }
    public required string Handle { get; set; }  // Unique within tenant
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public GroupPrivacy Privacy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int MemberCount { get; set; }
}

public enum GroupPrivacy
{
    Public = 0,
    Private = 1,
    Secret = 2
}

public sealed class GroupMemberDto
{
    public required EntityRefDto Profile { get; set; }
    public GroupMemberRole Role { get; set; }
    public GroupMemberStatus Status { get; set; }
    public DateTimeOffset JoinedAt { get; set; }
}

public enum GroupMemberRole
{
    Member = 0,
    Moderator = 1,
    Admin = 2,
    Owner = 3
}

public enum GroupMemberStatus
{
    Active = 0,
    Pending = 1,    // Awaiting approval
    Invited = 2,    // Invited, not yet accepted
    Banned = 3
}
```

---

## 3) Interface

```csharp
public interface IGroupService
{
    Task<GroupDto> CreateGroupAsync(CreateGroupRequest request, CancellationToken ct = default);
    Task<GroupDto?> GetGroupAsync(string tenantId, string groupId, CancellationToken ct = default);
    Task<GroupDto?> GetGroupByHandleAsync(string tenantId, string handle, CancellationToken ct = default);
    Task<GroupDto> UpdateGroupAsync(UpdateGroupRequest request, CancellationToken ct = default);
    Task DeleteGroupAsync(string tenantId, string groupId, EntityRefDto actor, CancellationToken ct = default);
    Task<ContentPageResult<GroupDto>> SearchGroupsAsync(GroupSearchQuery query, CancellationToken ct = default);
    
    // Membership
    Task JoinGroupAsync(string tenantId, string groupId, EntityRefDto profile, CancellationToken ct = default);
    Task LeaveGroupAsync(string tenantId, string groupId, EntityRefDto profile, CancellationToken ct = default);
    Task InviteToGroupAsync(string tenantId, string groupId, EntityRefDto actor, EntityRefDto invitee, CancellationToken ct = default);
    Task ApproveMemberAsync(string tenantId, string groupId, EntityRefDto actor, EntityRefDto member, CancellationToken ct = default);
    Task RejectMemberAsync(string tenantId, string groupId, EntityRefDto actor, EntityRefDto member, CancellationToken ct = default);
    Task UpdateMemberRoleAsync(string tenantId, string groupId, EntityRefDto actor, EntityRefDto member, GroupMemberRole role, CancellationToken ct = default);
    Task BanMemberAsync(string tenantId, string groupId, EntityRefDto actor, EntityRefDto member, CancellationToken ct = default);
    Task<ContentPageResult<GroupMemberDto>> GetMembersAsync(GroupMembersQuery query, CancellationToken ct = default);
    Task<ContentPageResult<GroupDto>> GetUserGroupsAsync(UserGroupsQuery query, CancellationToken ct = default);
}
```

---

# P5: Realtime Hub (SignalR)

## 1) Core Concepts

### 1.1 Events to Push
- New activity in feed
- New inbox notification
- New chat message
- Typing indicators
- Online/offline presence

### 1.2 Connection Mapping
Map connections to profiles for targeted pushes.

---

## 2) Interfaces

```csharp
public interface IRealtimeHub
{
    Task SendToProfileAsync(string tenantId, EntityRefDto profile, string eventType, object payload, CancellationToken ct = default);
    Task SendToProfilesAsync(string tenantId, IEnumerable<EntityRefDto> profiles, string eventType, object payload, CancellationToken ct = default);
    Task SendToConversationAsync(string tenantId, string conversationId, string eventType, object payload, CancellationToken ct = default);
    Task SendToGroupAsync(string tenantId, string groupId, string eventType, object payload, CancellationToken ct = default);
}

public interface IConnectionManager
{
    Task AddConnectionAsync(string connectionId, string tenantId, EntityRefDto profile, CancellationToken ct = default);
    Task RemoveConnectionAsync(string connectionId, CancellationToken ct = default);
    Task<IEnumerable<string>> GetConnectionsAsync(string tenantId, EntityRefDto profile, CancellationToken ct = default);
    Task<bool> IsOnlineAsync(string tenantId, EntityRefDto profile, CancellationToken ct = default);
}
```

### 2.1 Event Types

```csharp
public static class RealtimeEvents
{
    public const string ActivityCreated = "activity.created";
    public const string InboxItemCreated = "inbox.item.created";
    public const string InboxItemUpdated = "inbox.item.updated";
    public const string MessageReceived = "message.received";
    public const string MessageEdited = "message.edited";
    public const string MessageDeleted = "message.deleted";
    public const string TypingStarted = "typing.started";
    public const string TypingStopped = "typing.stopped";
    public const string PresenceChanged = "presence.changed";
}
```

---

# P6: Search Service

## 1) Core Concepts

### 1.1 Searchable Entities
- Profiles (by name, handle)
- Posts (by content)
- Groups (by name, description)
- Hashtags

### 1.2 External Search Engine
This service is a **facade** over an external search engine (Elasticsearch, Algolia, Azure Cognitive Search).

---

## 2) Interface

```csharp
public interface ISearchService
{
    Task<SearchResult<ProfileDto>> SearchProfilesAsync(ProfileSearchQuery query, CancellationToken ct = default);
    Task<SearchResult<PostDto>> SearchPostsAsync(PostSearchQuery query, CancellationToken ct = default);
    Task<SearchResult<GroupDto>> SearchGroupsAsync(GroupSearchQuery query, CancellationToken ct = default);
    Task<SearchResult<string>> SearchHashtagsAsync(HashtagSearchQuery query, CancellationToken ct = default);
}

public interface ISearchIndexer
{
    Task IndexProfileAsync(ProfileDto profile, CancellationToken ct = default);
    Task IndexPostAsync(PostDto post, CancellationToken ct = default);
    Task IndexGroupAsync(GroupDto group, CancellationToken ct = default);
    Task RemoveFromIndexAsync(string indexType, string id, CancellationToken ct = default);
}

public sealed class SearchResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public string? NextCursor { get; set; }
}
```

---

# Implementation Order & Estimates

## Phase 1: Core Social (2-3 days)
1. **Content.Abstractions** - DTOs, interfaces, exceptions (0.5 day)
2. **Content.Core** - Service implementation (1 day)
3. **Content.Store.InMemory** - Reference store (0.5 day)
4. **Content.Tests** - Unit tests (0.5 day)

## Phase 2: Friendship (1 day)
1. **Friendship.Abstractions** - DTOs, interfaces (0.25 day)
2. **Friendship.Core** - Wraps RelationshipService + Inbox (0.5 day)
3. **Friendship.Tests** - Unit tests (0.25 day)

## Phase 3: Media (1 day)
1. **Media.Abstractions** - DTOs, interfaces (0.25 day)
2. **Media.Core** - Service + mock storage provider (0.5 day)
3. **Media.Tests** - Unit tests (0.25 day)

## Phase 4: Realtime (1 day)
1. **Realtime.Abstractions** - Hub interfaces, events (0.25 day)
2. **Realtime.SignalR** - SignalR implementation (0.5 day)
3. **Realtime.Tests** - Integration tests (0.25 day)

## Phase 5: Chat (2 days)
1. **Chat.Abstractions** - DTOs, interfaces (0.25 day)
2. **Chat.Core** - Service implementation (1 day)
3. **Chat.Store.InMemory** - Reference store (0.5 day)
4. **Chat.Tests** - Unit tests (0.25 day)

## Phase 6: Groups (1.5 days)
1. **Groups.Abstractions** - DTOs, interfaces (0.25 day)
2. **Groups.Core** - Service implementation (0.75 day)
3. **Groups.Store.InMemory** - Reference store (0.25 day)
4. **Groups.Tests** - Unit tests (0.25 day)

## Phase 7: Demo API (1 day)
1. **SocialNetwork.Demo** - Minimal API wiring all services (1 day)

---

# Total Estimate: ~10 days

| Phase | Subsystem | Days |
|-------|-----------|------|
| 1 | Content Service | 2.5 |
| 2 | Friendship Service | 1 |
| 3 | Media Service | 1 |
| 4 | Realtime Hub | 1 |
| 5 | Chat Service | 2 |
| 6 | Groups Service | 1.5 |
| 7 | Demo API | 1 |
| **Total** | | **10 days** |

---

# Quick Start (Minimal Demo)

For the fastest path to a working demo, implement only:

| Priority | Component | Days |
|----------|-----------|------|
| 1 | Content Service (Posts, Comments, Reactions) | 2.5 |
| 2 | Friendship Service (Mutual friends) | 1 |
| 3 | Demo API | 0.5 |
| **Minimal Total** | | **4 days** |

This gives you: sign up, profiles, posts, comments, likes, follow, friend requests, news feed, notifications.

---

# Next Steps

1. **Confirm scope**: Full 10-day plan or minimal 4-day demo?
2. **Start implementation**: Begin with Content.Abstractions
3. **Incremental testing**: Build and test each layer before moving on
