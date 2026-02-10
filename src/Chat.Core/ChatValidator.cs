using Chat.Abstractions;

namespace Chat.Core;

/// <summary>
/// Request validation for chat operations.
/// </summary>
public static class ChatValidator
{
    public static void ValidateTenantId(string? tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ChatValidationException(ChatValidationError.TenantIdRequired);
    }

    public static void ValidateConversationId(string? conversationId)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
            throw new ChatValidationException(ChatValidationError.ConversationIdRequired);
    }

    public static void ValidateMessageId(string? messageId)
    {
        if (string.IsNullOrWhiteSpace(messageId))
            throw new ChatValidationException(ChatValidationError.MessageIdRequired);
    }

    public static void ValidateSender(ActivityStream.Abstractions.EntityRefDto? sender)
    {
        if (sender == null || string.IsNullOrWhiteSpace(sender.Id))
            throw new ChatValidationException(ChatValidationError.SenderRequired);
    }

    public static void ValidateActor(ActivityStream.Abstractions.EntityRefDto? actor)
    {
        if (actor == null || string.IsNullOrWhiteSpace(actor.Id))
            throw new ChatValidationException(ChatValidationError.ActorRequired);
    }

    public static void ValidateProfile(ActivityStream.Abstractions.EntityRefDto? profile)
    {
        if (profile == null || string.IsNullOrWhiteSpace(profile.Id))
            throw new ChatValidationException(ChatValidationError.ProfileRequired);
    }

    public static void ValidateParticipant(ActivityStream.Abstractions.EntityRefDto? participant)
    {
        if (participant == null || string.IsNullOrWhiteSpace(participant.Id))
            throw new ChatValidationException(ChatValidationError.ParticipantRequired);
    }

    public static void ValidateNewParticipant(ActivityStream.Abstractions.EntityRefDto? participant)
    {
        if (participant == null || string.IsNullOrWhiteSpace(participant.Id))
            throw new ChatValidationException(ChatValidationError.NewParticipantRequired);
    }

    public static void ValidateBody(string? body, ChatServiceOptions options)
    {
        if (string.IsNullOrWhiteSpace(body))
            throw new ChatValidationException(ChatValidationError.BodyRequired);

        if (body.Length > options.MaxMessageBodyLength)
            throw new ChatValidationException(ChatValidationError.BodyTooLong);
    }

    public static void ValidateTitle(string? title, ChatServiceOptions options)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ChatValidationException(ChatValidationError.TitleRequired);

        if (title.Length > options.MaxTitleLength)
            throw new ChatValidationException(ChatValidationError.TitleTooLong);
    }

    public static void ValidateParticipants(List<ActivityStream.Abstractions.EntityRefDto>? participants, ChatServiceOptions options)
    {
        if (participants == null || participants.Count == 0)
            throw new ChatValidationException(ChatValidationError.ParticipantsRequired);

        if (participants.Count < options.MinGroupParticipants)
            throw new ChatValidationException(ChatValidationError.TooFewParticipants);

        if (participants.Count > options.MaxGroupParticipants)
            throw new ChatValidationException(ChatValidationError.TooManyParticipants);
    }

    public static void ValidateSendMessage(SendMessageRequest request, ChatServiceOptions options)
    {
        ValidateTenantId(request.TenantId);
        ValidateConversationId(request.ConversationId);
        ValidateSender(request.Sender);
        ValidateBody(request.Body, options);
    }

    public static void ValidateEditMessage(EditMessageRequest request, ChatServiceOptions options)
    {
        ValidateTenantId(request.TenantId);
        ValidateConversationId(request.ConversationId);
        ValidateMessageId(request.MessageId);
        ValidateActor(request.Actor);
        ValidateBody(request.Body, options);
    }

    public static void ValidateDeleteMessage(DeleteMessageRequest request)
    {
        ValidateTenantId(request.TenantId);
        ValidateConversationId(request.ConversationId);
        ValidateMessageId(request.MessageId);
        ValidateActor(request.Actor);
    }

    public static void ValidateCreateGroupConversation(CreateConversationRequest request, ChatServiceOptions options)
    {
        ValidateTenantId(request.TenantId);
        ValidateSender(request.Creator);
        ValidateTitle(request.Title, options);
        ValidateParticipants(request.Participants, options);
    }

    public static void ValidateUpdateConversation(UpdateConversationRequest request, ChatServiceOptions options)
    {
        ValidateTenantId(request.TenantId);
        ValidateConversationId(request.ConversationId);
        ValidateActor(request.Actor);

        if (request.Title != null && request.Title.Length > options.MaxTitleLength)
            throw new ChatValidationException(ChatValidationError.TitleTooLong);
    }

    public static void ValidateAddParticipant(AddParticipantRequest request)
    {
        ValidateTenantId(request.TenantId);
        ValidateConversationId(request.ConversationId);
        ValidateActor(request.Actor);
        ValidateNewParticipant(request.NewParticipant);
    }

    public static void ValidateRemoveParticipant(RemoveParticipantRequest request)
    {
        ValidateTenantId(request.TenantId);
        ValidateConversationId(request.ConversationId);
        ValidateActor(request.Actor);
        ValidateParticipant(request.Participant);
    }

    public static void ValidateMarkRead(MarkReadRequest request)
    {
        ValidateTenantId(request.TenantId);
        ValidateConversationId(request.ConversationId);
        ValidateProfile(request.Profile);
        ValidateMessageId(request.MessageId);
    }

    public static void ValidateMessageQuery(MessageQuery query)
    {
        ValidateTenantId(query.TenantId);
        ValidateConversationId(query.ConversationId);
        ValidateProfile(query.Viewer);
    }

    public static void ValidateConversationQuery(ConversationQuery query)
    {
        ValidateTenantId(query.TenantId);
        ValidateProfile(query.Participant);
    }
}
