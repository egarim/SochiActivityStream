# Content.Abstractions

Core abstractions for the Content Service — defines DTOs, interfaces, and validation types for managing posts, comments, and reactions.

## Purpose

This project contains **only interfaces and DTOs** with zero external dependencies. It defines the contract for:

- **Posts**: User-generated content with body, media references, visibility
- **Comments**: Threaded replies to posts
- **Reactions**: Lightweight responses (Like, Love, Haha, Wow, Sad, Angry)

## Key Types

### DTOs
- `PostDto` - Post with author, body, media, visibility, denormalized counts
- `CommentDto` - Comment with threading support
- `ReactionDto` - Reaction linking actor to target

### Enums
- `ContentVisibility` - Public, Friends, Private
- `ReactionType` - Like, Love, Haha, Wow, Sad, Angry
- `ReactionTargetKind` - Post or Comment

### Interfaces
- `IContentService` - Main API for CRUD operations
- `IPostStore` - Storage abstraction for posts
- `ICommentStore` - Storage abstraction for comments
- `IReactionStore` - Storage abstraction for reactions
- `IIdGenerator` - ID generation abstraction

### Validation
- `ContentValidationError` - Error codes
- `ContentValidationException` - Validation exception

## Pluggable Architecture

```
IContentService (business logic)
       │
       ▼
ContentService (in Content.Core)
       │
       ├── IPostStore ───► InMemoryPostStore / SqlPostStore / CosmosPostStore
       ├── ICommentStore ► InMemoryCommentStore / ...
       └── IReactionStore► InMemoryReactionStore / ...
```

## Usage

```csharp
// Implement stores for your database
public class SqlPostStore : IPostStore { ... }

// Wire up with DI
services.AddScoped<IPostStore, SqlPostStore>();
services.AddScoped<ICommentStore, SqlCommentStore>();
services.AddScoped<IReactionStore, SqlReactionStore>();
services.AddScoped<IContentService, ContentService>();
```
