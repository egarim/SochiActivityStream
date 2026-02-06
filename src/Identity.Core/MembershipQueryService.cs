using Identity.Abstractions;

namespace Identity.Core;

/// <summary>
/// Service for querying membership information.
/// </summary>
public sealed class MembershipQueryService : IMembershipQuery
{
    private readonly IMembershipStore _membershipStore;

    public MembershipQueryService(IMembershipStore membershipStore)
    {
        _membershipStore = membershipStore ?? throw new ArgumentNullException(nameof(membershipStore));
    }

    /// <inheritdoc />
    public async Task<bool> IsActiveMemberAsync(string tenantId, string userId, string profileId, CancellationToken ct = default)
    {
        var normalizedTenantId = IdentityNormalizer.NormalizeTenantId(tenantId);

        var membership = await _membershipStore.FindAsync(normalizedTenantId, userId, profileId, ct);
        return membership?.Membership.Status == MembershipStatus.Active;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetActiveProfileIdsForUserAsync(string tenantId, string userId, CancellationToken ct = default)
    {
        var normalizedTenantId = IdentityNormalizer.NormalizeTenantId(tenantId);

        var memberships = await _membershipStore.GetForUserAsync(normalizedTenantId, userId, ct);
        return memberships
            .Where(m => m.Membership.Status == MembershipStatus.Active)
            .Select(m => m.Membership.ProfileId)
            .ToList();
    }
}
