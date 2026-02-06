using Chat.Abstractions;

namespace Chat.Core;

/// <summary>
/// Main chat service implementation.
/// </summary>
public sealed class ChatService : IChatService
{
    private readonly IConversationStore _conversationStore;
    private readonly IMessageStore _messageStore;
    private readonly IChatNotifier _notifier;
    private readonly IIdGenerator _idGenerator;
    private readonly ChatServiceOptions _options;

    public ChatService(
        IConversationStore conversationStore,
        IMessageStore messageStore,
        IChatNotifier notifier,
        IIdGenerator idGenerator,
        ChatServiceOptions? options = null)
    {
        _conversationStore = conversationStore ?? throw new ArgumentNullException(nameof(conversationStore));
        _messageStore = messageStore ?? throw new ArgumentNullException(nameof(messageStore));
        _notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
        _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
        _options = options ?? new ChatServiceOptions();
    }

    // ─────────────────────────────────────────────────────────────────
    // Conversations
    // ─────────────────────────────────────────────────────────────────

    public async Task<ConversationDto> GetOrCreateDirectConversationAsync(
        string tenantId,
        EntityRefDto user1,
        EntityRefDto user2,
        CancellationToken ct = default)
    {
        ChatValidator.ValidateTenantId(tenantId);
        ChatValidator.ValidateProfile(user1);
        ChatValidator.ValidateProfile(user2);

        // Check for existing
        var existing = await _conversationStore.FindDirectConversationAsync(
            tenantId, user1.Id, user2.Id, ct);

        if (existing != null)
        {
            // Populate viewer-specific fields for user1
            existing = await PopulateViewerFieldsAsync(existing, user1, ct);
            return existing;
        }

        // Create new
        var now = DateTimeOffset.UtcNow;
        var conversation = new ConversationDto
        {
            Id = _idGenerator.NewId(),
            TenantId = tenantId,
            Type = ConversationType.Direct,
            Participants =
            [
                new() { Profile = user1, Role = ParticipantRole.Member, JoinedAt = now },
                new() { Profile = user2, Role = ParticipantRole.Member, JoinedAt = now }
            ],
            CreatedAt = now,
            UpdatedAt = now
        };

        return await _conversationStore.UpsertAsync(conversation, ct);
    }

    public async Task<ConversationDto> CreateGroupConversationAsync(
        CreateConversationRequest request,
        CancellationToken ct = default)
    {
        ChatValidator.ValidateCreateGroupConversation(request, _options);

        var now = DateTimeOffset.UtcNow;

        // Ensure creator is in participants
        var allParticipants = request.Participants.ToList();
        if (!allParticipants.Any(p => p.Id == request.Creator.Id))
        {
            allParticipants.Insert(0, request.Creator);
        }

        var conversation = new ConversationDto
        {
            Id = _idGenerator.NewId(),
            TenantId = request.TenantId,
            Type = ConversationType.Group,
            Title = ChatNormalizer.NormalizeTitle(request.Title),
            Participants = allParticipants.Select((p, index) => new ConversationParticipantDto
            {
                Profile = p,
                Role = p.Id == request.Creator.Id ? ParticipantRole.Owner : ParticipantRole.Member,
                JoinedAt = now
            }).ToList(),
            CreatedAt = now,
            UpdatedAt = now
        };

        var result = await _conversationStore.UpsertAsync(conversation, ct);
        await _notifier.NotifyConversationUpdatedAsync(result, ct);
        return result;
    }

    public async Task<ConversationDto?> GetConversationAsync(
        string tenantId,
        string conversationId,
        EntityRefDto viewer,
        CancellationToken ct = default)
    {
        ChatValidator.ValidateTenantId(tenantId);
        ChatValidator.ValidateConversationId(conversationId);
        ChatValidator.ValidateProfile(viewer);

        var conversation = await _conversationStore.GetByIdAsync(tenantId, conversationId, ct);
        if (conversation == null)
            return null;

        // Verify viewer is participant
        var isParticipant = conversation.Participants.Any(p => p.Profile.Id == viewer.Id && !p.HasLeft);
        if (!isParticipant)
            return null;

        return await PopulateViewerFieldsAsync(conversation, viewer, ct);
    }

