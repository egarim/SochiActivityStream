using Identity.Abstractions;
using Identity.Core;
using Identity.Store.InMemory;

namespace Identity.Tests;

public class AuthServiceTests
{
    private readonly AuthService _authService;
    private readonly InMemoryUserStore _userStore;
    private readonly InMemoryProfileStore _profileStore;
    private readonly InMemoryMembershipStore _membershipStore;
    private readonly InMemorySessionStore _sessionStore;

    public AuthServiceTests()
    {
        _userStore = new InMemoryUserStore();
        _profileStore = new InMemoryProfileStore();
        _membershipStore = new InMemoryMembershipStore();
        _sessionStore = new InMemorySessionStore();

        _authService = new AuthService(
            _userStore,
            _profileStore,
            _membershipStore,
            _sessionStore,
            new Pbkdf2PasswordHasher(1000), // Lower iterations for faster tests
            new UlidIdGenerator());
    }

    private static SignUpRequest CreateValidSignUpRequest(string suffix = "") => new()
    {
        Email = $"test{suffix}@example.com",
        Username = $"testuser{suffix}",
        Password = "Password123!",
        DisplayName = "Test User"
    };

    [Fact]
    public async Task SignUp_creates_user_profile_and_membership()
    {
        var request = CreateValidSignUpRequest();

        var result = await _authService.SignUpAsync("tenant1", request);

        Assert.NotNull(result.User);
        Assert.NotNull(result.Profile);
        Assert.NotNull(result.Membership);
        Assert.NotNull(result.User.Id);
        Assert.NotNull(result.Profile.Id);
        Assert.NotNull(result.Membership.Id);
    }

    [Fact]
    public async Task SignUp_profile_handle_equals_username()
    {
        var request = CreateValidSignUpRequest();

        var result = await _authService.SignUpAsync("tenant1", request);

        Assert.Equal(request.Username.ToLowerInvariant(), result.Profile.Handle);
    }

    [Fact]
    public async Task SignUp_membership_role_is_owner()
    {
        var request = CreateValidSignUpRequest();

        var result = await _authService.SignUpAsync("tenant1", request);

        Assert.Equal(ProfileRole.Owner, result.Membership.Role);
        Assert.Equal(MembershipStatus.Active, result.Membership.Status);
    }

    [Fact]
    public async Task SignIn_with_username_returns_session()
    {
        var request = CreateValidSignUpRequest();
        await _authService.SignUpAsync("tenant1", request);

        var session = await _authService.SignInAsync("tenant1", new SignInRequest
        {
            Login = request.Username,
            Password = request.Password
        });

        Assert.NotNull(session);
        Assert.NotNull(session.SessionId);
        Assert.NotNull(session.AccessToken);
    }

    [Fact]
    public async Task SignIn_with_email_returns_session()
    {
        var request = CreateValidSignUpRequest();
        await _authService.SignUpAsync("tenant1", request);

        var session = await _authService.SignInAsync("tenant1", new SignInRequest
        {
            Login = request.Email,
            Password = request.Password
        });

        Assert.NotNull(session);
        Assert.NotNull(session.SessionId);
    }

    [Fact]
    public async Task SignIn_session_contains_default_profile()
    {
        var request = CreateValidSignUpRequest();
        var signupResult = await _authService.SignUpAsync("tenant1", request);

        var session = await _authService.SignInAsync("tenant1", new SignInRequest
        {
            Login = request.Username,
            Password = request.Password
        });

        Assert.Contains(signupResult.Profile.Id!, session.ProfileIds);
    }

    [Fact]
    public async Task SignIn_wrong_password_throws()
    {
        var request = CreateValidSignUpRequest();
        await _authService.SignUpAsync("tenant1", request);

        var ex = await Assert.ThrowsAsync<IdentityValidationException>(() =>
            _authService.SignInAsync("tenant1", new SignInRequest
            {
                Login = request.Username,
                Password = "WrongPassword123!"
            }));

        Assert.Contains(ex.Errors, e => e.Code == "INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task SignUp_duplicate_email_throws()
    {
        var request1 = CreateValidSignUpRequest("1");
        await _authService.SignUpAsync("tenant1", request1);

        var request2 = new SignUpRequest
        {
            Email = request1.Email, // Same email
            Username = "different_user",
            Password = "Password123!"
        };

        var ex = await Assert.ThrowsAsync<IdentityValidationException>(() =>
            _authService.SignUpAsync("tenant1", request2));

        Assert.Contains(ex.Errors, e => e.Code == "DUPLICATE" && e.Path == "Email");
    }

    [Fact]
    public async Task SignUp_duplicate_username_throws()
    {
        var request1 = CreateValidSignUpRequest("1");
        await _authService.SignUpAsync("tenant1", request1);

        var request2 = new SignUpRequest
        {
            Email = "different@example.com",
            Username = request1.Username, // Same username
            Password = "Password123!"
        };

        var ex = await Assert.ThrowsAsync<IdentityValidationException>(() =>
            _authService.SignUpAsync("tenant1", request2));

        Assert.Contains(ex.Errors, e => e.Code == "DUPLICATE" && e.Path == "Username");
    }

    [Fact]
    public async Task ValidateAccessToken_returns_session()
    {
        var request = CreateValidSignUpRequest();
        await _authService.SignUpAsync("tenant1", request);

        var session = await _authService.SignInAsync("tenant1", new SignInRequest
        {
            Login = request.Username,
            Password = request.Password
        });

        var validatedSession = await _authService.ValidateAccessTokenAsync(session.AccessToken);

        Assert.NotNull(validatedSession);
        Assert.Equal(session.SessionId, validatedSession.SessionId);
    }

    [Fact]
    public async Task ValidateAccessToken_invalid_token_returns_null()
    {
        var result = await _authService.ValidateAccessTokenAsync("invalid_token");

        Assert.Null(result);
    }

    [Fact]
    public async Task SignOut_revokes_session()
    {
        var request = CreateValidSignUpRequest();
        await _authService.SignUpAsync("tenant1", request);

        var session = await _authService.SignInAsync("tenant1", new SignInRequest
        {
            Login = request.Username,
            Password = request.Password
        });

        await _authService.SignOutAsync(session.SessionId);

        var validatedSession = await _authService.ValidateAccessTokenAsync(session.AccessToken);
        Assert.Null(validatedSession);
    }
}
