using Identity.Abstractions;
using Identity.Core;
using Identity.Store.InMemory;

namespace Identity.Tests;

public class ProfileServiceTests
{
    private readonly AuthService _authService;
    private readonly ProfileService _profileService;
    private readonly InMemoryUserStore _userStore;
    private readonly InMemoryProfileStore _profileStore;
    private readonly InMemoryMembershipStore _membershipStore;
    private readonly InMemorySessionStore _sessionStore;
    private readonly UlidIdGenerator _idGenerator;

    public ProfileServiceTests()
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
    }

    private async Task<SignUpResult> CreateTestUserAsync(string suffix = "")
    {
        return await _authService.SignUpAsync("tenant1", new SignUpRequest
        {
            Email = $"test{suffix}@example.com",
            Username = $"testuser{suffix}",
            Password = "Password123!",
            DisplayName = "Test User"
        });
    }

    [Fact]
    public async Task CreateProfile_creates_additional_profile_and_owner_membership()
    {
        var user = await CreateTestUserAsync();

        var profile = await _profileService.CreateProfileAsync("tenant1", user.User.Id!, new CreateProfileRequest
        {
            Handle = "newprofile",
            DisplayName = "New Profile"
        });

        Assert.NotNull(profile.Id);
        Assert.Equal("newprofile", profile.Handle);

        var members = await _profileService.GetMembersAsync("tenant1", profile.Id!);
        Assert.Single(members);
        Assert.Equal(ProfileRole.Owner, members[0].Role);
    }

    [Fact]
    public async Task CreateProfile_duplicate_handle_throws()
    {
        var user = await CreateTestUserAsync();

        await _profileService.CreateProfileAsync("tenant1", user.User.Id!, new CreateProfileRequest
        {
            Handle = "uniquehandle"
        });

        var ex = await Assert.ThrowsAsync<IdentityValidationException>(() =>
            _profileService.CreateProfileAsync("tenant1", user.User.Id!, new CreateProfileRequest
            {
                Handle = "uniquehandle"
            }));

        Assert.Contains(ex.Errors, e => e.Code == "DUPLICATE" && e.Path == "Handle");
    }

    [Fact]
    public async Task AddMember_creates_active_membership()
    {
        var owner = await CreateTestUserAsync("owner");
        var member = await CreateTestUserAsync("member");

        var profile = await _profileService.CreateProfileAsync("tenant1", owner.User.Id!, new CreateProfileRequest
        {
            Handle = "teamprofile"
        });

        await _profileService.AddMemberAsync("tenant1", profile.Id!, new AddMemberRequest
        {
            UserId = member.User.Id!,
            Role = ProfileRole.Member
        });

        var members = await _profileService.GetMembersAsync("tenant1", profile.Id!);
        Assert.Equal(2, members.Count);

        var addedMember = members.First(m => m.UserId == member.User.Id);
        Assert.Equal(MembershipStatus.Active, addedMember.Status);
        Assert.Equal(ProfileRole.Member, addedMember.Role);
    }

    [Fact]
    public async Task InviteMember_creates_invited_membership_and_returns_dto()
    {
        var owner = await CreateTestUserAsync("owner");
        var invitee = await CreateTestUserAsync("invitee");

        var profile = await _profileService.CreateProfileAsync("tenant1", owner.User.Id!, new CreateProfileRequest
        {
            Handle = "privateprofile"
        });

        var membership = await _profileService.InviteMemberAsync("tenant1", profile.Id!, new InviteMemberRequest
        {
            Login = invitee.User.Username,
            Role = ProfileRole.Member
        });

        Assert.NotNull(membership);
        Assert.Equal(MembershipStatus.Invited, membership.Status);
        Assert.Equal(invitee.User.Id, membership.UserId);
    }

    [Fact]
    public async Task AcceptInvite_sets_status_active()
    {
        var owner = await CreateTestUserAsync("owner");
        var invitee = await CreateTestUserAsync("invitee");

        var profile = await _profileService.CreateProfileAsync("tenant1", owner.User.Id!, new CreateProfileRequest
        {
            Handle = "inviteprofile"
        });

        await _profileService.InviteMemberAsync("tenant1", profile.Id!, new InviteMemberRequest
        {
            Login = invitee.User.Username
        });

        await _profileService.AcceptInviteAsync("tenant1", profile.Id!, invitee.User.Id!);

        var members = await _profileService.GetMembersAsync("tenant1", profile.Id!);
        var inviteeMembership = members.First(m => m.UserId == invitee.User.Id);
        Assert.Equal(MembershipStatus.Active, inviteeMembership.Status);
    }

    [Fact]
    public async Task DeclineInvite_sets_status_disabled()
    {
        var owner = await CreateTestUserAsync("owner");
        var invitee = await CreateTestUserAsync("invitee");

        var profile = await _profileService.CreateProfileAsync("tenant1", owner.User.Id!, new CreateProfileRequest
        {
            Handle = "declineprofile"
        });

        await _profileService.InviteMemberAsync("tenant1", profile.Id!, new InviteMemberRequest
        {
            Login = invitee.User.Username
        });

        await _profileService.DeclineInviteAsync("tenant1", profile.Id!, invitee.User.Id!);

        var members = await _profileService.GetMembersAsync("tenant1", profile.Id!);
        var inviteeMembership = members.First(m => m.UserId == invitee.User.Id);
        Assert.Equal(MembershipStatus.Disabled, inviteeMembership.Status);
    }

    [Fact]
    public async Task GetProfilesForUser_returns_tenant_scoped_profiles()
    {
        var user = await CreateTestUserAsync();

        var profile2 = await _profileService.CreateProfileAsync("tenant1", user.User.Id!, new CreateProfileRequest
        {
            Handle = "secondprofile"
        });

        var profiles = await _profileService.GetProfilesForUserAsync("tenant1", user.User.Id!);

        // Should have the auto-created profile from signup + the new one
        Assert.Equal(2, profiles.Count);
    }
}
