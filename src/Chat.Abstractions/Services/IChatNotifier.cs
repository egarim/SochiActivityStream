namespace Chat.Abstractions;

/// <summary>
/// Interface for pushing real-time chat events.
/// Implemented by integrating with IRealtimePublisher.
/// </summary>
public interface IChatNotifier
{
    /// <summary>
    /// Notifies participants of a new message.
    /// </summary>
    Task NotifyMessageReceivedAsync(
        ConversationDto conversation,
        MessageDto message,
        CancellationToken ct = default);

    /// <summary>
    /// Notifies participants of a message edit.
    /// </summary>
    Task NotifyMessageEditedAsync(
        ConversationDto conversation,
        MessageDto message,
        CancellationToken ct = default);

    /// <summary>
    /// Notifies participants of a message deletion.
    /// </summary>
    Task NotifyMessageDeletedAsync(
        string tenantId,
        string conversationId,
        string messageId,
        CancellationToken ct = default);

    /// <summary>
    /// Notifies of read receipt update.
    /// </summary>
    Task NotifyReadReceiptAsync(
        string tenantId,
        string conversationId,
        ReadReceiptDto receipt,
        CancellationToken ct = default);

    /// <summary>
    /// Notifies of typing indicator.
    /// </summary>
    Task NotifyTypingAsync(
        string tenantId,
        string conversationId,
        ActivityStream.Abstractions.EntityRefDto profile,
        bool isTyping,
        CancellationToken ct = default);

    /// <summary>
    /// Notifies of conversation update (title, avatar, participants).
    /// </summary>
    Task NotifyConversationUpdatedAsync(
        ConversationDto conversation,
        CancellationToken ct = default);
}