    public async Task<ChatPageResult<ConversationDto>> GetConversationsAsync(
        ConversationQuery query,
        CancellationToken ct = default)
    {
        ChatValidator.ValidateConversationQuery(query);

        query.Limit = Math.Clamp(query.Limit, 1, _options.MaxPageSize);

        var result = await _conversationStore.QueryAsync(query, ct);

        // Populate viewer-specific fields for each conversation
        for (int i = 0; i < result.Items.Count; i++)
        {
            result.Items[i] = await PopulateViewerFieldsAsync(result.Items[i], query.Participant, ct);
        }

        return result;
    }

    public async Task<ConversationDto> UpdateConversationAsync(
        UpdateConversationRequest request,
        CancellationToken ct = default)
    {
        ChatValidator.ValidateUpdateConversation(request, _options);

        var conversation = await GetAndVerifyParticipantAsync(
            request.TenantId, request.ConversationId, request.Actor, ct);

        // For groups, verify actor has permission
        if (conversation.Type == ConversationType.Group)
        {
            var actorParticipant = conversation.Participants.First(p => p.Profile.Id == request.Actor.Id);
            if (actorParticipant.Role == ParticipantRole.Member)
                throw new ChatValidationException(ChatValidationError.NotAuthorized);
        }

        var now = DateTimeOffset.UtcNow;
        var oldTitle = conversation.Title;

        if (request.Title != null)
            conversation.Title = ChatNormalizer.NormalizeTitle(request.Title);
        if (request.AvatarUrl != null)
            conversation.AvatarUrl = request.AvatarUrl;

        conversation.UpdatedAt = now;

        var result = await _conversationStore.UpsertAsync(conversation, ct);

        // Create system message if title changed
        if (request.Title != null && oldTitle != conversation.Title)
        {
            await CreateSystemMessageAsync(
                request.TenantId,
                request.ConversationId,
                request.Actor,
                SystemMessageType.TitleChanged,
                $"changed the title to \"{conversation.Title}\"",
                ct);
        }

        await _notifier.NotifyConversationUpdatedAsync(result, ct);
        return result;
    }

    public async Task AddParticipantAsync(
        AddParticipantRequest request,
        CancellationToken ct = default)
    {
        ChatValidator.ValidateAddParticipant(request);

        var conversation = await GetAndVerifyParticipantAsync(
            request.TenantId, request.ConversationId, request.Actor, ct);

        // Cannot add participants to direct conversations
        if (conversation.Type == ConversationType.Direct)
            throw new ChatValidationException(ChatValidationError.DirectConversationCannotAddParticipants);

        // Verify actor has permission
        var actorParticipant = conversation.Participants.First(p => p.Profile.Id == request.Actor.Id);
        if (actorParticipant.Role == ParticipantRole.Member)
            throw new ChatValidationException(ChatValidationError.NotAuthorized);

        // Check if already a participant
        var existingParticipant = conversation.Participants.FirstOrDefault(p => p.Profile.Id == request.NewParticipant.Id);
        if (existingParticipant != null && !existingParticipant.HasLeft)
            throw new ChatValidationException(ChatValidationError.ParticipantAlreadyInConversation);

        // Check max participants
        var activeCount = conversation.Participants.Count(p => !p.HasLeft);
        if (activeCount >= _options.MaxGroupParticipants)
            throw new ChatValidationException(ChatValidationError.TooManyParticipants);

        var now = DateTimeOffset.UtcNow;

        if (existingParticipant != null)
        {
            // Re-join
            existingParticipant.HasLeft = false;
            existingParticipant.LeftAt = null;
            existingParticipant.JoinedAt = now;
            await _conversationStore.UpdateParticipantAsync(
                request.TenantId, request.ConversationId, existingParticipant, ct);
        }
        else
        {
            // New participant
            var newParticipant = new ConversationParticipantDto
            {
                Profile = request.NewParticipant,
                Role = ParticipantRole.Member,
                JoinedAt = now
            };
            conversation.Participants.Add(newParticipant);
            await _conversationStore.UpsertAsync(conversation, ct);
        }

        // Create system message
        await CreateSystemMessageAsync(
            request.TenantId,
            request.ConversationId,
            request.Actor,
            SystemMessageType.ParticipantJoined,
            $"added {request.NewParticipant.DisplayName ?? request.NewParticipant.Id}",
            ct);

        await _notifier.NotifyConversationUpdatedAsync(conversation, ct);
    }

