namespace Realtime.Abstractions;

/// <summary>
/// Standard event type constants.
/// </summary>
public static class RealtimeEventTypes
{
    // Activity events
    public const string ActivityCreated = "activity.created";

    // Inbox events
    public const string InboxItemCreated = "inbox.item.created";
    public const string InboxItemUpdated = "inbox.item.updated";
    public const string InboxItemRead = "inbox.item.read";

    // Content events
    public const string PostCreated = "post.created";
    public const string PostUpdated = "post.updated";
    public const string PostDeleted = "post.deleted";
    public const string CommentCreated = "comment.created";
    public const string CommentUpdated = "comment.updated";
    public const string CommentDeleted = "comment.deleted";
    public const string ReactionAdded = "reaction.added";
    public const string ReactionRemoved = "reaction.removed";

    // Chat events
    public const string MessageReceived = "message.received";
    public const string MessageEdited = "message.edited";
    public const string MessageDeleted = "message.deleted";
    public const string ConversationCreated = "conversation.created";

    // Presence events
    public const string PresenceOnline = "presence.online";
    public const string PresenceOffline = "presence.offline";
    public const string PresenceAway = "presence.away";

    // Typing events
    public const string TypingStarted = "typing.started";
    public const string TypingStopped = "typing.stopped";
}
