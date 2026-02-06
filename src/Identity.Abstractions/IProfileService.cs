namespace Identity.Abstractions;

/// <summary>
/// Service for managing profiles and memberships.
/// </summary>
public interface IProfileService
{
    /// <summary>
    /// Creates a new profile and assigns the user as Owner.
    /// </summary>
    /// <param name="tenantId">The tenant for the membership.</param>
    /// <param name="userId">The user creating the profile (becomes Owner).</param>
    /// <param name="request">Profile creation details.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created profile.</returns>
    /// <exception cref="IdentityValidationException">When validation fails or handle is taken.</exception>
    Task<ProfileDto> CreateProfileAsync(string tenantId, string userId, CreateProfileRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gets a profile by its ID.
    /// </summary>
    /// <param name="profileId">The profile ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The profile if found, null otherwise.</returns>
    Task<ProfileDto?> GetProfileByIdAsync(string profileId, CancellationToken ct = default);

    /// <summary>
    /// Gets all profiles a user is a member of in a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant to query.</param>
    /// <param name="userId">The user to query profiles for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of profiles the user belongs to.</returns>
    Task<IReadOnlyList<ProfileDto>> GetProfilesForUserAsync(string tenantId, string userId, CancellationToken ct = default);

    /// <summary>
    /// Gets all members of a profile in a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant to query.</param>
    /// <param name="profileId">The profile to get members for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of memberships.</returns>
    Task<IReadOnlyList<MembershipDto>> GetMembersAsync(string tenantId, string profileId, CancellationToken ct = default);

    /// <summary>
    /// Adds a user as a member with Active status (no invite flow).
    /// </summary>
    /// <param name="tenantId">The tenant for the membership.</param>
    /// <param name="profileId">The profile to add the member to.</param>
    /// <param name="request">Add member details.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="IdentityValidationException">When user not found.</exception>
    Task AddMemberAsync(string tenantId, string profileId, AddMemberRequest request, CancellationToken ct = default);

    /// <summary>
    /// Invites a user to join a profile (creates Invited membership).
    /// The returned MembershipDto can be used to create an inbox notification.
    /// </summary>
    /// <param name="tenantId">The tenant for the membership.</param>
    /// <param name="profileId">The profile to invite to.</param>
    /// <param name="request">Invite details (login = username or email).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created membership (Status=Invited).</returns>
    /// <exception cref="IdentityValidationException">When user not found.</exception>
    Task<MembershipDto> InviteMemberAsync(string tenantId, string profileId, InviteMemberRequest request, CancellationToken ct = default);

    /// <summary>
    /// Accepts a pending invite, changing status to Active.
    /// </summary>
    /// <param name="tenantId">The tenant of the membership.</param>
    /// <param name="profileId">The profile invited to.</param>
    /// <param name="userId">The user accepting the invite.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="IdentityValidationException">When no pending invite exists.</exception>
    Task AcceptInviteAsync(string tenantId, string profileId, string userId, CancellationToken ct = default);

    /// <summary>
    /// Declines a pending invite, setting status to Disabled.
    /// </summary>
    /// <param name="tenantId">The tenant of the membership.</param>
    /// <param name="profileId">The profile invited to.</param>
    /// <param name="userId">The user declining the invite.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="IdentityValidationException">When no pending invite exists.</exception>
    Task DeclineInviteAsync(string tenantId, string profileId, string userId, CancellationToken ct = default);

    /// <summary>
    /// Updates the status of a membership.
    /// </summary>
    /// <param name="tenantId">The tenant of the membership.</param>
    /// <param name="profileId">The profile.</param>
    /// <param name="userId">The user.</param>
    /// <param name="status">The new status.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SetMemberStatusAsync(string tenantId, string profileId, string userId, MembershipStatus status, CancellationToken ct = default);

    /// <summary>
    /// Updates the role of a membership.
    /// </summary>
    /// <param name="tenantId">The tenant of the membership.</param>
    /// <param name="profileId">The profile.</param>
    /// <param name="userId">The user.</param>
    /// <param name="role">The new role.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SetMemberRoleAsync(string tenantId, string profileId, string userId, ProfileRole role, CancellationToken ct = default);
}
