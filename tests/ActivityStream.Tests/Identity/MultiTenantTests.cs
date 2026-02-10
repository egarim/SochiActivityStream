using Identity.Abstractions;
using Identity.Core;
using Identity.Store.InMemory;

namespace ActivityStream.Tests.Identity;

public class MultiTenantTests
{
    private readonly AuthService _authService;
    private readonly ProfileService _profileService;
    private readonly MembershipQueryService _membershipQuery;
    private readonly InMemoryUserStore _userStore;
    private readonly InMemoryProfileStore _profileStore;
    private readonly InMemoryMembershipStore _membershipStore;
    private readonly InMemorySessionStore _sessionStore;
    private readonly UlidIdGenerator _idGenerator;

    public MultiTenantTests()
    {
        _userStore = new InMemoryUserStore();
        _profileStore = new InMemoryProfileStore();
        _membershipStore = new InMemoryMembershipStore();
        _sessionStore = new InMemorySessionStore();
        _idGenerator = new UlidIdGenerator();

        var passwordHasher = new Pbkdf2PasswordHasher(1000);

        _authService = new AuthService(
            _userStore,
            _profileStore,
            _membershipStore,
            _sessionStore,
            passwordHasher,
            _idGenerator);

        _profileService = new ProfileService(
            _profileStore,
            _membershipStore,
            _userStore,
            _idGenerator);

        _membershipQuery = new MembershipQueryService(_membershipStore);
    }

    [Fact]
    public async Task Membership_in_tenant_A_invisible_in_tenant_B()
    {
        // Create user in tenant A
        var result = await _authService.SignUpAsync("tenantA", new SignUpRequest
        {
            Email = "user@example.com",
            Username = "testuser",
            Password = "Password123!"
        });

        // Query profiles in tenant A - should see the profile
        var profilesA = await _profileService.GetProfilesForUserAsync("tenantA", result.User.Id!);
        Assert.Single(profilesA);

        // Query profiles in tenant B - should NOT see the profile
        var profilesB = await _profileService.GetProfilesForUserAsync("tenantB", result.User.Id!);
        Assert.Empty(profilesB);
    }

    [Fact]
    public async Task Same_profile_different_memberships_per_tenant()
    {
        // Create owner in tenant A
        var owner = await _authService.SignUpAsync("tenantA", new SignUpRequest
        {
            Email = "owner@example.com",
            Username = "owner",
            Password = "Password123!"
        });

        // Create user for tenant B (different signup, same profile will be shared)
        var userB = await _authService.SignUpAsync("tenantB", new SignUpRequest
        {
            Email = "userb@example.com",
            Username = "userb",
            Password = "Password123!"
        });

        // Create a new profile owned by owner
        var sharedProfile = await _profileService.CreateProfileAsync("tenantA", owner.User.Id!, new CreateProfileRequest
        {
            Handle = "sharedprofile"
        });

        // Add userB to the shared profile in tenant B
        await _profileService.AddMemberAsync("tenantB", sharedProfile.Id!, new AddMemberRequest
        {
            UserId = userB.User.Id!,
            Role = ProfileRole.Viewer
        });

        // Owner is active member in tenant A
        var isActiveTenantA = await _membershipQuery.IsActiveMemberAsync("tenantA", owner.User.Id!, sharedProfile.Id!);
        Assert.True(isActiveTenantA);

        // UserB is active member in tenant B
        var isActiveTenantB = await _membershipQuery.IsActiveMemberAsync("tenantB", userB.User.Id!, sharedProfile.Id!);
        Assert.True(isActiveTenantB);

        // Owner is NOT active member in tenant B (membership was created in tenant A)
        var ownerInTenantB = await _membershipQuery.IsActiveMemberAsync("tenantB", owner.User.Id!, sharedProfile.Id!);
        Assert.False(ownerInTenantB);
    }

    [Fact]
    public async Task Session_profileIds_are_tenant_scoped()
    {
        // Create user in tenant A
        var owner = await _authService.SignUpAsync("tenantA", new SignUpRequest
        {
            Email = "session@example.com",
            Username = "sessionuser",
            Password = "Password123!"
        });

        // Sign in to tenant A - should see profiles
        var sessionA = await _authService.SignInAsync("tenantA", new SignInRequest
        {
            Login = "sessionuser",
            Password = "Password123!"
        });

        Assert.Single(sessionA.ProfileIds);

        // Sign in to tenant B - should NOT see profiles (no membership in B)
        var sessionB = await _authService.SignInAsync("tenantB", new SignInRequest
        {
            Login = "sessionuser",
            Password = "Password123!"
        });

        Assert.Empty(sessionB.ProfileIds);
    }
}
