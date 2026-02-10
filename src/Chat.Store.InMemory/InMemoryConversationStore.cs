using System.Collections.Concurrent;
using System.Text.Json;
using ActivityStream.Abstractions;
using Chat.Abstractions;

namespace Chat.Store.InMemory;

/// <summary>
/// In-memory implementation of IConversationStore.
/// </summary>
public sealed class InMemoryConversationStore : IConversationStore
{
    private readonly ConcurrentDictionary<string, ConversationDto> _conversations = new();
    private readonly ConcurrentDictionary<string, (bool IsArchived, bool IsMuted)> _userSettings = new();
    private readonly ReaderWriterLockSlim _lock = new();

    private static string Key(string tenantId, string id) => $"{tenantId}|{id}";
    private static string SettingsKey(string tenantId, string conversationId, string profileId) =>
        $"{tenantId}|{conversationId}|{profileId}";

    public Task<ConversationDto> UpsertAsync(
        ConversationDto conversation,
        CancellationToken ct = default)
    {
        _lock.EnterWriteLock();
        try
        {
            var key = Key(conversation.TenantId, conversation.Id!);
            var clone = Clone(conversation);
            _conversations[key] = clone;
            return Task.FromResult(Clone(clone));
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task<ConversationDto?> GetByIdAsync(
        string tenantId,
        string conversationId,
        CancellationToken ct = default)
    {
        _lock.EnterReadLock();
        try
        {
            var key = Key(tenantId, conversationId);
            if (_conversations.TryGetValue(key, out var conversation))
                return Task.FromResult<ConversationDto?>(Clone(conversation));
            return Task.FromResult<ConversationDto?>(null);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<ConversationDto?> FindDirectConversationAsync(
        string tenantId,
        string profileId1,
        string profileId2,
        CancellationToken ct = default)
    {
        _lock.EnterReadLock();
        try
        {
            var match = _conversations.Values
                .Where(c => c.TenantId == tenantId && c.Type == ConversationType.Direct)
                .FirstOrDefault(c =>
                {
                    var ids = c.Participants
                        .Where(p => !p.HasLeft)
                        .Select(p => p.Profile.Id)
                        .ToHashSet();
                    return ids.Contains(profileId1) && ids.Contains(profileId2);
                });

            return Task.FromResult(match is null ? null : Clone(match));
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<ChatPageResult<ConversationDto>> QueryAsync(
        ConversationQuery query,
        CancellationToken ct = default)
    {
        _lock.EnterReadLock();
        try
        {
            var conversations = _conversations.Values
                .Where(c => c.TenantId == query.TenantId)
                .Where(c => c.Participants.Any(p => p.Profile.Id == query.Participant.Id && !p.HasLeft))
                .AsEnumerable();

            // Filter by type
            if (query.Type.HasValue)
            {
                conversations = conversations.Where(c => c.Type == query.Type.Value);
            }

            // Filter by archived
            if (!query.IncludeArchived)
            {
                conversations = conversations.Where(c =>
                {
                    var key = SettingsKey(c.TenantId, c.Id!, query.Participant.Id);
                    return !_userSettings.TryGetValue(key, out var settings) || !settings.IsArchived;
                });
            }

            // Order by UpdatedAt descending
            var ordered = conversations.OrderByDescending(c => c.UpdatedAt).ToList();

            // Apply cursor
            if (!string.IsNullOrEmpty(query.Cursor))
            {
                var cursorIndex = ordered.FindIndex(c => c.Id == query.Cursor);
                if (cursorIndex >= 0)
                {
                    ordered = ordered.Skip(cursorIndex + 1).ToList();
                }
            }

            var items = ordered.Take(query.Limit).Select(Clone).ToList();
            var hasMore = ordered.Count > query.Limit;

            return Task.FromResult(new ChatPageResult<ConversationDto>
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

    public Task DeleteAsync(
        string tenantId,
        string conversationId,
        CancellationToken ct = default)
    {
        _lock.EnterWriteLock();
        try
        {
            var key = Key(tenantId, conversationId);
            _conversations.TryRemove(key, out _);
            return Task.CompletedTask;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task UpdateParticipantAsync(
        string tenantId,
        string conversationId,
        ConversationParticipantDto participant,
        CancellationToken ct = default)
    {
        _lock.EnterWriteLock();
        try
        {
            var key = Key(tenantId, conversationId);
            if (_conversations.TryGetValue(key, out var conversation))
            {
                var existingIndex = conversation.Participants.FindIndex(p => p.Profile.Id == participant.Profile.Id);
                if (existingIndex >= 0)
                {
                    conversation.Participants[existingIndex] = CloneParticipant(participant);
                }
                else
                {
                    conversation.Participants.Add(CloneParticipant(participant));
                }
            }
            return Task.CompletedTask;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task<ConversationParticipantDto?> GetParticipantAsync(
        string tenantId,
        string conversationId,
        string profileId,
        CancellationToken ct = default)
    {
        _lock.EnterReadLock();
        try
        {
            var key = Key(tenantId, conversationId);
            if (_conversations.TryGetValue(key, out var conversation))
            {
                var participant = conversation.Participants.FirstOrDefault(p => p.Profile.Id == profileId);
                if (participant != null)
                    return Task.FromResult<ConversationParticipantDto?>(CloneParticipant(participant));
            }
            return Task.FromResult<ConversationParticipantDto?>(null);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task SetUserSettingsAsync(
        string tenantId,
        string conversationId,
        string profileId,
        bool? isArchived,
        bool? isMuted,
        CancellationToken ct = default)
    {
        var key = SettingsKey(tenantId, conversationId, profileId);

        _userSettings.AddOrUpdate(
            key,
            _ => (isArchived ?? false, isMuted ?? false),
            (_, existing) => (isArchived ?? existing.IsArchived, isMuted ?? existing.IsMuted));

        return Task.CompletedTask;
    }

    public Task<(bool IsArchived, bool IsMuted)> GetUserSettingsAsync(
        string tenantId,
        string conversationId,
        string profileId,
        CancellationToken ct = default)
    {
        var key = SettingsKey(tenantId, conversationId, profileId);
        if (_userSettings.TryGetValue(key, out var settings))
            return Task.FromResult(settings);
        return Task.FromResult((false, false));
    }

    private static ConversationDto Clone(ConversationDto source)
    {
        return new ConversationDto
        {
            Id = source.Id,
            TenantId = source.TenantId,
            Type = source.Type,
            Title = source.Title,
            AvatarUrl = source.AvatarUrl,
            Participants = source.Participants.Select(CloneParticipant).ToList(),
            LastMessage = source.LastMessage != null ? CloneMessage(source.LastMessage) : null,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt,
            IsArchived = source.IsArchived,
            IsMuted = source.IsMuted,
            UnreadCount = source.UnreadCount
        };
    }

    private static ConversationParticipantDto CloneParticipant(ConversationParticipantDto source)
    {
        return new ConversationParticipantDto
        {
            Profile = CloneEntityRef(source.Profile),
            Role = source.Role,
            JoinedAt = source.JoinedAt,
            LastReadAt = source.LastReadAt,
            LastReadMessageId = source.LastReadMessageId,
            HasLeft = source.HasLeft,
            LeftAt = source.LeftAt
        };
    }

    private static EntityRefDto CloneEntityRef(EntityRefDto source)
    {
        return new EntityRefDto
        {
            Id = source.Id,
            Type = source.Type,
            DisplayName = source.DisplayName,
            AvatarUrl = source.AvatarUrl
        };
    }

    private static MessageDto CloneMessage(MessageDto source)
    {
        return new MessageDto
        {
            Id = source.Id,
            TenantId = source.TenantId,
            ConversationId = source.ConversationId,
            Sender = CloneEntityRef(source.Sender),
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
            CreatedAt = source.CreatedAt,
            EditedAt = source.EditedAt,
            IsDeleted = source.IsDeleted,
            DeletedByProfileIds = source.DeletedByProfileIds?.ToList(),
            SystemMessageType = source.SystemMessageType
        };
    }
}
