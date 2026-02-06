namespace Inbox.Abstractions;

/// <summary>
/// Defines the type of inbox item.
/// </summary>
public enum InboxItemKind
{
    /// <summary>
    /// A notification about an activity or event.
    /// </summary>
    Notification = 0,

    /// <summary>
    /// A request requiring action (e.g., follow request approval).
    /// </summary>
    Request = 1
}