    public async Task RemoveParticipantAsync(
        RemoveParticipantRequest request,
        CancellationToken ct = default)
    {
        ChatValidator.ValidateRemoveParticipant(request);

        var conversation = await GetAndVerifyParticipantAsync(
            request.TenantId, request.ConversationId, request.Actor, ct);

        // Cannot remove participants from direct conversations
        if (conversation.Type == ConversationType.Direct)
            throw new ChatValidationException(ChatValidationError.DirectConversationCannotRemoveParticipants);

        // Verify actor has permission (admin/owner can remove, or self)
        var actorParticipant = conversation.Participants.First(p => p.Profile.Id == request.Actor.Id);
        if (actorParticipant.Role == ParticipantRole.Member && request.Actor.Id != request.Participant.Id)
            throw new ChatValidationException(ChatValidationError.NotAuthorized);

        // Find participant to remove
        var participantToRemove = conversation.Participants.FirstOrDefault(p => p.Profile.Id == request.Participant.Id && !p.HasLeft);
        if (participantToRemove == null)
            throw new ChatValidationException(ChatValidationError.ParticipantNotInConversation);

        // Cannot remove owner
        if (participantToRemove.Role == ParticipantRole.Owner)
            throw new ChatValidationException(ChatValidationError.CannotRemoveOwner);

        // Mark as left
        var now = DateTimeOffset.UtcNow;
        participantToRemove.HasLeft = true;
        participantToRemove.LeftAt = now;
        await _conversationStore.UpdateParticipantAsync(
            request.TenantId, request.ConversationId, participantToRemove, ct);

        // Create system message
        var messageType = request.Actor.Id == request.Participant.Id
            ? SystemMessageType.ParticipantLeft
            : SystemMessageType.ParticipantRemoved;

        await CreateSystemMessageAsync(
            request.TenantId,
            request.ConversationId,
            request.Actor,
            messageType,
            request.Actor.Id == request.Participant.Id
                ? "left the conversation"
                : $"removed {request.Participant.DisplayName ?? request.Participant.Id}",
            ct);

        conversation.UpdatedAt = now;
        await _conversationStore.UpsertAsync(conversation, ct);
        await _notifier.NotifyConversationUpdatedAsync(conversation, ct);
    }

    public async Task LeaveConversationAsync(
        string tenantId,
        string conversationId,
        EntityRefDto participant,
        CancellationToken ct = default)
    {
        await RemoveParticipantAsync(new RemoveParticipantRequest
        {
            TenantId = tenantId,
            ConversationId = conversationId,
            Actor = participant,
            Participant = participant
        }, ct);
    }

    public async Task SetArchivedAsync(
        string tenantId,
        string conversationId,
        EntityRefDto participant,
        bool archived,
        CancellationToken ct = default)
    {
        ChatValidator.ValidateTenantId(tenantId);
        ChatValidator.ValidateConversationId(conversationId);
        ChatValidator.ValidateProfile(participant);

        await GetAndVerifyParticipantAsync(tenantId, conversationId, participant, ct);

        await _conversationStore.SetUserSettingsAsync(
            tenantId, conversationId, participant.Id, isArchived: archived, isMuted: null, ct);
    }

    public async Task SetMutedAsync(
        string tenantId,
        string conversationId,
        EntityRefDto participant,
        bool muted,
        CancellationToken ct = default)
    {
        ChatValidator.ValidateTenantId(tenantId);
        ChatValidator.ValidateConversationId(conversationId);
        ChatValidator.ValidateProfile(participant);

        await GetAndVerifyParticipantAsync(tenantId, conversationId, participant, ct);

        await _conversationStore.SetUserSettingsAsync(
            tenantId, conversationId, participant.Id, isArchived: null, isMuted: muted, ct);
    }

    // ─────────────────────────────────────────────────────────────────
    // Messages
    // ─────────────────────────────────────────────────────────────────

