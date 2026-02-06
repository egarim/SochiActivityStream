namespace Chat.Abstractions;

/// <summary>
/// Chat validation error codes.
/// </summary>
public enum ChatValidationError
{
    None,
    TenantIdRequired,
    ConversationIdRequired,
    MessageIdRequired,
    SenderRequired,
    BodyRequired,
    BodyTooLong,
    ParticipantsRequired,
    TooFewParticipants,
    TooManyParticipants,
    TitleRequired,
    TitleTooLong,
    NotParticipant,
    NotAuthorized,
    ConversationNotFound,
    MessageNotFound,
    CannotEditOthersMessage,
    CannotDeleteForEveryoneOthersMessage,
    DirectConversationCannotAddParticipants,
    DirectConversationCannotRemoveParticipants,
    CannotRemoveOwner,
    InvalidReplyToMessage,
    EditWindowExpired,
    ProfileRequired,
    ActorRequired,
    ParticipantRequired,
    NewParticipantRequired,
    ParticipantAlreadyInConversation,
    ParticipantNotInConversation
}
