namespace Realtime.Abstractions;

/// <summary>
/// Manages connection lifecycle and mapping.
/// </summary>
public interface IConnectionManager
{
    /// <summary>
    /// Registers a new connection.
    /// </summary>
    Task<ConnectionInfo> AddConnectionAsync(
        string connectionId,
        string tenantId,
        EntityRefDto profile,
        Dictionary<string, string>? metadata = null,
        CancellationToken ct = default);

    /// <summary>
    /// Removes a connection (disconnect).
    /// </summary>
    Task RemoveConnectionAsync(string connectionId, CancellationToken ct = default);

    /// <summary>
    /// Updates last activity (for idle detection).
    /// </summary>
    Task TouchConnectionAsync(string connectionId, CancellationToken ct = default);

    /// <summary>
    /// Gets all connections for a profile.
    /// </summary>
    Task<IReadOnlyList<ConnectionInfo>> GetConnectionsForProfileAsync(
        string tenantId,
        EntityRefDto profile,
        CancellationToken ct = default);

    /// <summary>
    /// Gets connection info by ID.
    /// </summary>
    Task<ConnectionInfo?> GetConnectionAsync(string connectionId, CancellationToken ct = default);

    /// <summary>
    /// Gets all connections in a tenant.
    /// </summary>
    Task<IReadOnlyList<ConnectionInfo>> GetTenantConnectionsAsync(
        string tenantId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets connection count for a profile.
    /// </summary>
    Task<int> GetConnectionCountAsync(
        string tenantId,
        EntityRefDto profile,
        CancellationToken ct = default);
}