    public async Task<MessageDto> SendMessageAsync(
        SendMessageRequest request,
        CancellationToken ct = default)
    {
        ChatValidator.ValidateSendMessage(request, _options);

        // Verify sender is participant
        var conversation = await GetAndVerifyParticipantAsync(
            request.TenantId, request.ConversationId, request.Sender, ct);

        var now = DateTimeOffset.UtcNow;
        var message = new MessageDto
        {
            Id = _idGenerator.NewId(),
            TenantId = request.TenantId,
            ConversationId = request.ConversationId,
            Sender = request.Sender,
            Body = ChatNormalizer.NormalizeBody(request.Body),
            Media = request.Media,
            ReplyToMessageId = request.ReplyToMessageId,
            CreatedAt = now
        };

        // Validate reply-to if specified
        if (request.ReplyToMessageId != null)
        {
            var replyTo = await _messageStore.GetByIdAsync(
                request.TenantId, request.ConversationId, request.ReplyToMessageId, ct);
            if (replyTo == null)
                throw new ChatValidationException(ChatValidationError.InvalidReplyToMessage);
        }

        // Save message
        message = await _messageStore.UpsertAsync(message, ct);

        // Update conversation's UpdatedAt and LastMessage
        conversation.UpdatedAt = now;
        conversation.LastMessage = message;
        await _conversationStore.UpsertAsync(conversation, ct);

        // Mark as read for sender
        await MarkReadInternalAsync(
            request.TenantId,
            request.ConversationId,
            request.Sender,
            message.Id!,
            now,
            ct);

        // Notify other participants
        await _notifier.NotifyMessageReceivedAsync(conversation, message, ct);

        return message;
    }

    public async Task<MessageDto> EditMessageAsync(
        EditMessageRequest request,
        CancellationToken ct = default)
    {
        ChatValidator.ValidateEditMessage(request, _options);

        // Verify actor is participant
        await GetAndVerifyParticipantAsync(
            request.TenantId, request.ConversationId, request.Actor, ct);

        // Get the message
        var message = await _messageStore.GetByIdAsync(
            request.TenantId, request.ConversationId, request.MessageId, ct);
        if (message == null)
            throw new ChatValidationException(ChatValidationError.MessageNotFound);

        // Verify actor is the sender
        if (message.Sender.Id != request.Actor.Id)
            throw new ChatValidationException(ChatValidationError.CannotEditOthersMessage);

        // Check edit window
        if (_options.EditWindowDuration > TimeSpan.Zero)
        {
            var elapsed = DateTimeOffset.UtcNow - message.CreatedAt;
            if (elapsed > _options.EditWindowDuration)
                throw new ChatValidationException(ChatValidationError.EditWindowExpired);
        }

        // Update message
        var now = DateTimeOffset.UtcNow;
        message.Body = ChatNormalizer.NormalizeBody(request.Body);
        message.EditedAt = now;

        message = await _messageStore.UpsertAsync(message, ct);

        // Notify
        var conversation = await _conversationStore.GetByIdAsync(
            request.TenantId, request.ConversationId, ct);
        if (conversation != null)
        {
            await _notifier.NotifyMessageEditedAsync(conversation, message, ct);
        }

        return message;
    }

    public async Task DeleteMessageAsync(
        DeleteMessageRequest request,
        CancellationToken ct = default)
    {
        ChatValidator.ValidateDeleteMessage(request);

        // Verify actor is participant
        await GetAndVerifyParticipantAsync(
            request.TenantId, request.ConversationId, request.Actor, ct);

        // Get the message
        var message = await _messageStore.GetByIdAsync(
            request.TenantId, request.ConversationId, request.MessageId, ct);
        if (message == null)
            throw new ChatValidationException(ChatValidationError.MessageNotFound);

        if (request.DeleteForEveryone)
        {
            // Only sender can delete for everyone
            if (message.Sender.Id != request.Actor.Id)
                throw new ChatValidationException(ChatValidationError.CannotDeleteForEveryoneOthersMessage);

            await _messageStore.HardDeleteAsync(
                request.TenantId, request.ConversationId, request.MessageId, ct);
        }
        else
        {
            // Soft delete for this user
            await _messageStore.SoftDeleteAsync(
                request.TenantId, request.ConversationId, request.MessageId, request.Actor.Id, ct);
        }

        await _notifier.NotifyMessageDeletedAsync(
            request.TenantId, request.ConversationId, request.MessageId, ct);
    }

