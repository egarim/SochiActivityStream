using ActivityStream.Abstractions;

namespace RelationshipService.Core;

/// <summary>
/// Provides equality comparison for EntityRefDto using case-insensitive matching
/// on Kind, Type, and Id after trimming.
/// </summary>
public static class EntityRefEqualityHelper
{
    /// <summary>
    /// Determines if two EntityRefDto are equal based on Kind, Type, and Id.
    /// Comparison is case-insensitive (OrdinalIgnoreCase) after trimming.
    /// </summary>
    public static bool AreEqual(EntityRefDto? a, EntityRefDto? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;

        return string.Equals(a.Kind?.Trim(), b.Kind?.Trim(), StringComparison.OrdinalIgnoreCase)
            && string.Equals(a.Type?.Trim(), b.Type?.Trim(), StringComparison.OrdinalIgnoreCase)
            && string.Equals(a.Id?.Trim(), b.Id?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Creates a normalized key for an EntityRefDto suitable for dictionary lookups.
    /// Format: "{kind}|{type}|{id}" all lowercase and trimmed.
    /// </summary>
    public static string ToKey(EntityRefDto entityRef)
    {
        var kind = (entityRef.Kind?.Trim() ?? string.Empty).ToLowerInvariant();
        var type = (entityRef.Type?.Trim() ?? string.Empty).ToLowerInvariant();
        var id = (entityRef.Id?.Trim() ?? string.Empty).ToLowerInvariant();
        return $"{kind}|{type}|{id}";
    }
}
