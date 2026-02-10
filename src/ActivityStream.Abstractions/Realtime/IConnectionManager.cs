namespace ActivityStream.Abstractions.Realtime;

public interface IConnectionManager
{
    Task<IEnumerable<string>> GetConnectionsForUserAsync(string userId, CancellationToken ct = default);
}
