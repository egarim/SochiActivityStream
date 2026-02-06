# Content.Core

Business logic implementation for the Content Service.

## Purpose

This project implements `IContentService` and orchestrates the stores to manage posts, comments, and reactions.

## Key Components

- **ContentService** - Main service implementation
- **ContentValidator** - Request validation logic
- **ContentNormalizer** - Text normalization
- **ContentServiceOptions** - Configuration
- **UlidIdGenerator** - ULID-based ID generation

## Features

- Post CRUD with soft delete
- Comment threading (nested replies)
- Reaction management (add/change/remove)
- Denormalized count maintenance
- Per-viewer reaction population
- Multi-tenant isolation

## Usage

```csharp
// Create with in-memory stores
var postStore = new InMemoryPostStore();
var commentStore = new InMemoryCommentStore();
var reactionStore = new InMemoryReactionStore();
var idGenerator = new UlidIdGenerator();

var service = new ContentService(
    postStore, 
    commentStore, 
    reactionStore, 
    idGenerator);

// Create a post
var post = await service.CreatePostAsync(new CreatePostRequest
{
    TenantId = "tenant1",
    Author = new EntityRefDto { Type = "Profile", Id = "user1", DisplayName = "John" },
    Body = "Hello, world!"
});

// Add a comment
var comment = await service.CreateCommentAsync(new CreateCommentRequest
{
    TenantId = "tenant1",
    Author = new EntityRefDto { Type = "Profile", Id = "user2" },
    PostId = post.Id!,
    Body = "Nice post!"
});

// React to the post
var reaction = await service.ReactAsync(new ReactRequest
{
    TenantId = "tenant1",
    Actor = new EntityRefDto { Type = "Profile", Id = "user2" },
    TargetId = post.Id!,
    TargetKind = ReactionTargetKind.Post,
    Type = ReactionType.Like
});

// Get post with viewer's reaction
var postWithReaction = await service.GetPostAsync("tenant1", post.Id!, viewer: new EntityRefDto { Type = "Profile", Id = "user2" });
// postWithReaction.ViewerReaction == ReactionType.Like
```

## Configuration

```csharp
var options = new ContentServiceOptions
{
    MaxPostBodyLength = 10_000,
    MaxCommentBodyLength = 5_000,
    DefaultPageSize = 20,
    MaxPageSize = 100
};

var service = new ContentService(postStore, commentStore, reactionStore, idGenerator, options);
```
