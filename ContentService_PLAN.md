# Content Service — C# Library Plan for an LLM Agent Programmer

**Goal:** Build a **Content Service** as a set of C# libraries for managing social content (Posts, Comments, Reactions) with a pluggable architecture allowing different storage implementations.

The service manages the core social content entities:
- **Posts**: Authored content with body, optional media references, visibility settings
- **Comments**: Threaded replies to posts (with nesting support)
- **Reactions**: Lightweight responses (Like, Love, Haha, Wow, Sad, Angry)

> **Architecture Principle:** Clean separation between Abstractions (interfaces/DTOs), Core (business logic), and Store implementations. Any store can be swapped without changing Core logic.

---

## 0) Definition of Done (v1 / MVP)

### 0.1 Project References

```
Content.Abstractions
  └── (no dependencies - pure DTOs and interfaces)

Content.Core
  └── Content.Abstractions
  └── (optionally) ActivityStream.Abstractions (for publishing activities)

Content.Store.InMemory
  └── Content.Abstractions

Content.Tests
  └── All of the above
```

### 0.2 Deliverables (projects)

1. **Content.Abstractions** (~0.5 day)
   - DTOs: `PostDto`, `CommentDto`, `ReactionDto`
   - Enums: `ContentVisibility`, `ReactionType`, `ReactionTargetKind`
   - Request types: `CreatePostRequest`, `UpdatePostRequest`, `AddCommentRequest`, etc.
   - Query types: `PostQuery`, `CommentQuery`, `ReactionQuery`
   - Result type: `ContentPageResult<T>`
   - Interfaces: `IContentService`, `IPostStore`, `ICommentStore`, `IReactionStore`
   - Validation: `ContentValidationError`, `ContentValidationException`
   - ID generator: `IIdGenerator` (reuse pattern from other services)
   - No external dependencies

2. **Content.Core** (~1 day)
   - `ContentService` implementing `IContentService`
   - `ContentValidator` for request validation
   - `ContentNormalizer` for text normalization
   - Orchestrates stores and handles business rules
   - Optional: `IActivityPublisher` integration for event publishing

3. **Content.Store.InMemory** (~0.5 day)
   - `InMemoryPostStore` implementing `IPostStore`
   - `InMemoryCommentStore` implementing `ICommentStore`
   - `InMemoryReactionStore` implementing `IReactionStore`
   - Thread-safe with `ConcurrentDictionary` and `ReaderWriterLockSlim`
   - Cursor-based pagination

4. **Content.Tests** (~0.5 day)
   - Unit tests for `ContentService`
   - Store tests for all three stores
   - Validation tests
   - Concurrency tests

### 0.3 Success Criteria

- All tests green
- Can create, read, update, delete posts
- Can add, update, delete comments (with threading)
- Can add, change, remove reactions
- Denormalized counts maintained correctly
- Multi-tenant isolation
- Pluggable stores (swap in-memory for SQL/NoSQL without code changes)

---

## 1) Core Concepts

### 1.1 Content vs Activity Stream

| Concern | Content Service | Activity Stream |
|---------|-----------------|-----------------|
| Purpose | "What exists" | "What happened" |
| Entities | Posts, Comments, Reactions | Activities |
| Mutability | Editable, deletable | Append-only |
| Queries | By author, post, filters | By actor, target, time |

**Integration pattern:** Content Service publishes activities after mutations:
- `post.created`, `post.updated`, `post.deleted`
- `comment.created`, `comment.updated`, `comment.deleted`
- `reaction.added`, `reaction.changed`, `reaction.removed`

