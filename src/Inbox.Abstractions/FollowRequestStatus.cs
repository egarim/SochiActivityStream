namespace Inbox.Abstractions;

/// <summary>
/// Defines the status of a follow/subscribe request.
/// </summary>
public enum FollowRequestStatus
{
    /// <summary>
    /// Request is awaiting approval.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Request has been approved.
    /// </summary>
    Approved = 1,

    /// <summary>
    /// Request has been denied.
    /// </summary>
    Denied = 2,

    /// <summary>
    /// Request has been cancelled by the requester.
    /// </summary>
    Cancelled = 3
}
