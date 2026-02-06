namespace Inbox.Abstractions;

/// <summary>
/// Defines the status of an inbox item.
/// </summary>
public enum InboxItemStatus
{
    /// <summary>
    /// Item has not been read.
    /// </summary>
    Unread = 0,

    /// <summary>
    /// Item has been read.
    /// </summary>
    Read = 1,

    /// <summary>
    /// Item has been archived.
    /// </summary>
    Archived = 2
}
