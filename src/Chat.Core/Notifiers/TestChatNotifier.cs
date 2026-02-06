using Chat.Abstractions;

namespace Chat.Core;

/// <summary>
/// Chat notifier that captures notifications for testing.
/// </summary>
public sealed class TestChatNotifier : IChatNotifier
{
    public List<(ConversationDto Conversation, MessageDto Message)> MessagesReceived { get; } = [];
    public List<(ConversationDto Conversation, MessageDto Message)> MessagesEdited { get; } = [];
    public List<(string TenantId, string ConversationId, string MessageId)> MessagesDeleted { get; } = [];
    public List<(string TenantId, string ConversationId, ReadReceiptDto Receipt)> ReadReceipts { get; } = [];
    public List<(string TenantId, string ConversationId, EntityRefDto Profile, bool IsTyping)> TypingIndicators { get; } = [];
    public List<ConversationDto> ConversationsUpdated { get; } = [];

    public Task NotifyMessageReceivedAsync(
        ConversationDto conversation,
        MessageDto message,
        CancellationToken ct = default)
    {
        MessagesReceived.Add((conversation, message));
        return Task.CompletedTask;
    }

    public Task NotifyMessageEditedAsync(
        ConversationDto conversation,
        MessageDto message,
        CancellationToken ct = default)
    {
        MessagesEdited.Add((conversation, message));
        return Task.CompletedTask;
    }

    public Task NotifyMessageDeletedAsync(
        string tenantId,
        string conversationId,
        string messageId,
        CancellationToken ct = default)
    {
        MessagesDeleted.Add((tenantId, conversationId, messageId));
        return Task.CompletedTask;
    }

    public Task NotifyReadReceiptAsync(
        string tenantId,
        string conversationId,
        ReadReceiptDto receipt,
        CancellationToken ct = default)
    {
        ReadReceipts.Add((tenantId, conversationId, receipt));
        return Task.CompletedTask;
    }

    public Task NotifyTypingAsync(
        string tenantId,
        string conversationId,
        EntityRefDto profile,
        bool isTyping,
        CancellationToken ct = default)
    {
        TypingIndicators.Add((tenantId, conversationId, profile, isTyping));
        return Task.CompletedTask;
    }

    public Task NotifyConversationUpdatedAsync(
        ConversationDto conversation,
        CancellationToken ct = default)
    {
        ConversationsUpdated.Add(conversation);
        return Task.CompletedTask;
    }

    public void Clear()
    {
        MessagesReceived.Clear();
        MessagesEdited.Clear();
        MessagesDeleted.Clear();
        ReadReceipts.Clear();
        TypingIndicators.Clear();
        ConversationsUpdated.Clear();
    }
}
