using ActivityStream.Abstractions;

namespace Inbox.Abstractions;

/// <summary>
/// Policy hook for expanding recipients (e.g., Team → members).
/// Default implementation returns the recipient unchanged.
/// </summary>
public interface IRecipientExpansionPolicy
{
    /// <summary>
    /// Expands a recipient to a list of actual inbox owners.
    /// For example, a Team entity might expand to its member Profiles.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="recipient">The recipient to expand.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of expanded recipients (may be just the original).</returns>
    Task<IReadOnlyList<EntityRefDto>> ExpandRecipientsAsync(
        string tenantId,
        EntityRefDto recipient,
        CancellationToken ct = default);
}
