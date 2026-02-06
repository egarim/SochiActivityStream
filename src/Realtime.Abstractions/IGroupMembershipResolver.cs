namespace Realtime.Abstractions;

/// <summary>
/// Resolves members for conversation/group targets.
/// Implement this to integrate with Chat/Groups services.
/// </summary>
public interface IGroupMembershipResolver
{
    /// <summary>
    /// Gets all profiles in a conversation.
    /// </summary>
    Task<IReadOnlyList<EntityRefDto>> GetConversationMembersAsync(
        string tenantId,
        string conversationId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all profiles in a group.
    /// </summary>
    Task<IReadOnlyList<EntityRefDto>> GetGroupMembersAsync(
        string tenantId,
        string groupId,
        CancellationToken ct = default);
}