    public async Task<ChatPageResult<MessageDto>> GetMessagesAsync(
        MessageQuery query,
        CancellationToken ct = default)
    {
        ChatValidator.ValidateMessageQuery(query);

        // Verify viewer is participant
        await GetAndVerifyParticipantAsync(
            query.TenantId, query.ConversationId, query.Viewer, ct);

        query.Limit = Math.Clamp(query.Limit, 1, _options.MaxPageSize);

        var result = await _messageStore.QueryAsync(query, ct);

        // Populate ReplyTo for messages that have ReplyToMessageId
        foreach (var msg in result.Items.Where(m => m.ReplyToMessageId != null && m.ReplyTo == null))
        {
            msg.ReplyTo = await _messageStore.GetByIdAsync(
                query.TenantId, query.ConversationId, msg.ReplyToMessageId!, ct);
        }

        return result;
    }

    public async Task<MessageDto?> GetMessageAsync(
        string tenantId,
        string conversationId,
        string messageId,
        EntityRefDto viewer,
        CancellationToken ct = default)
    {
        ChatValidator.ValidateTenantId(tenantId);
        ChatValidator.ValidateConversationId(conversationId);
        ChatValidator.ValidateMessageId(messageId);
        ChatValidator.ValidateProfile(viewer);

        // Verify viewer is participant
        await GetAndVerifyParticipantAsync(tenantId, conversationId, viewer, ct);

        var message = await _messageStore.GetByIdAsync(tenantId, conversationId, messageId, ct);
        if (message == null)
            return null;

        // Check if deleted for this viewer
        if (message.IsDeleted || (message.DeletedByProfileIds?.Contains(viewer.Id) ?? false))
            return null;

        // Populate ReplyTo if needed
        if (message.ReplyToMessageId != null && message.ReplyTo == null)
        {
            message.ReplyTo = await _messageStore.GetByIdAsync(
                tenantId, conversationId, message.ReplyToMessageId, ct);
        }

        return message;
    }

    // ─────────────────────────────────────────────────────────────────
    // Read Receipts
    // ─────────────────────────────────────────────────────────────────

    public async Task MarkReadAsync(
        MarkReadRequest request,
        CancellationToken ct = default)
    {
        ChatValidator.ValidateMarkRead(request);

        // Verify profile is participant
        await GetAndVerifyParticipantAsync(
            request.TenantId, request.ConversationId, request.Profile, ct);

        // Verify message exists
        var message = await _messageStore.GetByIdAsync(
            request.TenantId, request.ConversationId, request.MessageId, ct);
        if (message == null)
            throw new ChatValidationException(ChatValidationError.MessageNotFound);

        var now = DateTimeOffset.UtcNow;
        await MarkReadInternalAsync(
            request.TenantId,
            request.ConversationId,
            request.Profile,
            request.MessageId,
            now,
            ct);

        // Notify
        var receipt = new ReadReceiptDto
        {
            Profile = request.Profile,
            LastReadMessageId = request.MessageId,
            ReadAt = now
        };
        await _notifier.NotifyReadReceiptAsync(request.TenantId, request.ConversationId, receipt, ct);
    }

    private async Task MarkReadInternalAsync(
        string tenantId,
        string conversationId,
        EntityRefDto profile,
        string messageId,
        DateTimeOffset readAt,
        CancellationToken ct)
    {
        var participant = await _conversationStore.GetParticipantAsync(
            tenantId, conversationId, profile.Id, ct);

        if (participant != null)
        {
            participant.LastReadMessageId = messageId;
            participant.LastReadAt = readAt;
            await _conversationStore.UpdateParticipantAsync(
                tenantId, conversationId, participant, ct);
        }
    }

