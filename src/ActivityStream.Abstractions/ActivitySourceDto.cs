namespace ActivityStream.Abstractions;

/// <summary>
/// Producer/source metadata for an activity.
/// </summary>
public class ActivitySourceDto
{
    /// <summary>
    /// Producer/system name: erp | ci | importer | ai-agent | etc
    /// </summary>
    public string? System { get; set; }

    /// <summary>
    /// Correlation id for tracing across systems.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Dedup key (unique per tenant + system).
    /// If present along with System, Publish must be idempotent.
    /// </summary>
    public string? IdempotencyKey { get; set; }
}
