using Identity.Abstractions;
using Microsoft.EntityFrameworkCore;
using BlazorBook.Web.Data;

namespace BlazorBook.Web.Stores.EFCore;

/// <summary>
/// EF Core implementation of IMembershipStore
/// </summary>
public class EFCoreMembershipStore : IMembershipStore
{
    private readonly ApplicationDbContext _context;

    public EFCoreMembershipStore(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<MembershipRecord?> FindAsync(string tenantId, string userId, string profileId, CancellationToken ct = default)
    {
        var normalizedTenantId = tenantId.ToLowerInvariant();
        
        var membership = await _context.Memberships
            .FirstOrDefaultAsync(m => 
                m.TenantId.ToLower() == normalizedTenantId && 
                m.UserId == userId && 
                m.ProfileId == profileId, ct);

        if (membership == null)
            return null;

        return new MembershipRecord { Membership = membership };
    }

    public async Task<IReadOnlyList<MembershipRecord>> GetForUserAsync(string tenantId, string userId, CancellationToken ct = default)
    {
        var normalizedTenantId = tenantId.ToLowerInvariant();

        var memberships = await _context.Memberships
            .Where(m => m.TenantId.ToLower() == normalizedTenantId && m.UserId == userId)
            .ToListAsync(ct);

        return memberships.Select(m => new MembershipRecord { Membership = m }).ToList();
    }

    public async Task<IReadOnlyList<MembershipRecord>> GetForProfileAsync(string tenantId, string profileId, CancellationToken ct = default)
    {
        var normalizedTenantId = tenantId.ToLowerInvariant();

        var memberships = await _context.Memberships
            .Where(m => m.TenantId.ToLower() == normalizedTenantId && m.ProfileId == profileId)
            .ToListAsync(ct);

        return memberships.Select(m => new MembershipRecord { Membership = m }).ToList();
    }

    public async Task UpsertAsync(MembershipRecord record, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(record.Membership);

        var membership = record.Membership;
        var normalizedTenantId = membership.TenantId.ToLowerInvariant();

        var existing = await _context.Memberships
            .FirstOrDefaultAsync(m => 
                m.TenantId.ToLower() == normalizedTenantId && 
                m.UserId == membership.UserId && 
                m.ProfileId == membership.ProfileId, ct);

        if (existing != null)
        {
            // Update
            existing.Role = membership.Role;
            existing.Status = membership.Status;
            _context.Memberships.Update(existing);
        }
        else
        {
            // Create
            _context.Memberships.Add(membership);
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(string tenantId, string userId, string profileId, CancellationToken ct = default)
    {
        var normalizedTenantId = tenantId.ToLowerInvariant();

        var membership = await _context.Memberships
            .FirstOrDefaultAsync(m => 
                m.TenantId.ToLower() == normalizedTenantId && 
                m.UserId == userId && 
                m.ProfileId == profileId, ct);

        if (membership != null)
        {
            _context.Memberships.Remove(membership);
            await _context.SaveChangesAsync(ct);
        }
    }
}
