namespace ActivityStream.Abstractions.Identity;

public interface IUserStore
{
    Task<UserRecord?> GetUserByIdAsync(string userId, CancellationToken ct = default);
    Task<UserRecord?> GetUserByLoginAsync(string login, CancellationToken ct = default);
}
