namespace Chat.Core;

/// <summary>
/// Configuration options for the Chat Service.
/// </summary>
public sealed class ChatServiceOptions
{
    /// <summary>Maximum message body length.</summary>
    public int MaxMessageBodyLength { get; set; } = 10_000;

    /// <summary>Maximum group title length.</summary>
    public int MaxTitleLength { get; set; } = 100;

    /// <summary>Maximum participants in a group.</summary>
    public int MaxGroupParticipants { get; set; } = 100;

    /// <summary>Minimum participants to create a group.</summary>
    public int MinGroupParticipants { get; set; } = 2;

    /// <summary>Default page size for queries.</summary>
    public int DefaultPageSize { get; set; } = 50;

    /// <summary>Maximum page size.</summary>
    public int MaxPageSize { get; set; } = 100;

    /// <summary>Time window for editing messages (Zero = unlimited).</summary>
    public TimeSpan EditWindowDuration { get; set; } = TimeSpan.Zero;
}