    public async Task<int> GetTotalUnreadCountAsync(
        string tenantId,
        EntityRefDto participant,
        CancellationToken ct = default)
    {
        ChatValidator.ValidateTenantId(tenantId);
        ChatValidator.ValidateProfile(participant);

        var query = new ConversationQuery
        {
            TenantId = tenantId,
            Participant = participant,
            IncludeArchived = false,
            Limit = 1000 // Get all active conversations
        };

        var conversations = await _conversationStore.QueryAsync(query, ct);
        var totalUnread = 0;

        foreach (var conv in conversations.Items)
        {
            var p = conv.Participants.FirstOrDefault(x => x.Profile.Id == participant.Id);
            if (p != null && !p.HasLeft)
            {
                var unread = await _messageStore.CountMessagesAfterAsync(
                    tenantId,
                    conv.Id!,
                    p.LastReadMessageId,
                    participant.Id,
                    ct);
                totalUnread += unread;
            }
        }

        return totalUnread;
    }

    public async Task<IReadOnlyList<ReadReceiptDto>> GetReadReceiptsAsync(
        string tenantId,
        string conversationId,
        string messageId,
        CancellationToken ct = default)
    {
        ChatValidator.ValidateTenantId(tenantId);
        ChatValidator.ValidateConversationId(conversationId);
        ChatValidator.ValidateMessageId(messageId);

        var conversation = await _conversationStore.GetByIdAsync(tenantId, conversationId, ct);
        if (conversation == null)
            return [];

        var receipts = new List<ReadReceiptDto>();

        foreach (var p in conversation.Participants.Where(x => !x.HasLeft && x.LastReadMessageId != null))
        {
            // Check if this participant has read up to or past this message
            // For simplicity, we compare message IDs (ULIDs are chronologically sortable)
            if (string.Compare(p.LastReadMessageId, messageId, StringComparison.Ordinal) >= 0)
            {
                receipts.Add(new ReadReceiptDto
                {
                    Profile = p.Profile,
                    LastReadMessageId = p.LastReadMessageId!,
                    ReadAt = p.LastReadAt ?? DateTimeOffset.UtcNow
                });
            }
        }

        return receipts;
    }

    // ─────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────

    private async Task<ConversationDto> GetAndVerifyParticipantAsync(
        string tenantId,
        string conversationId,
        EntityRefDto profile,
        CancellationToken ct)
    {
        var conversation = await _conversationStore.GetByIdAsync(tenantId, conversationId, ct);
        if (conversation == null)
            throw new ChatValidationException(ChatValidationError.ConversationNotFound);

        var participant = conversation.Participants.FirstOrDefault(p => p.Profile.Id == profile.Id);
        if (participant == null || participant.HasLeft)
            throw new ChatValidationException(ChatValidationError.NotParticipant);

        return conversation;
    }

    private async Task<ConversationDto> PopulateViewerFieldsAsync(
        ConversationDto conversation,
        EntityRefDto viewer,
        CancellationToken ct)
    {
        // Get user settings
        var (isArchived, isMuted) = await _conversationStore.GetUserSettingsAsync(
            conversation.TenantId, conversation.Id!, viewer.Id, ct);

        conversation.IsArchived = isArchived;
        conversation.IsMuted = isMuted;

        // Calculate unread count
        var participant = conversation.Participants.FirstOrDefault(p => p.Profile.Id == viewer.Id);
        if (participant != null && !participant.HasLeft)
        {
            conversation.UnreadCount = await _messageStore.CountMessagesAfterAsync(
                conversation.TenantId,
                conversation.Id!,
                participant.LastReadMessageId,
                viewer.Id,
                ct);
        }

        // Get last message if not populated
        if (conversation.LastMessage == null)
        {
            conversation.LastMessage = await _messageStore.GetLatestMessageAsync(
                conversation.TenantId, conversation.Id!, ct);
        }

        return conversation;
    }

    private async Task CreateSystemMessageAsync(
        string tenantId,
        string conversationId,
        EntityRefDto actor,
        SystemMessageType systemMessageType,
        string body,
        CancellationToken ct)
    {
        var message = new MessageDto
        {
            Id = _idGenerator.NewId(),
            TenantId = tenantId,
            ConversationId = conversationId,
            Sender = actor,
            Body = body,
            SystemMessageType = systemMessageType,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _messageStore.UpsertAsync(message, ct);
    }
}
