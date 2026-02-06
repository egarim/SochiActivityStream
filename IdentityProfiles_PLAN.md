# Identity + Profiles Library (Multi-tenant, Global Profiles, v1 username+email+password, v2 Keycloak-ready)

**Goal:** Create a small, **framework-agnostic** C# library that provides:
- **Sign up / sign in** with **username + email + password** (v1)
- A first-class **Profile** concept:
  - users can have many profiles
  - profiles can be shared by multiple users
  - profiles can be private
- **Multi-tenant** support (many tenant apps hosted in the same backend)
- A **simple session** model suitable for **Blazor Hybrid mobile**
- Clean adapter points to switch to **Keycloak/OIDC** later without breaking the system

This library integrates cleanly with:
- Activity Stream (Actor/Owner/Targets are EntityRefDto)
- Relationship Service (edges From/To are EntityRefDto; typically Profiles)
- Inbox Notification Service (Recipient is EntityRefDto; typically Profiles)
- SignalR later (hub group join authorization based on profile membership)

---

## 0) Decisions locked in (from our conversation)
- Multi-tenant: **YES**
- Credentials: **Username + Email + Password**
- Profiles are **GLOBAL entities** (same profile can exist across many tenants)
- Profile membership is **TENANT-SCOPED**
- Profile handle uniqueness: **global across tenants**
- Email verification in v1: **NONE**
- Session strategy v1: **simple** (opaque access token, no refresh required in v1)
- Private profiles: **YES**
- Following private profiles: **YES, always requires approval** via Inbox request workflow

---

## 1) Core Concepts

### 1.1 User (identity principal)
- Global identity: email, username, password hash (v1)
- Later: maps to Keycloak subject (v2)
- Can be a member of 0..N profiles per tenant

### 1.2 Profile (actor/persona/inbox owner)
- Global entity: Handle is globally unique
- Is the main “actor” in Activity Stream and the main “owner” in Relationship edges and Inbox items
- Can be shared by many users via memberships (tenant-scoped)

### 1.3 Membership (User ↔ Profile, tenant-scoped)
- Defines which users can use which profile *within a tenant*
- Roles: Owner/Admin/Member/Viewer
- Status: Active/Invited/Disabled

### 1.4 Session (for Blazor Hybrid)
- Opaque AccessToken issued by server
- Contains (UserId, TenantId) context
- Client stores token securely; server validates token for API + SignalR

---

## 2) Projects

1. **Identity.Abstractions**
   - DTOs
   - Interfaces (auth, profiles, membership, stores)
   - Errors/exceptions
   - Password hashing abstractions

2. **Identity.Core**
   - `AuthService` (signup/signin/signout/validate session)
   - `ProfileService` (create profiles, add members, invite members, roles/status)
   - Multi-tenant validation + normalization

3. **Identity.Store.InMemory**
   - In-memory reference stores for users, profiles, memberships, sessions

4. **Identity.Tests**
   - Sign up/sign in flows
   - Password hashing verification
   - Profile creation + sharing
   - Membership/roles correctness
   - Multi-tenant isolation checks
   - Session expiration (optional)

Optional later:
- `Identity.Store.Postgres`
- `Identity.KeycloakAdapter` (OIDC)
- `Identity.AspNetCore` helpers (claims mapping, SignalR auth helpers)

---

## 3) DTOs (v1)

### 3.1 UserDto
```csharp
public sealed class UserDto
{
    public string? Id { get; set; } // "u_..."

    public required string Email { get; set; }
    public required string Username { get; set; }

    public string? DisplayName { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = default;
}
```

### 3.2 SignUpRequest / SignInRequest
```csharp
public sealed class SignUpRequest
{
    public required string Email { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }

    public string? DisplayName { get; set; }
}

public sealed class SignInRequest
{
    public required string Login { get; set; }    // username OR email
    public required string Password { get; set; }
}
```

### 3.3 ProfileDto
```csharp
public sealed class ProfileDto
{
    public string? Id { get; set; } // "p_..."

    public required string Handle { get; set; }   // globally unique
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }

    public bool IsPrivate { get; set; } = false;

    public DateTimeOffset CreatedAt { get; set; } = default;
}
```

### 3.4 MembershipDto (tenant-scoped)
```csharp
public enum ProfileRole { Owner = 0, Admin = 1, Member = 2, Viewer = 3 }
public enum MembershipStatus { Active = 0, Invited = 1, Disabled = 2 }

public sealed class MembershipDto
{
    public string? Id { get; set; }

    public required string TenantId { get; set; }

    public required string UserId { get; set; }
    public required string ProfileId { get; set; }

    public ProfileRole Role { get; set; } = ProfileRole.Member;
    public MembershipStatus Status { get; set; } = MembershipStatus.Active;

    public DateTimeOffset CreatedAt { get; set; } = default;
}
```

