using ActivityStream.Abstractions;
using Inbox.Abstractions;

namespace Inbox.Core;

/// <summary>
/// Static methods for normalizing inbox DTOs.
/// </summary>
public static class InboxNormalizer
{
    /// <summary>
    /// Normalizes a tenant ID (trim + lowercase).
    /// </summary>
    public static string NormalizeTenantId(string? tenantId)
    {
        return tenantId?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    /// <summary>
    /// Normalizes an inbox item in-place.
    /// </summary>
    public static void Normalize(InboxItemDto item)
    {
        item.Title = item.Title?.Trim();
        item.Body = item.Body?.Trim();
        item.DedupKey = item.DedupKey?.Trim();
        item.ThreadKey = item.ThreadKey?.Trim();

        NormalizeEntityRef(item.Recipient);
        
        foreach (var target in item.Targets)
        {
            NormalizeEntityRef(target);
        }
    }

    /// <summary>
    /// Normalizes a follow request in-place.
    /// </summary>
    public static void Normalize(FollowRequestDto request)
    {
        request.DecisionReason = request.DecisionReason?.Trim();
        request.IdempotencyKey = request.IdempotencyKey?.Trim();

        NormalizeEntityRef(request.Requester);
        NormalizeEntityRef(request.Target);

        if (request.DecidedBy is not null)
        {
            NormalizeEntityRef(request.DecidedBy);
        }
    }

    /// <summary>
    /// Normalizes an entity reference in-place.
    /// </summary>
    public static void NormalizeEntityRef(EntityRefDto entity)
    {
        entity.Kind = entity.Kind?.Trim() ?? string.Empty;
        entity.Type = entity.Type?.Trim() ?? string.Empty;
        entity.Id = entity.Id?.Trim() ?? string.Empty;
        entity.Display = entity.Display?.Trim();
    }

    /// <summary>
    /// Normalizes an inbox event reference in-place.
    /// </summary>
    public static void NormalizeEventRef(InboxEventRefDto eventRef)
    {
        eventRef.Kind = eventRef.Kind?.Trim() ?? string.Empty;
        eventRef.Id = eventRef.Id?.Trim() ?? string.Empty;
        eventRef.TypeKey = eventRef.TypeKey?.Trim();
    }
}
