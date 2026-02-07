using Identity.Abstractions;
using Microsoft.EntityFrameworkCore;
using BlazorBook.Web.Data;

namespace BlazorBook.Web.Stores.EFCore;

/// <summary>
/// EF Core implementation of IProfileStore
/// </summary>
public class EFCoreProfileStore : IProfileStore
{
    private readonly ApplicationDbContext _context;

    public EFCoreProfileStore(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<ProfileRecord?> GetByIdAsync(string profileId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(profileId))
            return null;

        var profile = await _context.Profiles
            .FirstOrDefaultAsync(p => p.Id == profileId, ct);

        return profile != null ? new ProfileRecord { Profile = profile } : null;
    }

    public async Task<ProfileRecord?> FindByHandleAsync(string handleNormalized, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(handleNormalized))
            return null;

        var profile = await _context.Profiles
            .FirstOrDefaultAsync(p => p.Handle.ToLower() == handleNormalized.ToLower(), ct);

        return profile != null ? new ProfileRecord { Profile = profile } : null;
    }

    public async Task CreateAsync(ProfileRecord record, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(record.Profile);

        if (string.IsNullOrEmpty(record.Profile.Id))
            throw new ArgumentException("Profile must have an Id.", nameof(record));

        _context.Profiles.Add(record.Profile);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(ProfileRecord record, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(record.Profile);

        if (string.IsNullOrEmpty(record.Profile.Id))
            throw new ArgumentException("Profile must have an Id.", nameof(record));

        var existing = await _context.Profiles
            .FirstOrDefaultAsync(p => p.Id == record.Profile.Id, ct);

        if (existing == null)
            throw new InvalidOperationException($"Profile with Id '{record.Profile.Id}' not found.");

        existing.Handle = record.Profile.Handle;
        existing.DisplayName = record.Profile.DisplayName;
        existing.AvatarUrl = record.Profile.AvatarUrl;
        existing.IsPrivate = record.Profile.IsPrivate;

        await _context.SaveChangesAsync(ct);
    }
}