### 3.5 SessionDto (tenant-aware)
```csharp
public sealed class SessionDto
{
    public required string SessionId { get; set; }
    public required string UserId { get; set; }
    public required string TenantId { get; set; }

    public required string AccessToken { get; set; } // opaque
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// Convenience cache: profiles this user can act as in TenantId.
    /// </summary>
    public List<string> ProfileIds { get; set; } = new();
}
```

### 3.6 CreateProfileRequest
```csharp
public sealed class CreateProfileRequest
{
    public required string Handle { get; set; }
    public string? DisplayName { get; set; }
    public bool IsPrivate { get; set; } = false;
}
```

### 3.7 Add/Invite member requests
```csharp
public sealed class AddMemberRequest
{
    public required string UserId { get; set; }
    public ProfileRole Role { get; set; } = ProfileRole.Member;
}

public sealed class InviteMemberRequest
{
    public required string Login { get; set; } // email OR username
    public ProfileRole Role { get; set; } = ProfileRole.Member;
}
```

---

## 4) EntityRef conventions (stable defaults)

Use these everywhere in your other libs:

- Profile EntityRef:
```csharp
new EntityRefDto { Kind="identity", Type="Profile", Id=profileId }
```

- User EntityRef (rarely used in feeds; mostly internal):
```csharp
new EntityRefDto { Kind="identity", Type="User", Id=userId }
```

---

## 5) Interfaces

### 5.1 IAuthService
```csharp
public interface IAuthService
{
    Task<UserDto> SignUpAsync(SignUpRequest request, CancellationToken ct = default);

    Task<SessionDto> SignInAsync(string tenantId, SignInRequest request, CancellationToken ct = default);

    Task SignOutAsync(string sessionId, CancellationToken ct = default);

    Task<SessionDto?> ValidateAccessTokenAsync(string accessToken, CancellationToken ct = default);
}
```

### 5.2 IProfileService
```csharp
public interface IProfileService
{
    // Profile lifecycle (global profiles)
    Task<ProfileDto> CreateProfileAsync(string tenantId, string userId, CreateProfileRequest request, CancellationToken ct = default);

    // Membership (tenant-scoped)
    Task<IReadOnlyList<ProfileDto>> GetProfilesForUserAsync(string tenantId, string userId, CancellationToken ct = default);

    Task<IReadOnlyList<MembershipDto>> GetMembersAsync(string tenantId, string profileId, CancellationToken ct = default);

    /// <summary>
    /// Direct add (admin/internal): membership becomes Active immediately.
    /// </summary>
    Task AddMemberAsync(string tenantId, string profileId, AddMemberRequest request, CancellationToken ct = default);

    /// <summary>
    /// Invite flow (UX): creates an Invited membership and publishes an inbox notification (via adapter in app layer).
    /// v1 does NOT require email sending; the invite is visible in the invitee inbox.
    /// </summary>
    Task InviteMemberAsync(string tenantId, string profileId, InviteMemberRequest request, CancellationToken ct = default);

    Task AcceptInviteAsync(string tenantId, string profileId, string userId, CancellationToken ct = default);
    Task DeclineInviteAsync(string tenantId, string profileId, string userId, CancellationToken ct = default);

    Task SetMemberStatusAsync(string tenantId, string profileId, string userId, MembershipStatus status, CancellationToken ct = default);
    Task SetMemberRoleAsync(string tenantId, string profileId, string userId, ProfileRole role, CancellationToken ct = default);
}
```

### 5.3 IMembershipQuery (SignalR + governance helper)
```csharp
public interface IMembershipQuery
{
    Task<bool> IsActiveMemberAsync(string tenantId, string userId, string profileId, CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetActiveProfileIdsForUserAsync(string tenantId, string userId, CancellationToken ct = default);
}
```

---

## 6) Stores

### 6.1 IUserStore (global uniqueness for username/email)
```csharp
public interface IUserStore
{
    Task<UserDto?> FindByUsernameAsync(string usernameNormalized, CancellationToken ct = default);
    Task<UserDto?> FindByEmailAsync(string emailNormalized, CancellationToken ct = default);

    Task<UserDto?> GetByIdAsync(string userId, CancellationToken ct = default);

    Task CreateAsync(UserRecord record, CancellationToken ct = default);
}
```

