# Identity.Core

Service implementations for the Identity + Profiles library.

## Services

### AuthService

Handles authentication flows:

```csharp
var authService = new AuthService(
    userStore,
    profileStore,
    membershipStore,
    sessionStore,
    new Pbkdf2PasswordHasher(),
    new UlidIdGenerator());

// Sign up creates user + default profile + membership
var result = await authService.SignUpAsync("myapp", new SignUpRequest
{
    Email = "user@example.com",
    Username = "johndoe",
    Password = "SecurePass123!",
    DisplayName = "John Doe"
});

// result.User    - the created user
// result.Profile - default profile (handle = username)
// result.Membership - owner membership in tenant

// Sign in with username or email
var session = await authService.SignInAsync("myapp", new SignInRequest
{
    Login = "johndoe",  // or "user@example.com"
    Password = "SecurePass123!"
});

// Validate token
var validSession = await authService.ValidateAccessTokenAsync(session.AccessToken);

// Sign out
await authService.SignOutAsync(session.SessionId);
```

### ProfileService

Manages profiles and memberships:

```csharp
var profileService = new ProfileService(
    profileStore,
    membershipStore,
    userStore,
    new UlidIdGenerator());

// Create additional profile
var profile = await profileService.CreateProfileAsync("myapp", userId, new CreateProfileRequest
{
    Handle = "mycompany",
    DisplayName = "My Company",
    IsPrivate = true
});

// Get user's profiles
var profiles = await profileService.GetProfilesForUserAsync("myapp", userId);

// Invite member (returns MembershipDto for inbox integration)
var invite = await profileService.InviteMemberAsync("myapp", profileId, new InviteMemberRequest
{
    Login = "newuser@example.com",
    Role = ProfileRole.Member
});

// Accept/decline invite
await profileService.AcceptInviteAsync("myapp", profileId, inviteeUserId);
// or
await profileService.DeclineInviteAsync("myapp", profileId, inviteeUserId);

// Direct add (no invite flow)
await profileService.AddMemberAsync("myapp", profileId, new AddMemberRequest
{
    UserId = memberId,
    Role = ProfileRole.Admin
});
```

### MembershipQueryService

Quick membership checks (useful for SignalR authorization):

```csharp
var query = new MembershipQueryService(membershipStore);

// Check if user can act as profile
bool canAct = await query.IsActiveMemberAsync("myapp", userId, profileId);

// Get all profiles user can act as
var profileIds = await query.GetActiveProfileIdsForUserAsync("myapp", userId);
```

## Auto-Profile on SignUp

When a user signs up, the system automatically creates:
1. A **User** record with hashed password
2. A **Profile** with `handle = username`
3. A **Membership** with `Role = Owner` and `Status = Active`

This ensures every user has at least one profile they can act as.

## Password Hashing

Uses PBKDF2-HMAC-SHA256 with:
- 16-byte random salt
- 100,000 iterations (configurable)
- 32-byte hash output
- Constant-time comparison

```csharp
var hasher = new Pbkdf2PasswordHasher(); // 100k iterations
// or
var hasher = new Pbkdf2PasswordHasher(iterations: 50000);
```

## Session Tokens

Sessions use 128-bit secure random tokens (hex-encoded):
- 24-hour expiration by default
- Token stored in session store for validation
- ProfileIds cached for quick access

## Multi-Tenant Model

- **Users** are global (same user across all tenants)
- **Profiles** are global (same handle cannot exist twice)
- **Memberships** are tenant-scoped (user X is member of profile Y in tenant Z)
- **Sessions** are tenant-scoped (signing in to a specific tenant)

Example: User signs up in "app1" → gets profile → signs into "app2" → session has no profiles (no membership in app2).
