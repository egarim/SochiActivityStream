using ActivityStream.Abstractions;

namespace Inbox.Abstractions;

/// <summary>
/// Exception thrown when an activity violates inbox governance policy
/// (e.g., targeting a private entity that cannot be targeted).
/// </summary>
public class InboxPolicyViolationException : Exception
{
    /// <summary>
    /// The entity that violated the policy.
    /// </summary>
    public EntityRefDto Entity { get; }

    /// <summary>
    /// The reason for the violation.
    /// </summary>
    public string Reason { get; }

    public InboxPolicyViolationException(EntityRefDto entity, string reason)
        : base($"Policy violation for entity {entity.Type}/{entity.Id}: {reason}")
    {
        Entity = entity;
        Reason = reason;
    }
}
