using System.Collections.Concurrent;
using Chat.Abstractions;

namespace Chat.Store.InMemory;

/// <summary>
/// In-memory implementation of IMessageStore.
/// </summary>
public sealed class InMemoryMessageStore : IMessageStore
{
    private readonly ConcurrentDictionary<string, MessageDto> _messages = new();
    private readonly ReaderWriterLockSlim _lock = new();

    private static string Key(string tenantId, string conversationId, string messageId) =>
        $"{tenantId}|{conversationId}|{messageId}";

    public Task<MessageDto> UpsertAsync(
        MessageDto message,
        CancellationToken ct = default)
    {
        _lock.EnterWriteLock();
        try
        {
            var key = Key(message.TenantId, message.ConversationId, message.Id!);
            var clone = Clone(message);
            _messages[key] = clone;
            return Task.FromResult(Clone(clone));
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task<MessageDto?> GetByIdAsync(
        string tenantId,
        string conversationId,
        string messageId,
        CancellationToken ct = default)
    {
        _lock.EnterReadLock();
        try
        {
            var key = Key(tenantId, conversationId, messageId);
            if (_messages.TryGetValue(key, out var message))
                return Task.FromResult<MessageDto?>(Clone(message));
            return Task.FromResult<MessageDto?>(null);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<ChatPageResult<MessageDto>> QueryAsync(
        MessageQuery query,
        CancellationToken ct = default)
    {
        _lock.EnterReadLock();
        try
        {
            var messages = _messages.Values
                .Where(m => m.TenantId == query.TenantId &&
                           m.ConversationId == query.ConversationId &&
                           !m.IsDeleted &&
                           !(m.DeletedByProfileIds?.Contains(query.Viewer.Id) ?? false))
                .ToList();

            // Order by CreatedAt
            if (query.Direction == MessageQueryDirection.Older)
            {
                messages = messages.OrderByDescending(m => m.CreatedAt).ToList();
            }
            else
            {
                messages = messages.OrderBy(m => m.CreatedAt).ToList();
            }

            // Apply cursor
            if (!string.IsNullOrEmpty(query.Cursor))
            {
                var cursorIndex = messages.FindIndex(m => m.Id == query.Cursor);
                if (cursorIndex >= 0)
                {
                    messages = messages.Skip(cursorIndex + 1).ToList();
                }
            }

            var items = messages.Take(query.Limit).Select(Clone).ToList();
            var hasMore = messages.Count > query.Limit;

            return Task.FromResult(new ChatPageResult<MessageDto>
            {
                Items = items,
                NextCursor = hasMore && items.Count > 0 ? items[^1].Id : null,
                HasMore = hasMore
            });
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task SoftDeleteAsync(
        string tenantId,
        string conversationId,
        string messageId,
        string profileId,
        CancellationToken ct = default)
    {
        _lock.EnterWriteLock();
        try
        {
            var key = Key(tenantId, conversationId, messageId);
            if (_messages.TryGetValue(key, out var message))
            {
                message.DeletedByProfileIds ??= [];
                if (!message.DeletedByProfileIds.Contains(profileId))
                {
                    message.DeletedByProfileIds.Add(profileId);
                }
            }
            return Task.CompletedTask;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task HardDeleteAsync(
        string tenantId,
        string conversationId,
        string messageId,
        CancellationToken ct = default)
    {
        _lock.EnterWriteLock();
        try
        {
            var key = Key(tenantId, conversationId, messageId);
            if (_messages.TryGetValue(key, out var message))
            {
                message.IsDeleted = true;
                message.Body = "[Message deleted]";
            }
            return Task.CompletedTask;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task<MessageDto?> GetLatestMessageAsync(
        string tenantId,
        string conversationId,
        CancellationToken ct = default)
    {
        _lock.EnterReadLock();
        try
        {
            var latest = _messages.Values
                .Where(m => m.TenantId == tenantId &&
                           m.ConversationId == conversationId &&
                           !m.IsDeleted)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefault();

            return Task.FromResult(latest is null ? null : Clone(latest));
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<int> CountMessagesAfterAsync(
        string tenantId,
        string conversationId,
        string? afterMessageId,
        string excludeProfileId,
        CancellationToken ct = default)
    {
        _lock.EnterReadLock();
        try
        {
            var messages = _messages.Values
                .Where(m => m.TenantId == tenantId &&
                           m.ConversationId == conversationId &&
                           !m.IsDeleted &&
                           !(m.DeletedByProfileIds?.Contains(excludeProfileId) ?? false) &&
                           m.Sender.Id != excludeProfileId)
                .OrderBy(m => m.CreatedAt)
                .ToList();

            if (string.IsNullOrEmpty(afterMessageId))
            {
                return Task.FromResult(messages.Count);
            }

            var afterIndex = messages.FindIndex(m => m.Id == afterMessageId);
            if (afterIndex < 0)
            {
                // Message not found, return all messages
                return Task.FromResult(messages.Count);
            }

            return Task.FromResult(messages.Count - afterIndex - 1);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    private static MessageDto Clone(MessageDto source)
    {
        return new MessageDto
        {
            Id = source.Id,
            TenantId = source.TenantId,
            ConversationId = source.ConversationId,
            Sender = new EntityRefDto
            {
                Id = source.Sender.Id,
                Type = source.Sender.Type,
                DisplayName = source.Sender.DisplayName,
                AvatarUrl = source.Sender.AvatarUrl
            },
            Body = source.Body,
            Media = source.Media?.Select(m => new MediaRefDto
            {
                Id = m.Id,
                Type = m.Type,
                Url = m.Url,
                ThumbnailUrl = m.ThumbnailUrl,
                FileName = m.FileName,
                SizeBytes = m.SizeBytes,
                ContentType = m.ContentType
            }).ToList(),
            ReplyToMessageId = source.ReplyToMessageId,
            ReplyTo = source.ReplyTo != null ? Clone(source.ReplyTo) : null,
            CreatedAt = source.CreatedAt,
            EditedAt = source.EditedAt,
            IsDeleted = source.IsDeleted,
            DeletedByProfileIds = source.DeletedByProfileIds?.ToList(),
            SystemMessageType = source.SystemMessageType
        };
    }
}
