using System.Security.Cryptography;
using Identity.Abstractions;

namespace Identity.Core;

/// <summary>
/// Authentication service implementation.
/// </summary>
public sealed class AuthService : IAuthService
{
    private const int SessionExpirationHours = 24;

    private readonly IUserStore _userStore;
    private readonly IProfileStore _profileStore;
    private readonly IMembershipStore _membershipStore;
    private readonly ISessionStore _sessionStore;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IIdGenerator _idGenerator;

    public AuthService(
        IUserStore userStore,
        IProfileStore profileStore,
        IMembershipStore membershipStore,
        ISessionStore sessionStore,
        IPasswordHasher passwordHasher,
        IIdGenerator idGenerator)
    {
        _userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));
        _profileStore = profileStore ?? throw new ArgumentNullException(nameof(profileStore));
        _membershipStore = membershipStore ?? throw new ArgumentNullException(nameof(membershipStore));
        _sessionStore = sessionStore ?? throw new ArgumentNullException(nameof(sessionStore));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
    }

    /// <inheritdoc />
    public async Task<SignUpResult> SignUpAsync(string tenantId, SignUpRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Normalize
        var normalizedTenantId = IdentityNormalizer.NormalizeTenantId(tenantId);
        var normalizedEmail = IdentityNormalizer.NormalizeEmail(request.Email);
        var normalizedUsername = IdentityNormalizer.NormalizeUsername(request.Username);
        var normalizedHandle = normalizedUsername; // handle = username

        // Validate
        var errors = IdentityValidator.ValidateSignUp(request, tenantId);
        if (errors.Count > 0)
            throw new IdentityValidationException(errors);

        // Check email uniqueness
        var existingByEmail = await _userStore.FindByEmailAsync(normalizedEmail, ct);
        if (existingByEmail is not null)
            throw new IdentityValidationException("DUPLICATE", "Email is already registered.", "Email");

        // Check username uniqueness
        var existingByUsername = await _userStore.FindByUsernameAsync(normalizedUsername, ct);
        if (existingByUsername is not null)
            throw new IdentityValidationException("DUPLICATE", "Username is already taken.", "Username");

        // Check handle uniqueness (same as username, but checking profile store)
        var existingByHandle = await _profileStore.FindByHandleAsync(normalizedHandle, ct);
        if (existingByHandle is not null)
            throw new IdentityValidationException("DUPLICATE", "Handle is already taken.", "Handle");

        // Hash password
        var passwordHash = _passwordHasher.Hash(request.Password);

        // Create user
        var userId = _idGenerator.NewId();
        var user = new UserDto
        {
            Id = userId,
            Email = normalizedEmail,
            Username = normalizedUsername,
            DisplayName = request.DisplayName?.Trim(),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var userRecord = new UserRecord
        {
            User = user,
            Hash = passwordHash
        };

        await _userStore.CreateAsync(userRecord, ct);

        // Create profile (handle = username)
        var profileId = _idGenerator.NewId();
        var profile = new ProfileDto
        {
            Id = profileId,
            Handle = normalizedHandle,
            DisplayName = request.DisplayName?.Trim() ?? normalizedUsername,
            IsPrivate = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var profileRecord = new ProfileRecord
        {
            Profile = profile
        };

        await _profileStore.CreateAsync(profileRecord, ct);

        // Create membership (owner)
        var membershipId = _idGenerator.NewId();
        var membership = new MembershipDto
        {
            Id = membershipId,
            TenantId = normalizedTenantId,
            UserId = userId,
            ProfileId = profileId,
            Role = ProfileRole.Owner,
            Status = MembershipStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var membershipRecord = new MembershipRecord
        {
            Membership = membership
        };

        await _membershipStore.UpsertAsync(membershipRecord, ct);

        return new SignUpResult
        {
            User = user,
            Profile = profile,
            Membership = membership
        };
    }

    /// <inheritdoc />
    public async Task<SessionDto> SignInAsync(string tenantId, SignInRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Normalize
        var normalizedTenantId = IdentityNormalizer.NormalizeTenantId(tenantId);
        var normalizedLogin = request.Login?.Trim().ToLowerInvariant() ?? string.Empty;

        // Validate
        var errors = IdentityValidator.ValidateSignIn(request, tenantId);
        if (errors.Count > 0)
            throw new IdentityValidationException(errors);

        // Find user by email or username
        UserRecord? userRecord = null;

        if (normalizedLogin.Contains('@'))
        {
            userRecord = await _userStore.FindByEmailAsync(normalizedLogin, ct);
        }
        else
        {
            userRecord = await _userStore.FindByUsernameAsync(normalizedLogin, ct);
        }

        // Also try the other lookup if first failed
        if (userRecord is null)
        {
            userRecord = await _userStore.FindByEmailAsync(normalizedLogin, ct)
                         ?? await _userStore.FindByUsernameAsync(normalizedLogin, ct);
        }

        if (userRecord is null)
            throw new IdentityValidationException("INVALID_CREDENTIALS", "Invalid login or password.");

        if (!userRecord.User.IsActive)
            throw new IdentityValidationException("ACCOUNT_DISABLED", "Account is disabled.");

        // Verify password
        if (!_passwordHasher.Verify(request.Password, userRecord.Hash))
            throw new IdentityValidationException("INVALID_CREDENTIALS", "Invalid login or password.");

        // Get active profile IDs for this tenant
        var memberships = await _membershipStore.GetForUserAsync(normalizedTenantId, userRecord.User.Id!, ct);
        var activeProfileIds = memberships
            .Where(m => m.Membership.Status == MembershipStatus.Active)
            .Select(m => m.Membership.ProfileId)
            .ToList();

        // Generate session
        var sessionId = _idGenerator.NewId();
        var accessToken = GenerateAccessToken();

        var session = new SessionDto
        {
            SessionId = sessionId,
            UserId = userRecord.User.Id!,
            TenantId = normalizedTenantId,
            AccessToken = accessToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(SessionExpirationHours),
            ProfileIds = activeProfileIds
        };

        var sessionRecord = new SessionRecord
        {
            Session = session
        };

        await _sessionStore.CreateAsync(sessionRecord, ct);

        return session;
    }

    /// <inheritdoc />
    public async Task SignOutAsync(string sessionId, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            await _sessionStore.RevokeAsync(sessionId, ct);
        }
    }

    /// <inheritdoc />
    public async Task<SessionDto?> ValidateAccessTokenAsync(string accessToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            return null;

        var record = await _sessionStore.FindByAccessTokenAsync(accessToken, ct);
        if (record is null)
            return null;

        // Check expiration
        if (record.Session.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            // Optionally revoke expired session
            await _sessionStore.RevokeAsync(record.Session.SessionId, ct);
            return null;
        }

        return record.Session;
    }

    /// <summary>
    /// Generates a 128-bit secure random access token as hex string.
    /// </summary>
    private static string GenerateAccessToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(16); // 128 bits
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
