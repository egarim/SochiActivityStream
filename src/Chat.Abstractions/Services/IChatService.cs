namespace Chat.Abstractions;

/// <summary>
/// Main chat service interface.
/// </summary>
public interface IChatService
{
    // ─────────────────────────────────────────────────────────────────
    // Conversations
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets or creates a direct (1:1) conversation between two users.
    /// Returns existing conversation if one exists.
    /// </summary>
    Task<ConversationDto> GetOrCreateDirectConversationAsync(
        string tenantId,
        ActivityStream.Abstractions.EntityRefDto user1,
        ActivityStream.Abstractions.EntityRefDto user2,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a new group conversation.
    /// </summary>
    Task<ConversationDto> CreateGroupConversationAsync(
        CreateConversationRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a conversation by ID.
    /// </summary>
    Task<ConversationDto?> GetConversationAsync(
        string tenantId,
        string conversationId,
        ActivityStream.Abstractions.EntityRefDto viewer,
        CancellationToken ct = default);

    /// <summary>
    /// Lists conversations for a participant.
    /// </summary>
    Task<ChatPageResult<ConversationDto>> GetConversationsAsync(
        ConversationQuery query,
        CancellationToken ct = default);

    /// <summary>
    /// Updates conversation settings (title, avatar).
    /// </summary>
    Task<ConversationDto> UpdateConversationAsync(
        UpdateConversationRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Adds a participant to a group conversation.
    /// </summary>
    Task AddParticipantAsync(
        AddParticipantRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Removes a participant from a group conversation.
    /// </summary>
    Task RemoveParticipantAsync(
        RemoveParticipantRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Current user leaves a group conversation.
    /// </summary>
    Task LeaveConversationAsync(
        string tenantId,
        string conversationId,
        ActivityStream.Abstractions.EntityRefDto participant,
        CancellationToken ct = default);

    /// <summary>
    /// Archives/unarchives a conversation for a user.
    /// </summary>
    Task SetArchivedAsync(
        string tenantId,
        string conversationId,
        ActivityStream.Abstractions.EntityRefDto participant,
        bool archived,
        CancellationToken ct = default);

    /// <summary>
    /// Mutes/unmutes a conversation for a user.
    /// </summary>
    Task SetMutedAsync(
        string tenantId,
        string conversationId,
        ActivityStream.Abstractions.EntityRefDto participant,
        bool muted,
        CancellationToken ct = default);

    // ─────────────────────────────────────────────────────────────────
    // Messages
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sends a message to a conversation.
    /// </summary>
    Task<MessageDto> SendMessageAsync(
        SendMessageRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Edits an existing message (sender only).
    /// </summary>
    Task<MessageDto> EditMessageAsync(
        EditMessageRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a message.
    /// </summary>
    Task DeleteMessageAsync(
        DeleteMessageRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Gets messages in a conversation.
    /// </summary>
    Task<ChatPageResult<MessageDto>> GetMessagesAsync(
        MessageQuery query,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a specific message by ID.
    /// </summary>
    Task<MessageDto?> GetMessageAsync(
        string tenantId,
        string conversationId,
        string messageId,
        ActivityStream.Abstractions.EntityRefDto viewer,
        CancellationToken ct = default);

    // ─────────────────────────────────────────────────────────────────
    // Read Receipts
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Marks messages as read up to a given message.
    /// </summary>
    Task MarkReadAsync(
        MarkReadRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Gets total unread count across all conversations for a user.
    /// </summary>
    Task<int> GetTotalUnreadCountAsync(
        string tenantId,
        ActivityStream.Abstractions.EntityRefDto participant,
        CancellationToken ct = default);

    /// <summary>
    /// Gets read receipts for a message.
    /// </summary>
    Task<IReadOnlyList<ReadReceiptDto>> GetReadReceiptsAsync(
        string tenantId,
        string conversationId,
        string messageId,
        CancellationToken ct = default);
}
