using ActivityStream.Abstractions;
using Inbox.Abstractions;

namespace Inbox.Core;

/// <summary>
/// Default implementation of IRecipientExpansionPolicy that returns the recipient unchanged.
/// </summary>
public sealed class DefaultRecipientExpansionPolicy : IRecipientExpansionPolicy
{
    /// <inheritdoc />
    public Task<IReadOnlyList<EntityRefDto>> ExpandRecipientsAsync(
        string tenantId,
        EntityRefDto recipient,
        CancellationToken ct = default)
    {
        // Default v1 behavior: no expansion, return the recipient as-is
        return Task.FromResult<IReadOnlyList<EntityRefDto>>(new[] { recipient });
    }
}
