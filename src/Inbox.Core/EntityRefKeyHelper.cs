using ActivityStream.Abstractions;

namespace Inbox.Core;

/// <summary>
/// Helper for generating normalized keys from EntityRefDto instances.
/// Used for dedup keys, thread keys, and store indexes.
/// </summary>
public static class EntityRefKeyHelper
{
    /// <summary>
    /// Generates a normalized key string from an EntityRefDto.
    /// Format: "{kind}|{type}|{id}" (all lowercase, trimmed).
    /// </summary>
    public static string ToKey(EntityRefDto entity)
    {
        var kind = entity.Kind?.Trim().ToLowerInvariant() ?? string.Empty;
        var type = entity.Type?.Trim().ToLowerInvariant() ?? string.Empty;
        var id = entity.Id?.Trim().ToLowerInvariant() ?? string.Empty;
        return $"{kind}|{type}|{id}";
    }

    /// <summary>
    /// Checks if two EntityRefDto instances are equal using normalized comparison.
    /// </summary>
    public static bool AreEqual(EntityRefDto? a, EntityRefDto? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        return ToKey(a) == ToKey(b);
    }

    /// <summary>
    /// Generates a dedup key for an activity notification.
    /// Format: "activity:{activityId}:recipient:{recipientKey}"
    /// </summary>
    public static string BuildActivityDedupKey(string activityId, EntityRefDto recipient)
    {
        return $"activity:{activityId}:recipient:{ToKey(recipient)}";
    }

    /// <summary>
    /// Generates a thread key for grouping notifications.
    /// Format: "target:{Type}:{Id}:type:{prefix}" or "actor:{Type}:{Id}:type:{prefix}"
    /// </summary>
    public static string BuildThreadKey(EntityRefDto? target, EntityRefDto actor, string typeKey)
    {
        var prefix = GetTypeKeyPrefix(typeKey);
        
        if (target is not null)
        {
            var type = target.Type?.Trim() ?? string.Empty;
            var id = target.Id?.Trim() ?? string.Empty;
            return $"target:{type}:{id}:type:{prefix}";
        }
        else
        {
            var type = actor.Type?.Trim() ?? string.Empty;
            var id = actor.Id?.Trim() ?? string.Empty;
            return $"actor:{type}:{id}:type:{prefix}";
        }
    }

    /// <summary>
    /// Gets the prefix of a type key (up to first '.').
    /// </summary>
    public static string GetTypeKeyPrefix(string? typeKey)
    {
        if (string.IsNullOrEmpty(typeKey))
            return string.Empty;

        var dotIndex = typeKey.IndexOf('.');
        return dotIndex > 0 ? typeKey[..dotIndex] : typeKey;
    }
}
