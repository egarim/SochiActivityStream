namespace Media.Abstractions;

/// <summary>
/// Options for cleanup operations.
/// </summary>
public sealed class CleanupOptions
{
    /// <summary>
    /// How long pending uploads can stay before expiring.
    /// Default: 1 hour.
    /// </summary>
    public TimeSpan PendingExpiry { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// How long after soft delete before blob is removed.
    /// Default: 7 days.
    /// </summary>
    public TimeSpan DeletedRetention { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Maximum items to process per cleanup run.
    /// </summary>
    public int BatchSize { get; set; } = 100;
}
