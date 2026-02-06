# Identity.Abstractions

DTOs, interfaces, and contracts for the Identity + Profiles library.

## Core Concepts

### User (Identity Principal)
- Global identity with email, username, password hash
- Can be a member of multiple profiles per tenant

### Profile (Actor/Persona)
- Global entity with unique handle
- Main "actor" in Activity Stream and Relationship Service
- Can be shared by multiple users via memberships

### Membership (Tenant-Scoped)
- Links users to profiles within a specific tenant
- Roles: Owner, Admin, Member, Viewer
- Status: Active, Invited, Disabled

### Session
- Tenant-scoped authentication session
- Contains access token and cached profile IDs

## DTOs

| DTO | Description |
|-----|-------------|
| `UserDto` | User identity (id, email, username, displayName) |
| `ProfileDto` | Profile/persona (id, handle, displayName, isPrivate) |
| `MembershipDto` | User-profile link (tenantId, userId, profileId, role, status) |
| `SessionDto` | Active session (sessionId, accessToken, profileIds) |
| `SignUpRequest` | Sign-up input (email, username, password) |
| `SignInRequest` | Sign-in input (login, password) |
| `SignUpResult` | Sign-up output (user, profile, membership) |
| `CreateProfileRequest` | Create profile input (handle, displayName) |
| `AddMemberRequest` | Direct member add (userId, role) |
| `InviteMemberRequest` | Invite member (login, role) |

## EntityRef Conventions

Use these when referencing identity entities in other services:

```csharp
// Profile EntityRef (most common - used in feeds, relationships)
new EntityRefDto { Kind = "identity", Type = "Profile", Id = profileId }

// User EntityRef (internal use)
new EntityRefDto { Kind = "identity", Type = "User", Id = userId }
```

## Interfaces

### Services
- `IAuthService` - Sign up, sign in, sign out, validate tokens
- `IProfileService` - Create profiles, manage members, invites
- `IMembershipQuery` - Check membership status (for SignalR auth)
- `IPasswordHasher` - Password hashing and verification

### Stores
- `IUserStore` - User persistence
- `IProfileStore` - Profile persistence
- `IMembershipStore` - Membership persistence
- `ISessionStore` - Session persistence

## Password Security

The `IPasswordHasher` interface and `PasswordHash` record define secure password storage:

```csharp
public interface IPasswordHasher
{
    PasswordHash Hash(string password);
    bool Verify(string password, PasswordHash hash);
}

public sealed record PasswordHash(
    byte[] Salt,
    int Iterations,
    byte[] HashBytes,
    string Algorithm);
```

## Validation

Validation rules are enforced by the Core services:

| Field | Rules |
|-------|-------|
| Email | Required, max 254 chars, valid format |
| Username | 3-50 chars, alphanumeric + underscore |
| Handle | 3-50 chars, alphanumeric + underscore |
| Password | 8-256 chars |
| DisplayName | Max 100 chars |
| TenantId | Required, max 100 chars |
