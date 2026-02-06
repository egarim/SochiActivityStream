using Identity.Abstractions;
using Identity.Core;
using Identity.Store.InMemory;

namespace Identity.Tests;

public class ValidationTests
{
    private readonly AuthService _authService;

    public ValidationTests()
    {
        var userStore = new InMemoryUserStore();
        var profileStore = new InMemoryProfileStore();
        var membershipStore = new InMemoryMembershipStore();
        var sessionStore = new InMemorySessionStore();

        _authService = new AuthService(
            userStore,
            profileStore,
            membershipStore,
            sessionStore,
            new Pbkdf2PasswordHasher(1000),
            new UlidIdGenerator());
    }

    [Fact]
    public async Task SignUp_short_password_fails()
    {
        var request = new SignUpRequest
        {
            Email = "test@example.com",
            Username = "testuser",
            Password = "short" // Less than 8 chars
        };

        var ex = await Assert.ThrowsAsync<IdentityValidationException>(() =>
            _authService.SignUpAsync("tenant1", request));

        Assert.Contains(ex.Errors, e => e.Path == "Password" && e.Code == "MIN_LENGTH");
    }

    [Fact]
    public async Task SignUp_short_username_fails()
    {
        var request = new SignUpRequest
        {
            Email = "test@example.com",
            Username = "ab", // Less than 3 chars
            Password = "Password123!"
        };

        var ex = await Assert.ThrowsAsync<IdentityValidationException>(() =>
            _authService.SignUpAsync("tenant1", request));

        Assert.Contains(ex.Errors, e => e.Path == "Username" && e.Code == "MIN_LENGTH");
    }

    [Fact]
    public async Task SignUp_invalid_email_fails()
    {
        var request = new SignUpRequest
        {
            Email = "not-an-email",
            Username = "testuser",
            Password = "Password123!"
        };

        var ex = await Assert.ThrowsAsync<IdentityValidationException>(() =>
            _authService.SignUpAsync("tenant1", request));

        Assert.Contains(ex.Errors, e => e.Path == "Email" && e.Code == "INVALID_FORMAT");
    }

    [Fact]
    public async Task SignUp_invalid_username_chars_fails()
    {
        var request = new SignUpRequest
        {
            Email = "test@example.com",
            Username = "user@name!", // Invalid chars
            Password = "Password123!"
        };

        var ex = await Assert.ThrowsAsync<IdentityValidationException>(() =>
            _authService.SignUpAsync("tenant1", request));

        Assert.Contains(ex.Errors, e => e.Path == "Username" && e.Code == "INVALID_FORMAT");
    }

    [Fact]
    public async Task SignUp_missing_tenantId_fails()
    {
        var request = new SignUpRequest
        {
            Email = "test@example.com",
            Username = "testuser",
            Password = "Password123!"
        };

        var ex = await Assert.ThrowsAsync<IdentityValidationException>(() =>
            _authService.SignUpAsync("", request));

        Assert.Contains(ex.Errors, e => e.Path == "TenantId" && e.Code == "REQUIRED");
    }

    [Fact]
    public async Task SignIn_missing_login_fails()
    {
        var ex = await Assert.ThrowsAsync<IdentityValidationException>(() =>
            _authService.SignInAsync("tenant1", new SignInRequest
            {
                Login = "",
                Password = "Password123!"
            }));

        Assert.Contains(ex.Errors, e => e.Path == "Login" && e.Code == "REQUIRED");
    }

    [Fact]
    public async Task SignIn_missing_password_fails()
    {
        var ex = await Assert.ThrowsAsync<IdentityValidationException>(() =>
            _authService.SignInAsync("tenant1", new SignInRequest
            {
                Login = "testuser",
                Password = ""
            }));

        Assert.Contains(ex.Errors, e => e.Path == "Password" && e.Code == "REQUIRED");
    }

    [Fact]
    public void Handle_validation_checks()
    {
        // Valid handle
        var errors1 = IdentityValidator.ValidateCreateProfile(
            new CreateProfileRequest { Handle = "valid_handle123" },
            "tenant1",
            "user1");
        Assert.Empty(errors1);

        // Short handle
        var errors2 = IdentityValidator.ValidateCreateProfile(
            new CreateProfileRequest { Handle = "ab" },
            "tenant1",
            "user1");
        Assert.Contains(errors2, e => e.Path == "Handle" && e.Code == "MIN_LENGTH");

        // Invalid chars
        var errors3 = IdentityValidator.ValidateCreateProfile(
            new CreateProfileRequest { Handle = "my-handle!" },
            "tenant1",
            "user1");
        Assert.Contains(errors3, e => e.Path == "Handle" && e.Code == "INVALID_FORMAT");
    }
}