### 1.2 Pluggable Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      IContentService                        │
│                    (Business Logic API)                     │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      ContentService                         │
│  - Validation, normalization                                │
│  - Orchestrates stores                                      │
│  - Publishes activities (optional)                          │
└─────────────────────────────────────────────────────────────┘
           │                    │                    │
           ▼                    ▼                    ▼
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│   IPostStore    │  │  ICommentStore  │  │ IReactionStore  │
└─────────────────┘  └─────────────────┘  └─────────────────┘
           │                    │                    │
           ▼                    ▼                    ▼
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│ InMemoryPost    │  │ InMemoryComment │  │ InMemoryReact   │
│     Store       │  │      Store      │  │     Store       │
└─────────────────┘  └─────────────────┘  └─────────────────┘
        OR                   OR                   OR
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│  CosmosPost     │  │  CosmosComment  │  │  CosmosReact    │
│     Store       │  │      Store      │  │     Store       │
└─────────────────┘  └─────────────────┘  └─────────────────┘
```

### 1.3 Denormalized Counts

Content Service maintains counts for performance:
- `Post.CommentCount` - total comments on post
- `Post.ReactionCounts` - dictionary of reaction type → count
- `Comment.ReplyCount` - nested comment count
- `Comment.ReactionCounts` - reactions on comment

Stores handle atomic increment/decrement operations.

### 1.4 Viewer Context

Some fields are populated per-viewer:
- `Post.ViewerReaction` - the current user's reaction (if any)
- `Comment.ViewerReaction` - the current user's reaction (if any)

This requires passing a `viewer` parameter to read operations.

---

## 2) DTOs

### 2.1 EntityRefDto (reuse from existing)

```csharp
public sealed class EntityRefDto
{
    public required string Type { get; set; }  // "Profile", "Group", etc.
    public required string Id { get; set; }
    public string? DisplayName { get; set; }
    public string? ImageUrl { get; set; }
}
```

### 2.2 PostDto

```csharp
public sealed class PostDto
{
    public string? Id { get; set; }
    public required string TenantId { get; set; }
    public required EntityRefDto Author { get; set; }
    public required string Body { get; set; }
    public List<string>? MediaIds { get; set; }  // References to Media service
    public ContentVisibility Visibility { get; set; } = ContentVisibility.Public;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    
    // Denormalized counts (maintained by store)
    public int CommentCount { get; set; }
    public Dictionary<ReactionType, int> ReactionCounts { get; set; } = new();
    
