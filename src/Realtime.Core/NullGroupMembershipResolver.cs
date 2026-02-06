using Realtime.Abstractions;

namespace Realtime.Core;

/// <summary>
/// Default implementation that returns empty member lists.
/// Replace with actual Chat/Groups integration.
/// </summary>
public sealed class NullGroupMembershipResolver : IGroupMembershipResolver
{
    /// <inheritdoc />
    public Task<IReadOnlyList<EntityRefDto>> GetConversationMembersAsync(
        string tenantId,
        string conversationId,
        CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<EntityRefDto>>([]);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<EntityRefDto>> GetGroupMembersAsync(
        string tenantId,
        string groupId,
        CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<EntityRefDto>>([]);
    }
}
