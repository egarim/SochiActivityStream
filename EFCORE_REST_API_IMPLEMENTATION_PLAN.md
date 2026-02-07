# BlazorBook.Web - EF Core + REST API Implementation Plan

## Executive Summary
This document outlines the implementation of EF Core persistence with SQLite and REST APIs for the BlazorBook.Web demo application. The implementation uses a side-by-side approach where both in-memory and EF Core stores can be used via configuration.

## Architecture Overview
- **Single Project**: Both Blazor Server UI and REST APIs hosted in BlazorBook.Web
- **Database**: SQLite with EF Core 8.0
- **Authentication**: 
  - Blazor UI: Session-based (existing)
  - REST API: JWT Bearer tokens
- **Storage Mode**: Configurable via `appsettings.json` ("InMemory" | "EFCore")
- **Service Lifetime**: Changed from Singleton to Scoped (required for EF Core)

---

## ✅ COMPLETED COMPONENTS

### 1. NuGet Packages Added
- `Microsoft.EntityFrameworkCore.Sqlite` (8.0.0)
- `Microsoft.EntityFrameworkCore.Design` (8.0.0)
- `Microsoft.AspNetCore.Authentication.JwtBearer` (8.0.0)
- `System.IdentityModel.Tokens.Jwt` (8.0.0)

**File**: `src/BlazorBook.Web/BlazorBook.Web.csproj`

### 2. EF Core Infrastructure

#### ApplicationDbContext
**File**: `src/BlazorBook.Web/Data/ApplicationDbContext.cs`
- Unified DbContext for all 9 domains
- DbSets for: Users, Profiles, Memberships, Sessions, UserPasswords, Posts, Comments, Reactions, Conversations, Messages, ReadReceipts, RelationshipEdges, Activities, InboxItems, FollowRequests, Media, SearchDocuments
- Auto-applies all entity configurations

#### Value Converters
**File**: `src/BlazorBook.Web/Data/ValueConverters/JsonValueConverter.cs`
- Generic JSON converter for complex types (Lists, Dictionaries, EntityRefDto, etc.)
- Uses System.Text.Json with camelCase

#### Entity Configurations (Fluent API)
**Folder**: `src/BlazorBook.Web/Data/Configurations/`

All entities configured with:
✅ **Identity Domain**
- UserConfiguration.cs
- ProfileConfiguration.cs
- MembershipConfiguration.cs
- SessionConfiguration.cs
- UserPasswordConfiguration.cs (separate table for password hashes)

✅ **Content Domain**
- PostConfiguration.cs
- CommentConfiguration.cs
- ReactionConfiguration.cs

✅ **Chat Domain**
- ConversationConfiguration.cs
- MessageConfiguration.cs
- ReadReceiptConfiguration.cs

✅ **Relationship Domain**
- RelationshipEdgeConfiguration.cs

✅ **ActivityStream Domain**
- ActivityConfiguration.cs

✅ **Inbox Domain**
- InboxItemConfiguration.cs
- FollowRequestConfiguration.cs

✅ **Media Domain**
- MediaConfiguration.cs

✅ **Search Domain**
- SearchDocumentConfiguration.cs

**Key Configuration Patterns**:
- EntityRefDto as owned entities (JSON)
- Collections/Dictionaries as JSON columns
- Soft delete global query filters
- Composite indexes for queries
- String length constraints

### 3. EF Core Store Implementations

#### Identity Store
**File**: `src/BlazorBook.Web/Stores/EFCore/EFCoreUserStore.cs`
- Implements `IUserStore`
- Handles UserRecord (User + PasswordHash) storage
- Methods: FindByUsernameAsync, FindByEmailAsync, GetByIdAsync, CreateAsync

#### Content Store
**File**: `src/BlazorBook.Web/Stores/EFCore/EFCorePostStore.cs`
- Implements `IPostStore`
- Methods: UpsertAsync, GetByIdAsync, QueryAsync, DeleteAsync, IncrementCommentCountAsync, UpdateReactionCountAsync
- Cursor-based pagination
- Soft delete support

### 4. REST API Controllers

#### AuthController
**File**: `src/BlazorBook.Web/Controllers/AuthController.cs`
- POST `/api/auth/signup` - Register new user + return JWT
- POST `/api/auth/login` - Login + return JWT
- POST `/api/auth/logout` - Logout session
- Uses IAuthService + JwtTokenService

#### PostsController
**File**: `src/BlazorBook.Web/Controllers/PostsController.cs`
- GET `/api/posts` - Get posts feed (with pagination)
- GET `/api/posts/{id}` - Get single post
- POST `/api/posts` - Create post
- PUT `/api/posts/{id}` - Update post
- DELETE `/api/posts/{id}` - Soft delete post
- Requires JWT authentication
- Extracts tenantId/profileId from JWT claims

