using Identity.Abstractions;
using Microsoft.EntityFrameworkCore;
using BlazorBook.Web.Data;
using BlazorBook.Web.Data.Configurations;

namespace BlazorBook.Web.Stores.EFCore;

/// <summary>
/// EF Core implementation of IUserStore
/// </summary>
public class EFCoreUserStore : IUserStore
{
    private readonly ApplicationDbContext _context;

    public EFCoreUserStore(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<UserRecord?> FindByUsernameAsync(string usernameNormalized, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(usernameNormalized))
            return null;

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == usernameNormalized, ct);

        if (user == null)
            return null;

        return await GetRecordAsync(user, ct);
    }

    public async Task<UserRecord?> FindByEmailAsync(string emailNormalized, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(emailNormalized))
            return null;

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == emailNormalized, ct);

        if (user == null)
            return null;

        return await GetRecordAsync(user, ct);
    }

    public async Task<UserRecord?> GetByIdAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId))
            return null;

        var user = await _context.Users.FindAsync(new object[] { userId }, ct);
        
        if (user == null)
            return null;

        return await GetRecordAsync(user, ct);
    }

    public async Task CreateAsync(UserRecord record, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(record.User);
        ArgumentNullException.ThrowIfNull(record.Hash);

        if (string.IsNullOrEmpty(record.User.Id))
            throw new ArgumentException("User must have an Id.", nameof(record));

        // Add user
        _context.Users.Add(record.User);

        // Add password hash
        var passwordEntity = new UserPasswordEntity
        {
            UserId = record.User.Id,
            Salt = record.Hash.Salt,
            Iterations = record.Hash.Iterations,
            HashBytes = record.Hash.HashBytes,
            Algorithm = record.Hash.Algorithm
        };
        _context.UserPasswords.Add(passwordEntity);

        await _context.SaveChangesAsync(ct);
    }

    private async Task<UserRecord?> GetRecordAsync(UserDto user, CancellationToken ct)
    {
        var passwordEntity = await _context.UserPasswords
            .FirstOrDefaultAsync(p => p.UserId == user.Id, ct);

        if (passwordEntity == null)
            return null;

        var hash = new PasswordHash(
            passwordEntity.Salt,
            passwordEntity.Iterations,
            passwordEntity.HashBytes,
            passwordEntity.Algorithm
        );

        return new UserRecord
        {
            User = user,
            Hash = hash
        };
    }
}
