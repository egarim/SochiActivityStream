# Content.Store.InMemory

In-memory reference implementation of the Content Service stores.

## Purpose

Provides thread-safe, in-memory implementations of:

- `InMemoryPostStore` - IPostStore
- `InMemoryCommentStore` - ICommentStore
- `InMemoryReactionStore` - IReactionStore

Useful for:
- Unit testing without database dependencies
- Local development
- Prototyping

## Features

- Thread-safe with `ConcurrentDictionary` and `ReaderWriterLockSlim`
- Cursor-based pagination (base64-encoded offsets)
- Cloning to prevent external mutation
- Atomic count operations

## Usage

```csharp
var postStore = new InMemoryPostStore();
var commentStore = new InMemoryCommentStore();
var reactionStore = new InMemoryReactionStore();
var idGenerator = new UlidIdGenerator();

var service = new ContentService(
    postStore,
    commentStore,
    reactionStore,
    idGenerator);
```

## Limitations

- Data is not persisted across restarts
- No replication or horizontal scaling
- TotalCount always returns -1 (could be implemented but expensive)

## Implementing Other Stores

To create a database-backed implementation:

```csharp
public class SqlPostStore : IPostStore
{
    private readonly IDbConnection _db;
    
    public async Task<PostDto> UpsertAsync(PostDto post, CancellationToken ct)
    {
        // INSERT ... ON CONFLICT UPDATE
        await _db.ExecuteAsync("...", post);
        return post;
    }
    
    // ... other methods
}
```

The same pattern applies for NoSQL stores (Cosmos, MongoDB, etc.).
