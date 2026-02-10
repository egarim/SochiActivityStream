namespace ActivityStream.Abstractions.Identity;

/// <summary>
/// Service for managing profiles and memberships.
/// </summary>
public interface IProfileService
{
    Task<ProfileDto> CreateProfileAsync(string tenantId, string userId, CreateProfileRequest request, CancellationToken ct = default);
    Task<ProfileDto?> GetProfileByIdAsync(string profileId, CancellationToken ct = default);
    Task<ProfileDto> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<ProfileDto>> GetProfilesForUserAsync(string tenantId, string userId, CancellationToken ct = default);
    Task<IReadOnlyList<MembershipDto>> GetMembersAsync(string tenantId, string profileId, CancellationToken ct = default);
    Task AddMemberAsync(string tenantId, string profileId, AddMemberRequest request, CancellationToken ct = default);
    Task<MembershipDto> InviteMemberAsync(string tenantId, string profileId, InviteMemberRequest request, CancellationToken ct = default);
    Task AcceptInviteAsync(string tenantId, string profileId, string userId, CancellationToken ct = default);
    Task DeclineInviteAsync(string tenantId, string profileId, string userId, CancellationToken ct = default);
    Task SetMemberStatusAsync(string tenantId, string profileId, string userId, MembershipStatus status, CancellationToken ct = default);
    Task SetMemberRoleAsync(string tenantId, string profileId, string userId, ProfileRole role, CancellationToken ct = default);
}
