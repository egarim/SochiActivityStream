namespace ActivityStream.Abstractions;

/// <summary>
/// Visibility level for an activity.
/// </summary>
public enum ActivityVisibility
{
    /// <summary>
    /// Visible to everyone.
    /// </summary>
    Public = 0,

    /// <summary>
    /// Visible only within the organization/tenant.
    /// </summary>
    Internal = 1,

    /// <summary>
    /// Visible only to specific users/roles.
    /// </summary>
    Private = 2
}
