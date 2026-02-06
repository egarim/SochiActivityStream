namespace Identity.Abstractions;

/// <summary>
/// Internal record for storing membership data.
/// </summary>
public sealed class MembershipRecord
{
    /// <summary>
    /// The membership data.
    /// </summary>
    public required MembershipDto Membership { get; set; }
}
