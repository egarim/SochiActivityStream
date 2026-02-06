namespace Chat.Abstractions;

/// <summary>
/// Storage interface for conversations.
/// </summary>
public interface IConversationStore
{
    Task<ConversationDto> UpsertAsync(
        ConversationDto conversation,
        CancellationToken ct = default);

    Task<ConversationDto?> GetByIdAsync(
        string tenantId,
        string conversationId,
        CancellationToken ct = default);

    /// <summary>
    /// Finds existing direct conversation between two users.
    /// </summary>
    Task<ConversationDto?> FindDirectConversationAsync(
        string tenantId,
        string profileId1,
        string profileId2,
        CancellationToken ct = default);

    Task<ChatPageResult<ConversationDto>> QueryAsync(
        ConversationQuery query,
        CancellationToken ct = default);

    Task DeleteAsync(
        string tenantId,
        string conversationId,
        CancellationToken ct = default);

    /// <summary>
    /// Updates a participant in the conversation.
    /// </summary>
    Task UpdateParticipantAsync(
        string tenantId,
        string conversationId,
        ConversationParticipantDto participant,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a participant from the conversation.
    /// </summary>
    Task<ConversationParticipantDto?> GetParticipantAsync(
        string tenantId,
        string conversationId,
        string profileId,
        CancellationToken ct = default);

    /// <summary>
    /// Sets per-user settings for a conversation.
    /// </summary>
    Task SetUserSettingsAsync(
        string tenantId,
        string conversationId,
        string profileId,
        bool? isArchived,
        bool? isMuted,
        CancellationToken ct = default);

    /// <summary>
    /// Gets per-user settings for a conversation.
    /// </summary>
    Task<(bool IsArchived, bool IsMuted)> GetUserSettingsAsync(
        string tenantId,
        string conversationId,
        string profileId,
        CancellationToken ct = default);
}
