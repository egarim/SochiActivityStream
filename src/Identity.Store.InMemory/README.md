# Identity.Store.InMemory

In-memory reference implementation of Identity stores.

## Usage

```csharp
// Create stores
var userStore = new InMemoryUserStore();
var profileStore = new InMemoryProfileStore();
var membershipStore = new InMemoryMembershipStore();
var sessionStore = new InMemorySessionStore();

// Use with services
var authService = new AuthService(
    userStore,
    profileStore,
    membershipStore,
    sessionStore,
    new Pbkdf2PasswordHasher(),
    new UlidIdGenerator());
```

## Stores

### InMemoryUserStore

- Primary index: `userId → UserRecord`
- Secondary indexes: `normalizedEmail → userId`, `normalizedUsername → userId`

### InMemoryProfileStore

- Primary index: `profileId → ProfileRecord`
- Secondary index: `normalizedHandle → profileId`

### InMemoryMembershipStore

- Composite key: `{tenantId}|{userId}|{profileId}`
- Supports queries by tenant+user and tenant+profile

### InMemorySessionStore

- Primary index: `sessionId → SessionRecord`
- Secondary index: `accessToken → sessionId`

## Thread Safety

All stores use `ConcurrentDictionary` with additional `lock` for multi-step operations to ensure thread safety.

## Notes

This is a **reference implementation** for development and testing. For production, implement the store interfaces with a proper database (PostgreSQL, CosmosDB, etc.).