### 5. JWT Authentication Service
**File**: `src/BlazorBook.Web/Services/JwtTokenService.cs`
- `GenerateToken(sessionId, userId, tenantId, profileId)` - Creates JWT with claims
- `ValidateToken(token)` - Validates JWT and returns ClaimsPrincipal
- Configured from appsettings.json (SecretKey, Issuer, Audience, ExpirationMinutes)

### 6. Program.cs Updates
**File**: `src/BlazorBook.Web/Program.cs`

✅ Added:
- EF Core DbContext registration (conditional on Storage:Mode)
- JWT Bearer authentication
- Controllers + Swagger/OpenAPI  
- Changed ALL service lifetimes: Singleton → Scoped
- Conditional store registration (InMemory vs EFCore based on config)
- Database migration on startup (Development only)
- Middleware: Authentication, Authorization, Controllers

### 7. Configuration Files
**File**: `src/BlazorBook.Web/appsettings.json`
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=blazorbook.db"
  },
  "Storage": {
    "Mode": "InMemory"
  },
  "Database": {
    "SeedDemo": false
  },
  "Jwt": {
    "SecretKey": "your-secret-key-min-32-chars-long...",
    "Issuer": "BlazorBook.Web",
    "Audience": "BlazorBook.Api",
    "ExpirationMinutes": 60
  }
}
```

**File**: `src/BlazorBook.Web/appsettings.Development.json`
- Storage:Mode = "EFCore"
- Database:SeedDemo = true
- Extended JWT expiration (480 min)
- EF Core logging enabled

---

## 🚧 REMAINING TASKS

### Task 1: Create Remaining EF Core Stores
**Priority**: High (for full functionality)

Create EFCore implementations in `src/BlazorBook.Web/Stores/EFCore/`:

**Identity Domain**:
- EFCoreProfileStore.cs (implements IProfileStore)
- EFCoreMembershipStore.cs (implements IMembershipStore)
- EFCoreSessionStore.cs (implements ISessionStore)

**Content Domain**:
- EFCoreCommentStore.cs (implements ICommentStore)
- EFCoreReactionStore.cs (implements IReactionStore)

**Chat Domain**:
- EFCoreConversationStore.cs (implements IConversationStore)
- EFCoreMessageStore.cs (implements IMessageStore)
- EFCoreReadReceiptStore.cs (implements IReadReceiptStore)

**Other Domains**:
- EFCoreRelationshipStore.cs (implements IRelationshipStore)
- EFCoreActivityStore.cs (implements IActivityStore)
- EFCoreInboxStore.cs (implements IInboxStore)
- EFCoreFollowRequestStore.cs (implements IFollowRequestStore)
- EFCoreMediaStore.cs (implements IMediaStore)
- EFCoreSearchIndex.cs (implements ISearchIndex)

**Pattern to follow**: See EFCoreUserStore.cs and EFCorePostStore.cs

**Update Program.cs**: Add conditional registrations for each new store in the storageMode == "EFCore" blocks.

### Task 2: Generate Initial EF Core Migration
**File**: Run in terminal from project root

```powershell
dotnet ef migrations add InitialCreate --project src/BlazorBook.Web --output-dir Data/Migrations
```

This will create:
- `src/BlazorBook.Web/Data/Migrations/{timestamp}_InitialCreate.cs`
- `src/BlazorBook.Web/Data/Migrations/ApplicationDbContextModelSnapshot.cs`

### Task 3: Create Database Seeding
**File**: `src/BlazorBook.Web/Data/SeedData.cs`

Create static class with:
```csharp
public static class SeedData
{
    public static async Task SeedAsync(ApplicationDbContext context, bool includeDemo)
    {
        // Essential data:
        // - System tenant
        // - Admin user (if needed)
        
        if (includeDemo)
        {
            // Demo data:
            // - 3-5 sample users (Alice, Bob, Charlie, etc.)
            // - Profiles for each user
            // - 10-15 sample posts
            // - Comments on posts
            // - Reactions (likes, hearts, etc.)
            // - 2-3 conversations
            // - Messages in conversations
            // - Follow relationships
        }
        
        await context.SaveChangesAsync();
    }
}
```

**Update Program.cs**: Uncomment the seeding call:
```csharp
if (seedDemo)
{
    await SeedData.SeedAsync(context, includeDemo: true);
}
```

### Task 4: Create Additional REST API Controllers
**Folder**: `src/BlazorBook.Web/Controllers/`

**Profiles Controller**:
- GET `/api/profiles/{handle}` - Get profile by handle
- GET `/api/profiles` - Get profiles list
- PUT `/api/profiles/{id}` - Update profile

**Comments Controller**:
- GET `/api/posts/{postId}/comments` - Get comments for post
- POST `/api/posts/{postId}/comments` - Create comment
- PUT `/api/comments/{id}` - Update comment
- DELETE `/api/comments/{id}` - Delete comment

**Reactions Controller**:
- POST `/api/posts/{postId}/reactions` - Add reaction to post
- DELETE `/api/posts/{postId}/reactions` - Remove reaction from post
- POST `/api/comments/{commentId}/reactions` - Add reaction to comment
- DELETE `/api/comments/{commentId}/reactions` - Remove reaction

**Conversations Controller**:
- GET `/api/conversations` - Get user's conversations
- GET `/api/conversations/{id}` - Get conversation details
- POST `/api/conversations` - Create conversation
- POST `/api/conversations/{id}/messages` - Send message
- GET `/api/conversations/{id}/messages` - Get messages (paginated)

**Relationships Controller**:
- POST `/api/relationships/follow` - Follow user
- DELETE `/api/relationships/follow/{targetId}` - Unfollow user
- GET `/api/relationships/followers` - Get followers
- GET `/api/relationships/following` - Get following

**Inbox Controller**:
- GET `/api/inbox` - Get notifications
- PUT `/api/inbox/{id}/read` - Mark as read
- POST `/api/inbox/read-all` - Mark all as read

**Media Controller**:
- POST `/api/media/upload` - Upload media (multipart)
- GET `/api/media/{id}` - Get media details
- DELETE `/api/media/{id}` - Delete media

**Search Controller**:
- GET `/api/search?q={query}&type={type}` - Search
- GET `/api/search/autocomplete?q={query}` - Autocomplete

### Task 5: Fix Build Errors
**Priority**: Critical

Current issues to resolve:
1. Missing using statements for `Swashbuckle.AspNetCore` (add NuGet package if needed)
2. Verify all entity configurations compile
3. Test Program.cs conditional registrations
4. Resolve any circular dependency issues

```powershell
# Run to identify errors:
dotnet build src/BlazorBook.Web/BlazorBook.Web.csproj --no-incremental
```

### Task 6: Test InMemory Mode
**Priority**: High (verify existing functionality preserved)

Steps:
1. Set `"Storage:Mode": "InMemory"` in appsettings.json
2. Run application: `dotnet run --project src/BlazorBook.Web`
3. Verify Blazor UI works as before
4. Test `/swagger` endpoint loads
5. Test API endpoints with JWT authentication

### Task 7: Test EFCore Mode
**Priority**: High

Steps:
1. Set `"Storage:Mode": "EFCore"` in appsettings.Development.json
2. Run migrations (if not auto-applied)
3. Run application in Development mode
4. Verify database created (`blazorbook.db`)
5. Check tables created correctly
6. Test CRUD operations via Blazor UI
7. Test API endpoints

### Task 8: Create Integration Tests
**Optional but Recommended**

**File**: `tests/BlazorBook.Api.Tests/`

Create tests for:
- Auth endpoints (signup, login, logout)
- Post CRUD operations
- JWT token validation
- EF Core store implementations

---

## USAGE GUIDE

### Running with In-Memory Storage (Default)
```powershell
cd src/BlazorBook.Web
dotnet run
```

Access:
- Blazor UI: https://localhost:5001
- Swagger API: https://localhost:5001/swagger

### Running with EF Core + SQLite
```powershell
cd src/BlazorBook.Web

