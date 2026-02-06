namespace Realtime.Abstractions;

/// <summary>
/// Metadata about an active connection.
/// </summary>
public sealed class ConnectionInfo
{
    /// <summary>Unique connection identifier.</summary>
    public required string ConnectionId { get; set; }

    /// <summary>Tenant this connection belongs to.</summary>
    public required string TenantId { get; set; }

    /// <summary>The connected profile.</summary>
    public required EntityRefDto Profile { get; set; }

    /// <summary>When the connection was established.</summary>
    public DateTimeOffset ConnectedAt { get; set; }

    /// <summary>Last activity timestamp (for idle detection).</summary>
    public DateTimeOffset LastActivityAt { get; set; }

    /// <summary>Transport type (SignalR, WebSocket, etc.).</summary>
    public string? TransportType { get; set; }

    /// <summary>Client-provided metadata (device, platform, etc.).</summary>
    public Dictionary<string, string>? Metadata { get; set; }
}