### 6.2 IProfileStore (global uniqueness for handle)
```csharp
public interface IProfileStore
{
    Task<ProfileDto?> GetByIdAsync(string profileId, CancellationToken ct = default);
    Task<ProfileDto?> FindByHandleAsync(string handleNormalized, CancellationToken ct = default);

    Task CreateAsync(ProfileRecord record, CancellationToken ct = default);
}
```

### 6.3 IMembershipStore (tenant-scoped)
```csharp
public interface IMembershipStore
{
    Task<MembershipDto?> FindAsync(string tenantId, string userId, string profileId, CancellationToken ct = default);

    Task<IReadOnlyList<MembershipDto>> GetForUserAsync(string tenantId, string userId, CancellationToken ct = default);
    Task<IReadOnlyList<MembershipDto>> GetForProfileAsync(string tenantId, string profileId, CancellationToken ct = default);

    Task UpsertAsync(MembershipRecord record, CancellationToken ct = default);
    Task DeleteAsync(string tenantId, string userId, string profileId, CancellationToken ct = default);
}
```

### 6.4 ISessionStore (tenant-scoped sessions)
```csharp
public interface ISessionStore
{
    Task CreateAsync(SessionRecord record, CancellationToken ct = default);
    Task<SessionDto?> FindByAccessTokenAsync(string accessToken, CancellationToken ct = default);
    Task RevokeAsync(string sessionId, CancellationToken ct = default);
}
```

---

## 7) Password hashing (v1 simple, secure)
- Use **PBKDF2-HMACSHA256** with per-user random salt
- Store: salt, iterations, hash bytes, algorithm label
- Validate using constant-time compare

```csharp
public interface IPasswordHasher
{
    PasswordHash Hash(string password);
    bool Verify(string password, PasswordHash hash);
}

public sealed record PasswordHash(byte[] Salt, int Iterations, byte[] HashBytes, string Algorithm);
```

---

## 8) Normalization & validation

- TenantId: trim, lower, cap length
- Email: trim, lower, basic validation, cap length, globally unique
- Username: trim, lower, allowed charset, cap length, globally unique
- Handle: trim, lower, allowed charset, cap length, globally unique
- Password: min length 8, cap length 256
- DisplayName: cap length 100

---

## 9) Integration with Inbox requests (private profiles)

Rule:
- If Profile.IsPrivate == true and someone tries to Follow/Subscribe:
  - must create a **FollowRequest** via InboxNotificationService
  - approvers: Profile members with Role Owner/Admin (tenant-scoped)
  - approval creates Relationship edge From=requesterProfile To=privateProfile

> Implementation detail: the Identity library exposes membership info; the Inbox governance policy uses it to determine approvers.

---

## 10) Tests (required)

Auth:
- SignUp creates user + hash stored
- SignIn with username works
- SignIn with email works
- Wrong password fails
- Duplicate email/username blocked (global uniqueness)

Profiles/Membership:
- CreateProfile creates global profile + membership Owner in tenant
- AddMember creates Active membership
- InviteMember creates Invited membership
- AcceptInvite flips to Active
- DeclineInvite removes or sets Disabled (choose and test)
- Shared profile: two users are Active members in same tenant

Multi-tenant:
- Membership in tenant A does not allow access in tenant B
- Same profile can have different roles per tenant (if needed; at minimum membership exists per tenant)

Sessions:
- ValidateAccessToken returns session until expiry
- SignOut revokes session

---

## 11) Milestones (agent execution order)

M0 Scaffold
- solution + projects, nullable/analyzers, xUnit

M1 Abstractions
- DTOs + interfaces + errors
- password hasher abstraction

M2 Core
- AuthService implementation
- ProfileService implementation (create/add/invite/accept/decline)
- normalization + validation

M3 InMemory stores
- thread-safe dictionaries + indexes

M4 Tests
- all green

M5 README
- examples, EntityRef conventions, multi-tenant flows

---

## 12) Invite delivery (locked)

Invites to share a profile with another user will be delivered via **InboxNotificationService** (recommended).

Implementation approach (keeps Identity library agnostic):
- Identity.Core publishes a domain event (or calls an injected sink) on `InviteMemberAsync`:
  - `ProfileMemberInvited` (tenantId, profileId, invitedUserId, role)
- The app layer (Blazor backend) listens and creates an Inbox item of Kind=Request or Notification for the invitee’s **Profile inbox** (or user inbox if you later add that concept).
- Accept/Decline actions call back into Identity.ProfileService to flip membership status.

This keeps Identity.Core free of Inbox dependencies while still giving you immediate in-app invites.