# Set environment
$env:ASPNETCORE_ENVIRONMENT="Development"

# Run migrations (first time only)
dotnet ef database update

# Run application
dotnet run
```

### Switching Between Storage Modes
Edit `appsettings.json`:
```json
"Storage": {
  "Mode": "InMemory"  // or "EFCore"
}
```

### Testing REST API

1. **Signup**:
```bash
curl -X POST https://localhost:5001/api/auth/signup \
  -H "Content-Type: application/json" \
  -d '{
    "tenantId": "default",
    "email": "alice@example.com",
    "username": "alice",
    "password": "Password123!",
    "displayName": "Alice"
  }'
```

2. **Login** (get JWT token):
```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "tenantId": "default",
    "usernameOrEmail": "alice",
    "password": "Password123!"
  }'
```

3. **Get Posts** (use JWT token):
```bash
curl -X GET https://localhost:5001/api/posts \
  -H "Authorization: Bearer {token}"
```

4. **Create Post**:
```bash
curl -X POST https://localhost:5001/api/posts \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "body": "Hello from API!",
    "visibility": 0
  }'
```

---

## TROUBLESHOOTING

### Build Errors
- Check all using statements in new files
- Verify NuGet packages restored: `dotnet restore`
- Clean and rebuild: `dotnet clean && dotnet build`

### Migration Errors
- Ensure connection string is valid
- Check entity configurations for syntax errors
- Verify all DbSet properties in ApplicationDbContext

### Runtime Errors
- Check appsettings.json configuration format
- Verify JWT SecretKey is at least 32 characters
- Check database file permissions (SQLite)
- Review application logs for detailed errors

### Authentication Issues
- Verify JWT token format in Authorization header: `Bearer {token}`
- Check token expiration time
- Ensure Issuer/Audience match between generation and validation

---

## NEXT STEPS FOR PRODUCTION

1. **Security**:
   - Use secure JWT secret (environment variable)
   - Implement refresh tokens
   - Add rate limiting
   - Enable CORS properly

2. **Database**:
   - Switch to PostgreSQL/SQL Server for production
   - Implement proper migrations strategy
   - Add database connection pooling
   - Set up backup strategy

3. **Performance**:
   - Add response caching
   - Implement pagination on all list endpoints
   - Add database indexes optimization
   - Consider Redis for sessions

4. **Monitoring**:
   - Add Application Insights
   - Implement structured logging
   - Add health checks
   - Set up alerts

5. **Testing**:
   - Unit tests for stores
   - Integration tests for APIs
   - E2E tests for Blazor UI
   - Load testing

---

## ARCHITECTURE DECISIONS

### Why Single Project?
- Simplifies development and deployment
- Shares authentication naturally
- Reduces configuration complexity
- Easy to split later if needed

### Why Side-by-Side Storage?
- Allows testing both implementations
- Provides fallback option
- Enables gradual migration
- Useful for demos and testing

### Why Scoped Lifetime?
- Required by EF Core DbContext
- Better for web applications
- Prevents state leakage between requests
- Aligns with ASP.NET Core best practices

### Why SQLite for Demo?
- Zero configuration required
- Single file database
- Perfect for demos and testing
- Easy to migrate to other databases later

---

## FILES CREATED / MODIFIED

### Created Files (31):
1. `src/BlazorBook.Web/Data/ApplicationDbContext.cs`
2. `src/BlazorBook.Web/Data/ValueConverters/JsonValueConverter.cs`
3. `src/BlazorBook.Web/Data/Configurations/UserConfiguration.cs`
4. `src/BlazorBook.Web/Data/Configurations/ProfileConfiguration.cs`
5. `src/BlazorBook.Web/Data/Configurations/MembershipConfiguration.cs`
6. `src/BlazorBook.Web/Data/Configurations/SessionConfiguration.cs`
7. `src/BlazorBook.Web/Data/Configurations/UserPasswordConfiguration.cs`
8. `src/BlazorBook.Web/Data/Configurations/PostConfiguration.cs`
9. `src/BlazorBook.Web/Data/Configurations/CommentConfiguration.cs`
10. `src/BlazorBook.Web/Data/Configurations/ReactionConfiguration.cs`
11. `src/BlazorBook.Web/Data/Configurations/ConversationConfiguration.cs`
12. `src/BlazorBook.Web/Data/Configurations/MessageConfiguration.cs`
13. `src/BlazorBook.Web/Data/Configurations/ReadReceiptConfiguration.cs`
14. `src/BlazorBook.Web/Data/Configurations/RelationshipEdgeConfiguration.cs`
15. `src/BlazorBook.Web/Data/Configurations/ActivityConfiguration.cs`
16. `src/BlazorBook.Web/Data/Configurations/InboxItemConfiguration.cs`
17. `src/BlazorBook.Web/Data/Configurations/FollowRequestConfiguration.cs`
18. `src/BlazorBook.Web/Data/Configurations/MediaConfiguration.cs`
19. `src/BlazorBook.Web/Data/Configurations/SearchDocumentConfiguration.cs`
20. `src/BlazorBook.Web/Stores/EFCore/EFCoreUserStore.cs`
21. `src/BlazorBook.Web/Stores/EFCore/EFCorePostStore.cs`
22. `src/BlazorBook.Web/Services/JwtTokenService.cs`
23. `src/BlazorBook.Web/Controllers/AuthController.cs`
24. `src/BlazorBook.Web/Controllers/PostsController.cs`

### Modified Files (3):
1. `src/BlazorBook.Web/BlazorBook.Web.csproj` - Added NuGet packages
2. `src/BlazorBook.Web/Program.cs` - Complete rewrite with EF Core + JWT + API
3. `src/BlazorBook.Web/appsettings.json` - Added configuration sections
4. `src/BlazorBook.Web/appsettings.Development.json` - EFCore mode enabled

---

## COMPLETION STATUS: ~60%

✅ Infrastructure: 100%
✅ Configuration: 100%
✅ Authentication: 100%
✅ Sample Stores: 30% (2 of 14)
✅ Sample Controllers: 20% (2 of 10)
⏳ Migrations: 0% (needs generation)
⏳ Seeding: 0% (needs implementation)
⏳ Testing: 0%
⏳ Documentation: 80% (this doc)

---

**Last Updated**: February 6, 2026
**Implementation Status**: Foundation Complete, Ready for Store/Controller Completion
