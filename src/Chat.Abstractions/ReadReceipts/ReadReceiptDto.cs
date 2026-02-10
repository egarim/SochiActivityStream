namespace Chat.Abstractions;

/// <summary>
/// Read receipt for a participant.
/// </summary>
public sealed class ReadReceiptDto
{
    /// <summary>The participant who read.</summary>
    public required ActivityStream.Abstractions.EntityRefDto Profile { get; set; }

    /// <summary>Last message ID they read up to.</summary>
    public required string LastReadMessageId { get; set; }

    /// <summary>When they read it.</summary>
    public DateTimeOffset ReadAt { get; set; }
}
