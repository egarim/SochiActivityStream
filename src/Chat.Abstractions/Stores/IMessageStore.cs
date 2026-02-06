namespace Chat.Abstractions;

/// <summary>
/// Storage interface for messages.
/// </summary>
public interface IMessageStore
{
    Task<MessageDto> UpsertAsync(
        MessageDto message,
        CancellationToken ct = default);

    Task<MessageDto?> GetByIdAsync(
        string tenantId,
        string conversationId,
        string messageId,
        CancellationToken ct = default);

    Task<ChatPageResult<MessageDto>> QueryAsync(
        MessageQuery query,
        CancellationToken ct = default);

    /// <summary>
    /// Soft delete (add profile to DeletedByProfileIds).
    /// </summary>
    Task SoftDeleteAsync(
        string tenantId,
        string conversationId,
        string messageId,
        string profileId,
        CancellationToken ct = default);

    /// <summary>
    /// Hard delete (for delete-for-everyone).
    /// </summary>
    Task HardDeleteAsync(
        string tenantId,
        string conversationId,
        string messageId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the latest message in a conversation.
    /// </summary>
    Task<MessageDto?> GetLatestMessageAsync(
        string tenantId,
        string conversationId,
        CancellationToken ct = default);

    /// <summary>
    /// Counts messages after a given message ID (for unread count).
    /// </summary>
    Task<int> CountMessagesAfterAsync(
        string tenantId,
        string conversationId,
        string? afterMessageId,
        string excludeProfileId,
        CancellationToken ct = default);
}
