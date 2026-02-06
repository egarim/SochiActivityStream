namespace Realtime.Abstractions;

/// <summary>
/// Envelope for all real-time events.
/// </summary>
public sealed class RealtimeEvent
{
    /// <summary>Unique event ID (for deduplication/ack).</summary>
    public string? Id { get; set; }

    /// <summary>Tenant isolation.</summary>
    public required string TenantId { get; set; }

    /// <summary>Event type (e.g., "message.received").</summary>
    public required string Type { get; set; }

    /// <summary>Event payload (serialized as JSON).</summary>
    public required object Payload { get; set; }

    /// <summary>Target for delivery.</summary>
    public required EventTarget Target { get; set; }

    /// <summary>When the event was created.</summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>Optional correlation ID for tracing.</summary>
    public string? CorrelationId { get; set; }
}
