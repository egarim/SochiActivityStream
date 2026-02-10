using Chat.Abstractions;

namespace Chat.Core;

/// <summary>
/// No-op chat notifier for testing without real-time functionality.
/// </summary>
public sealed class NullChatNotifier : IChatNotifier
{
    public Task NotifyMessageReceivedAsync(
        ConversationDto conversation,
        MessageDto message,
        CancellationToken ct = default) => Task.CompletedTask;

    public Task NotifyMessageEditedAsync(
        ConversationDto conversation,
        MessageDto message,
        CancellationToken ct = default) => Task.CompletedTask;

    public Task NotifyMessageDeletedAsync(
        string tenantId,
        string conversationId,
        string messageId,
        CancellationToken ct = default) => Task.CompletedTask;

    public Task NotifyReadReceiptAsync(
        string tenantId,
        string conversationId,
        ReadReceiptDto receipt,
        CancellationToken ct = default) => Task.CompletedTask;

    public Task NotifyTypingAsync(
        string tenantId,
        string conversationId,
        ActivityStream.Abstractions.EntityRefDto profile,
        bool isTyping,
        CancellationToken ct = default) => Task.CompletedTask;

    public Task NotifyConversationUpdatedAsync(
        ConversationDto conversation,
        CancellationToken ct = default) => Task.CompletedTask;
}
