namespace Chat.Abstractions;

/// <summary>
/// Query for listing conversations.
/// </summary>
public sealed class ConversationQuery
{
    public required string TenantId { get; set; }

    /// <summary>Filter to conversations this profile is a participant of.</summary>
    public required ActivityStream.Abstractions.EntityRefDto Participant { get; set; }

    /// <summary>Filter by conversation type.</summary>
    public ConversationType? Type { get; set; }

    /// <summary>Include archived conversations.</summary>
    public bool IncludeArchived { get; set; }

    /// <summary>Cursor for pagination.</summary>
    public string? Cursor { get; set; }

    /// <summary>Page size.</summary>
    public int Limit { get; set; } = 20;
}