    // Per-viewer (populated on read)
    public ReactionType? ViewerReaction { get; set; }
}
```

### 2.3 CommentDto

```csharp
public sealed class CommentDto
{
    public string? Id { get; set; }
    public required string TenantId { get; set; }
    public required EntityRefDto Author { get; set; }
    public required string PostId { get; set; }
    public string? ParentCommentId { get; set; }  // null = top-level comment
    public required string Body { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    
    // Denormalized
    public int ReplyCount { get; set; }
    public Dictionary<ReactionType, int> ReactionCounts { get; set; } = new();
    
    // Per-viewer
    public ReactionType? ViewerReaction { get; set; }
}
```

### 2.4 ReactionDto

```csharp
public sealed class ReactionDto
{
    public string? Id { get; set; }
    public required string TenantId { get; set; }
    public required EntityRefDto Actor { get; set; }
    public required string TargetId { get; set; }
    public required ReactionTargetKind TargetKind { get; set; }
    public required ReactionType Type { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
```

### 2.5 Enums

```csharp
public enum ContentVisibility
{
    Public = 0,    // Anyone can see
    Friends = 1,   // Only friends of author
    Private = 2    // Only author
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

public enum ReactionTargetKind
{
    Post = 0,
    Comment = 1
}
```

---

## 3) Request/Response Types

### 3.1 Post Requests

```csharp
public sealed class CreatePostRequest
{
    public required string TenantId { get; set; }
    public required EntityRefDto Author { get; set; }
    public required string Body { get; set; }
    public List<string>? MediaIds { get; set; }
    public ContentVisibility Visibility { get; set; } = ContentVisibility.Public;
}

public sealed class UpdatePostRequest
{
    public required string TenantId { get; set; }
    public required string PostId { get; set; }
    public required EntityRefDto Actor { get; set; }  // Must be author
    public string? Body { get; set; }
    public ContentVisibility? Visibility { get; set; }
}

public sealed class DeletePostRequest
{
    public required string TenantId { get; set; }
    public required string PostId { get; set; }
    public required EntityRefDto Actor { get; set; }  // Must be author
}
```

### 3.2 Comment Requests

```csharp
public sealed class CreateCommentRequest
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

public sealed class DeleteCommentRequest
{
    public required string TenantId { get; set; }
    public required string CommentId { get; set; }
    public required EntityRefDto Actor { get; set; }
}
```

### 3.3 Reaction Requests

```csharp
public sealed class ReactRequest
{
    public required string TenantId { get; set; }
    public required EntityRefDto Actor { get; set; }
    public required string TargetId { get; set; }
    public required ReactionTargetKind TargetKind { get; set; }
    public required ReactionType Type { get; set; }
}

public sealed class RemoveReactionRequest
{
    public required string TenantId { get; set; }
    public required EntityRefDto Actor { get; set; }
    public required string TargetId { get; set; }
    public required ReactionTargetKind TargetKind { get; set; }
}
```

### 3.4 Query Types

```csharp
public sealed class PostQuery
{
    public required string TenantId { get; set; }
    public EntityRefDto? Author { get; set; }         // Filter by author
    public EntityRefDto? Viewer { get; set; }         // For visibility + ViewerReaction
    public ContentVisibility? MinVisibility { get; set; }  // Filter by visibility
    public bool IncludeDeleted { get; set; } = false;
    public string? Cursor { get; set; }
    public int Limit { get; set; } = 20;
}

public sealed class CommentQuery
{
    public required string TenantId { get; set; }
    public required string PostId { get; set; }
    public string? ParentCommentId { get; set; }  // null = top-level only
    public EntityRefDto? Viewer { get; set; }
    public bool IncludeDeleted { get; set; } = false;
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
```

### 3.5 Page Result

```csharp
public sealed class ContentPageResult<T>
{
    public required IReadOnlyList<T> Items { get; set; }
    public string? NextCursor { get; set; }
    public bool HasMore => NextCursor != null;
    public int TotalCount { get; set; }  // Optional, may be -1 if unknown
}
```

---

## 4) Interfaces

### 4.1 IContentService (Main API)

```csharp
public interface IContentService
{
    // Posts
    Task<PostDto> CreatePostAsync(CreatePostRequest request, CancellationToken ct = default);
    Task<PostDto?> GetPostAsync(string tenantId, string postId, EntityRefDto? viewer = null, CancellationToken ct = default);
    Task<PostDto> UpdatePostAsync(UpdatePostRequest request, CancellationToken ct = default);
    Task DeletePostAsync(DeletePostRequest request, CancellationToken ct = default);
    Task<ContentPageResult<PostDto>> QueryPostsAsync(PostQuery query, CancellationToken ct = default);
    
    // Comments
    Task<CommentDto> CreateCommentAsync(CreateCommentRequest request, CancellationToken ct = default);
    Task<CommentDto?> GetCommentAsync(string tenantId, string commentId, EntityRefDto? viewer = null, CancellationToken ct = default);
    Task<CommentDto> UpdateCommentAsync(UpdateCommentRequest request, CancellationToken ct = default);
    Task DeleteCommentAsync(DeleteCommentRequest request, CancellationToken ct = default);
    Task<ContentPageResult<CommentDto>> QueryCommentsAsync(CommentQuery query, CancellationToken ct = default);
    
    // Reactions
    Task<ReactionDto> ReactAsync(ReactRequest request, CancellationToken ct = default);
    Task RemoveReactionAsync(RemoveReactionRequest request, CancellationToken ct = default);
    Task<ContentPageResult<ReactionDto>> QueryReactionsAsync(ReactionQuery query, CancellationToken ct = default);
}
```

### 4.2 IPostStore (Storage Abstraction)

```csharp
public interface IPostStore
{
    Task<PostDto> UpsertAsync(PostDto post, CancellationToken ct = default);
    Task<PostDto?> GetByIdAsync(string tenantId, string postId, CancellationToken ct = default);
    Task<ContentPageResult<PostDto>> QueryAsync(PostQuery query, CancellationToken ct = default);
    Task DeleteAsync(string tenantId, string postId, CancellationToken ct = default);
    
    // Atomic count operations
    Task IncrementCommentCountAsync(string tenantId, string postId, int delta, CancellationToken ct = default);
    Task UpdateReactionCountAsync(string tenantId, string postId, ReactionType type, int delta, CancellationToken ct = default);
}
```

### 4.3 ICommentStore (Storage Abstraction)

```csharp
public interface ICommentStore
{
    Task<CommentDto> UpsertAsync(CommentDto comment, CancellationToken ct = default);
    Task<CommentDto?> GetByIdAsync(string tenantId, string commentId, CancellationToken ct = default);
    Task<ContentPageResult<CommentDto>> QueryAsync(CommentQuery query, CancellationToken ct = default);
    Task DeleteAsync(string tenantId, string commentId, CancellationToken ct = default);
    
    // Atomic count operations
    Task IncrementReplyCountAsync(string tenantId, string commentId, int delta, CancellationToken ct = default);
    Task UpdateReactionCountAsync(string tenantId, string commentId, ReactionType type, int delta, CancellationToken ct = default);
}
```

### 4.4 IReactionStore (Storage Abstraction)

```csharp
public interface IReactionStore
{
    Task<ReactionDto> UpsertAsync(ReactionDto reaction, CancellationToken ct = default);
    Task<ReactionDto?> GetAsync(string tenantId, string targetId, ReactionTargetKind targetKind, string actorId, CancellationToken ct = default);
    Task<ContentPageResult<ReactionDto>> QueryAsync(ReactionQuery query, CancellationToken ct = default);
    Task DeleteAsync(string tenantId, string targetId, ReactionTargetKind targetKind, string actorId, CancellationToken ct = default);
    Task<Dictionary<ReactionType, int>> GetCountsAsync(string tenantId, string targetId, ReactionTargetKind targetKind, CancellationToken ct = default);
}
```

### 4.5 IIdGenerator (Reused Pattern)

```csharp
public interface IIdGenerator
{
    string NewId();
}
```

---

## 5) Validation

### 5.1 ContentValidationError Enum

```csharp
public enum ContentValidationError
{
    // General
    TenantIdRequired,
    
    // Post
    PostIdRequired,
    PostNotFound,
    PostBodyRequired,
    PostBodyTooLong,
    PostUnauthorized,
    
    // Comment
    CommentIdRequired,
    CommentNotFound,
    CommentBodyRequired,
    CommentBodyTooLong,
    CommentUnauthorized,
    ParentCommentNotFound,
    
    // Reaction
    TargetIdRequired,
    TargetNotFound,
    InvalidReactionType,
    
    // Author
    AuthorRequired,
    ActorRequired
}
```

### 5.2 ContentValidationException

```csharp
public sealed class ContentValidationException : Exception
{
    public ContentValidationError Error { get; }
    public string? Field { get; }
    
    public ContentValidationException(ContentValidationError error, string? field = null)
        : base($"Content validation failed: {error}" + (field != null ? $" ({field})" : ""))
    {
        Error = error;
        Field = field;
    }
}
```

### 5.3 Validation Rules

| Field | Rule |
|-------|------|
| `TenantId` | Required, non-empty |
| `Author.Id` | Required, non-empty |
| `Post.Body` | Required, 1-10,000 chars |
| `Comment.Body` | Required, 1-5,000 chars |
| `PostId` (for comments) | Required, must exist |
| `ParentCommentId` | If provided, must exist and belong to same post |
| `ReactionType` | Must be valid enum value |

---

## 6) Service Implementation

### 6.1 ContentService Constructor

```csharp
public sealed class ContentService : IContentService
{
    private readonly IPostStore _postStore;
    private readonly ICommentStore _commentStore;
    private readonly IReactionStore _reactionStore;
    private readonly IIdGenerator _idGenerator;
    private readonly ContentServiceOptions _options;
    
    public ContentService(
        IPostStore postStore,
        ICommentStore commentStore,
        IReactionStore reactionStore,
        IIdGenerator idGenerator,
        ContentServiceOptions? options = null)
    {
        _postStore = postStore;
        _commentStore = commentStore;
        _reactionStore = reactionStore;
        _idGenerator = idGenerator;
        _options = options ?? new ContentServiceOptions();
    }
}
```

### 6.2 ContentServiceOptions

```csharp
public sealed class ContentServiceOptions
{
    public int MaxPostBodyLength { get; set; } = 10_000;
    public int MaxCommentBodyLength { get; set; } = 5_000;
    public int DefaultPageSize { get; set; } = 20;
    public int MaxPageSize { get; set; } = 100;
}
```

### 6.3 CreatePostAsync Flow

```csharp
public async Task<PostDto> CreatePostAsync(CreatePostRequest request, CancellationToken ct)
{
    // 1. Validate
    ContentValidator.ValidateCreatePost(request, _options);
    
    // 2. Create DTO
    var post = new PostDto
    {
        Id = _idGenerator.NewId(),
        TenantId = request.TenantId,
        Author = request.Author,
        Body = ContentNormalizer.NormalizeText(request.Body),
        MediaIds = request.MediaIds,
        Visibility = request.Visibility,
        CreatedAt = DateTimeOffset.UtcNow
    };
    
    // 3. Store
    await _postStore.UpsertAsync(post, ct);
    
    // 4. (Optional) Publish activity
    // await _activityPublisher?.PublishAsync("post.created", post, ct);
    
    return post;
}
```

### 6.4 ReactAsync Flow (Upsert Pattern)

```csharp
public async Task<ReactionDto> ReactAsync(ReactRequest request, CancellationToken ct)
{
    // 1. Validate
    ContentValidator.ValidateReact(request);
    
    // 2. Check target exists
    if (request.TargetKind == ReactionTargetKind.Post)
    {
        var post = await _postStore.GetByIdAsync(request.TenantId, request.TargetId, ct);
        if (post == null) throw new ContentValidationException(ContentValidationError.TargetNotFound);
    }
    else
    {
        var comment = await _commentStore.GetByIdAsync(request.TenantId, request.TargetId, ct);
        if (comment == null) throw new ContentValidationException(ContentValidationError.TargetNotFound);
    }
    
    // 3. Check for existing reaction
    var existing = await _reactionStore.GetAsync(
        request.TenantId, request.TargetId, request.TargetKind, request.Actor.Id, ct);
    
    // 4. Create/update reaction
    var reaction = new ReactionDto
    {
        Id = existing?.Id ?? _idGenerator.NewId(),
        TenantId = request.TenantId,
        Actor = request.Actor,
        TargetId = request.TargetId,
        TargetKind = request.TargetKind,
        Type = request.Type,
        CreatedAt = existing?.CreatedAt ?? DateTimeOffset.UtcNow
    };
    
    await _reactionStore.UpsertAsync(reaction, ct);
    
    // 5. Update counts (if type changed or new)
    if (existing == null)
    {
        await UpdateReactionCount(request, 1, ct);
    }
    else if (existing.Type != request.Type)
    {
        await UpdateReactionCount(request with { Type = existing.Type }, -1, ct);
        await UpdateReactionCount(request, 1, ct);
    }
    
    return reaction;
}
```

---

## 7) In-Memory Store Implementation

### 7.1 InMemoryPostStore

```csharp
public sealed class InMemoryPostStore : IPostStore
{
    private readonly ConcurrentDictionary<string, PostDto> _posts = new();
    private readonly ReaderWriterLockSlim _lock = new();
    
    private static string GetKey(string tenantId, string postId) => $"{tenantId}|{postId}";
    
    public Task<PostDto> UpsertAsync(PostDto post, CancellationToken ct)
    {
        var key = GetKey(post.TenantId, post.Id!);
        _posts[key] = Clone(post);
        return Task.FromResult(Clone(post));
    }
    
    public Task<PostDto?> GetByIdAsync(string tenantId, string postId, CancellationToken ct)
    {
        var key = GetKey(tenantId, postId);
        return Task.FromResult(_posts.TryGetValue(key, out var post) ? Clone(post) : null);
    }
    
    public Task<ContentPageResult<PostDto>> QueryAsync(PostQuery query, CancellationToken ct)
    {
        _lock.EnterReadLock();
        try
        {
            var candidates = _posts.Values
                .Where(p => p.TenantId == query.TenantId)
                .Where(p => query.IncludeDeleted || !p.IsDeleted);
            
            if (query.Author != null)
                candidates = candidates.Where(p => p.Author.Id == query.Author.Id);
            
            var sorted = candidates.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id);
            
            var offset = DecodeCursor(query.Cursor);
            var page = sorted.Skip(offset).Take(query.Limit + 1).ToList();
            var hasMore = page.Count > query.Limit;
            if (hasMore) page = page.Take(query.Limit).ToList();
            
            return Task.FromResult(new ContentPageResult<PostDto>
            {
                Items = page.Select(Clone).ToList(),
                NextCursor = hasMore ? EncodeCursor(offset + query.Limit) : null,
                TotalCount = -1
            });
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
    
    public Task IncrementCommentCountAsync(string tenantId, string postId, int delta, CancellationToken ct)
    {
        var key = GetKey(tenantId, postId);
        if (_posts.TryGetValue(key, out var post))
        {
            post.CommentCount += delta;
        }
        return Task.CompletedTask;
    }
    
    public Task UpdateReactionCountAsync(string tenantId, string postId, ReactionType type, int delta, CancellationToken ct)
    {
        var key = GetKey(tenantId, postId);
        if (_posts.TryGetValue(key, out var post))
        {
            if (!post.ReactionCounts.ContainsKey(type))
                post.ReactionCounts[type] = 0;
            post.ReactionCounts[type] += delta;
            if (post.ReactionCounts[type] <= 0)
                post.ReactionCounts.Remove(type);
        }
        return Task.CompletedTask;
    }
}
```

---

## 8) File Structure

```
src/
├── Content.Abstractions/
│   ├── Content.Abstractions.csproj
│   ├── README.md
│   │
│   ├── DTOs/
│   │   ├── PostDto.cs
│   │   ├── CommentDto.cs
│   │   ├── ReactionDto.cs
│   │   └── EntityRefDto.cs
│   │
│   ├── Enums/
│   │   ├── ContentVisibility.cs
│   │   ├── ReactionType.cs
│   │   └── ReactionTargetKind.cs
│   │
│   ├── Requests/
│   │   ├── CreatePostRequest.cs
│   │   ├── UpdatePostRequest.cs
│   │   ├── DeletePostRequest.cs
│   │   ├── CreateCommentRequest.cs
│   │   ├── UpdateCommentRequest.cs
│   │   ├── DeleteCommentRequest.cs
│   │   ├── ReactRequest.cs
│   │   └── RemoveReactionRequest.cs
│   │
│   ├── Queries/
│   │   ├── PostQuery.cs
│   │   ├── CommentQuery.cs
│   │   └── ReactionQuery.cs
│   │
│   ├── Results/
│   │   └── ContentPageResult.cs
│   │
│   ├── Interfaces/
│   │   ├── IContentService.cs
│   │   ├── IPostStore.cs
│   │   ├── ICommentStore.cs
│   │   ├── IReactionStore.cs
│   │   └── IIdGenerator.cs
│   │
│   └── Validation/
│       ├── ContentValidationError.cs
│       └── ContentValidationException.cs
│
├── Content.Core/
│   ├── Content.Core.csproj
│   ├── README.md
│   ├── ContentService.cs
│   ├── ContentServiceOptions.cs
│   ├── ContentValidator.cs
│   ├── ContentNormalizer.cs
│   └── UlidIdGenerator.cs
│
├── Content.Store.InMemory/
│   ├── Content.Store.InMemory.csproj
│   ├── README.md
│   ├── InMemoryPostStore.cs
│   ├── InMemoryCommentStore.cs
│   └── InMemoryReactionStore.cs
│
tests/
└── Content.Tests/
    ├── Content.Tests.csproj
    ├── ContentServiceTests.cs
    ├── PostStoreTests.cs
    ├── CommentStoreTests.cs
    ├── ReactionStoreTests.cs
    └── ValidationTests.cs
```

---

## 9) Implementation Order

### Day 1: Abstractions (0.5 day)

1. Create `Content.Abstractions.csproj` (no dependencies)
2. Create enums: `ContentVisibility`, `ReactionType`, `ReactionTargetKind`
3. Create DTOs: `EntityRefDto`, `PostDto`, `CommentDto`, `ReactionDto`
4. Create request types (8 files)
5. Create query types (3 files)
6. Create `ContentPageResult<T>`
7. Create interfaces: `IContentService`, `IPostStore`, `ICommentStore`, `IReactionStore`, `IIdGenerator`
8. Create validation types

### Day 1-2: Core (1 day)

1. Create `Content.Core.csproj` (references Abstractions)
2. Implement `ContentServiceOptions`
3. Implement `ContentValidator` (static validation methods)
4. Implement `ContentNormalizer` (text cleanup)
5. Implement `UlidIdGenerator`
6. Implement `ContentService`:
   - Post CRUD
   - Comment CRUD
   - Reaction add/remove
   - Count maintenance

### Day 2-3: In-Memory Store (0.5 day)

1. Create `Content.Store.InMemory.csproj`
2. Implement `InMemoryPostStore`
3. Implement `InMemoryCommentStore`
4. Implement `InMemoryReactionStore`
5. Add cursor encoding/decoding helpers

### Day 3: Tests (0.5 day)

1. Create `Content.Tests.csproj`
2. Write `ContentServiceTests`:
   - Post lifecycle (create, read, update, delete)
   - Comment lifecycle with threading
   - Reaction add/change/remove
   - Count accuracy
3. Write `ValidationTests`
4. Write store-specific tests

---

## 10) Testing Strategy

### 10.1 Unit Tests (ContentService)

```csharp
[Fact]
public async Task CreatePost_ValidRequest_ReturnsPostWithId()
{
    var service = CreateService();
    var post = await service.CreatePostAsync(new CreatePostRequest
    {
        TenantId = "tenant1",
        Author = new EntityRefDto { Type = "Profile", Id = "user1", DisplayName = "John" },
        Body = "Hello, world!"
    });
    
    Assert.NotNull(post.Id);
    Assert.Equal("Hello, world!", post.Body);
    Assert.Equal(0, post.CommentCount);
}

[Fact]
public async Task React_NewReaction_IncrementsCount()
{
    var service = CreateService();
    var post = await CreateTestPost(service);
    
    await service.ReactAsync(new ReactRequest
    {
        TenantId = "tenant1",
        Actor = new EntityRefDto { Type = "Profile", Id = "user2" },
        TargetId = post.Id!,
        TargetKind = ReactionTargetKind.Post,
        Type = ReactionType.Like
    });
    
    var updated = await service.GetPostAsync("tenant1", post.Id!);
    Assert.Equal(1, updated!.ReactionCounts[ReactionType.Like]);
}

[Fact]
public async Task React_ChangeReaction_UpdatesCounts()
{
    // ... previous Like → change to Love
    // Assert: Like count = 0, Love count = 1
}
```

### 10.2 Store Tests

Test each store independently with the same test patterns, allowing future SQL/NoSQL stores to pass the same tests.

---

## 11) Future Extensions

### 11.1 Alternative Store Implementations

```
Content.Store.SqlServer/
  └── SqlPostStore.cs, SqlCommentStore.cs, SqlReactionStore.cs

Content.Store.Cosmos/
  └── CosmosPostStore.cs, CosmosCommentStore.cs, CosmosReactionStore.cs

Content.Store.Postgres/
  └── NpgsqlPostStore.cs, NpgsqlCommentStore.cs, NpgsqlReactionStore.cs
```

### 11.2 Activity Stream Integration

```csharp
public interface IActivityPublisher
{
    Task PublishAsync(string verb, object entity, CancellationToken ct = default);
}

// Wire up in DI:
services.AddScoped<IActivityPublisher, ActivityStreamPublisher>();
```

### 11.3 Media Service Integration

When creating a post with `MediaIds`, optionally validate they exist via `IMediaService`.

---

## 12) Estimate Summary

| Phase | Task | Days |
|-------|------|------|
| 1 | Content.Abstractions | 0.5 |
| 2 | Content.Core | 1.0 |
| 3 | Content.Store.InMemory | 0.5 |
| 4 | Content.Tests | 0.5 |
| **Total** | | **2.5 days** |

---

## 13) Next Steps

1. **Create Content.Abstractions project** with all DTOs, enums, interfaces
2. **Create Content.Core project** with ContentService
3. **Create Content.Store.InMemory project** with in-memory stores
4. **Create Content.Tests project** with comprehensive tests
5. **Build and verify** all tests pass

Ready to implement? Say "go" to start with Content.Abstractions.
