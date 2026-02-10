namespace ActivityStream.Abstractions.Identity;

public interface IProfileStore
{
    Task CreateProfileAsync(ProfileRecord record, CancellationToken ct = default);
    Task<ProfileRecord?> GetProfileByIdAsync(string id, CancellationToken ct = default);
    Task UpdateProfileAsync(ProfileRecord record, CancellationToken ct = default);
}
