using Identity.Abstractions;

namespace Identity.Core;

/// <summary>
/// Profile and membership management service.
/// </summary>
public sealed class ProfileService : IProfileService
{
    private readonly IProfileStore _profileStore;
    private readonly IMembershipStore _membershipStore;
    private readonly IUserStore _userStore;
    private readonly IIdGenerator _idGenerator;

    public ProfileService(
        IProfileStore profileStore,
        IMembershipStore membershipStore,
        IUserStore userStore,
        IIdGenerator idGenerator)
    {
        _profileStore = profileStore ?? throw new ArgumentNullException(nameof(profileStore));
        _membershipStore = membershipStore ?? throw new ArgumentNullException(nameof(membershipStore));
        _userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));
        _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
    }

    /// <inheritdoc />
    public async Task<ProfileDto> CreateProfileAsync(string tenantId, string userId, CreateProfileRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Normalize
        var normalizedTenantId = IdentityNormalizer.NormalizeTenantId(tenantId);
        var normalizedHandle = IdentityNormalizer.NormalizeHandle(request.Handle);

        // Validate
        var errors = IdentityValidator.ValidateCreateProfile(request, tenantId, userId);
        if (errors.Count > 0)
            throw new IdentityValidationException(errors);

        // Check user exists
        var user = await _userStore.GetByIdAsync(userId, ct);
        if (user is null)
            throw new IdentityValidationException("NOT_FOUND", "User not found.", "UserId");

        // Check handle uniqueness
        var existingByHandle = await _profileStore.FindByHandleAsync(normalizedHandle, ct);
        if (existingByHandle is not null)
            throw new IdentityValidationException("DUPLICATE", "Handle is already taken.", "Handle");

        // Create profile
        var profileId = _idGenerator.NewId();
        var profile = new ProfileDto
        {
            Id = profileId,
            Handle = normalizedHandle,
            DisplayName = request.DisplayName?.Trim(),
            IsPrivate = request.IsPrivate,
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

        return profile;
    }

    /// <inheritdoc />
    public async Task<ProfileDto?> GetProfileByIdAsync(string profileId, CancellationToken ct = default)
    {
        var profileRecord = await _profileStore.GetByIdAsync(profileId, ct);
        return profileRecord?.Profile;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProfileDto>> GetProfilesForUserAsync(string tenantId, string userId, CancellationToken ct = default)
    {
        var normalizedTenantId = IdentityNormalizer.NormalizeTenantId(tenantId);

        var memberships = await _membershipStore.GetForUserAsync(normalizedTenantId, userId, ct);
        var profiles = new List<ProfileDto>();

        foreach (var membership in memberships)
        {
            if (membership.Membership.Status != MembershipStatus.Active)
                continue;

            var profileRecord = await _profileStore.GetByIdAsync(membership.Membership.ProfileId, ct);
            if (profileRecord is not null)
            {
                profiles.Add(profileRecord.Profile);
            }
        }

        return profiles;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MembershipDto>> GetMembersAsync(string tenantId, string profileId, CancellationToken ct = default)
    {
        var normalizedTenantId = IdentityNormalizer.NormalizeTenantId(tenantId);

        var memberships = await _membershipStore.GetForProfileAsync(normalizedTenantId, profileId, ct);
        return memberships.Select(m => m.Membership).ToList();
    }

    /// <inheritdoc />
    public async Task AddMemberAsync(string tenantId, string profileId, AddMemberRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Normalize
        var normalizedTenantId = IdentityNormalizer.NormalizeTenantId(tenantId);

        // Validate
        var errors = IdentityValidator.ValidateAddMember(request, tenantId, profileId);
        if (errors.Count > 0)
            throw new IdentityValidationException(errors);

        // Check user exists
        var user = await _userStore.GetByIdAsync(request.UserId, ct);
        if (user is null)
            throw new IdentityValidationException("NOT_FOUND", "User not found.", "UserId");

        // Check profile exists
        var profile = await _profileStore.GetByIdAsync(profileId, ct);
        if (profile is null)
            throw new IdentityValidationException("NOT_FOUND", "Profile not found.", "ProfileId");

        // Create or update membership
        var membershipId = _idGenerator.NewId();
        var membership = new MembershipDto
        {
            Id = membershipId,
            TenantId = normalizedTenantId,
            UserId = request.UserId,
            ProfileId = profileId,
            Role = request.Role,
            Status = MembershipStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var membershipRecord = new MembershipRecord
        {
            Membership = membership
        };

        await _membershipStore.UpsertAsync(membershipRecord, ct);
    }

    /// <inheritdoc />
    public async Task<MembershipDto> InviteMemberAsync(string tenantId, string profileId, InviteMemberRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Normalize
        var normalizedTenantId = IdentityNormalizer.NormalizeTenantId(tenantId);
        var normalizedLogin = request.Login?.Trim().ToLowerInvariant() ?? string.Empty;

        // Validate
        var errors = IdentityValidator.ValidateInviteMember(request, tenantId, profileId);
        if (errors.Count > 0)
            throw new IdentityValidationException(errors);

        // Check profile exists
        var profile = await _profileStore.GetByIdAsync(profileId, ct);
        if (profile is null)
            throw new IdentityValidationException("NOT_FOUND", "Profile not found.", "ProfileId");

        // Find user by email or username
        UserRecord? userRecord = normalizedLogin.Contains('@')
            ? await _userStore.FindByEmailAsync(normalizedLogin, ct)
            : await _userStore.FindByUsernameAsync(normalizedLogin, ct);

        if (userRecord is null)
        {
            // Try the other lookup
            userRecord = await _userStore.FindByEmailAsync(normalizedLogin, ct)
                         ?? await _userStore.FindByUsernameAsync(normalizedLogin, ct);
        }

        if (userRecord is null)
            throw new IdentityValidationException("NOT_FOUND", "User not found.", "Login");

        // Create invited membership
        var membershipId = _idGenerator.NewId();
        var membership = new MembershipDto
        {
            Id = membershipId,
            TenantId = normalizedTenantId,
            UserId = userRecord.User.Id!,
            ProfileId = profileId,
            Role = request.Role,
            Status = MembershipStatus.Invited,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var membershipRecord = new MembershipRecord
        {
            Membership = membership
        };

        await _membershipStore.UpsertAsync(membershipRecord, ct);

        return membership;
    }

    /// <inheritdoc />
    public async Task AcceptInviteAsync(string tenantId, string profileId, string userId, CancellationToken ct = default)
    {
        var normalizedTenantId = IdentityNormalizer.NormalizeTenantId(tenantId);

        var membership = await _membershipStore.FindAsync(normalizedTenantId, userId, profileId, ct);
        if (membership is null)
            throw new IdentityValidationException("NOT_FOUND", "No pending invite found.");

        if (membership.Membership.Status != MembershipStatus.Invited)
            throw new IdentityValidationException("INVALID_STATE", "Invite is not pending.");

        membership.Membership.Status = MembershipStatus.Active;
        await _membershipStore.UpsertAsync(membership, ct);
    }

    /// <inheritdoc />
    public async Task DeclineInviteAsync(string tenantId, string profileId, string userId, CancellationToken ct = default)
    {
        var normalizedTenantId = IdentityNormalizer.NormalizeTenantId(tenantId);

        var membership = await _membershipStore.FindAsync(normalizedTenantId, userId, profileId, ct);
        if (membership is null)
            throw new IdentityValidationException("NOT_FOUND", "No pending invite found.");

        if (membership.Membership.Status != MembershipStatus.Invited)
            throw new IdentityValidationException("INVALID_STATE", "Invite is not pending.");

        // Set to Disabled for audit trail
        membership.Membership.Status = MembershipStatus.Disabled;
        await _membershipStore.UpsertAsync(membership, ct);
    }

    /// <inheritdoc />
    public async Task SetMemberStatusAsync(string tenantId, string profileId, string userId, MembershipStatus status, CancellationToken ct = default)
    {
        var normalizedTenantId = IdentityNormalizer.NormalizeTenantId(tenantId);

        var membership = await _membershipStore.FindAsync(normalizedTenantId, userId, profileId, ct);
        if (membership is null)
            throw new IdentityValidationException("NOT_FOUND", "Membership not found.");

        membership.Membership.Status = status;
        await _membershipStore.UpsertAsync(membership, ct);
    }

    /// <inheritdoc />
    public async Task SetMemberRoleAsync(string tenantId, string profileId, string userId, ProfileRole role, CancellationToken ct = default)
    {
        var normalizedTenantId = IdentityNormalizer.NormalizeTenantId(tenantId);

        var membership = await _membershipStore.FindAsync(normalizedTenantId, userId, profileId, ct);
        if (membership is null)
            throw new IdentityValidationException("NOT_FOUND", "Membership not found.");

        membership.Membership.Role = role;
        await _membershipStore.UpsertAsync(membership, ct);
    }
}
