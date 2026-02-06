using ActivityStream.Abstractions;
using RelationshipService.Abstractions;

namespace RelationshipService.Core;

/// <summary>
/// Normalizes relationship edge DTOs before validation and persistence.
/// </summary>
public static class RelationshipEdgeNormalizer
{
    /// <summary>
    /// Normalizes an edge in-place by trimming strings, ensuring lists are not null,
    /// and removing empty entries from filter lists.
    /// </summary>
    public static void Normalize(RelationshipEdgeDto edge, IIdGenerator idGenerator)
    {
        // Trim TenantId
        edge.TenantId = edge.TenantId?.Trim() ?? string.Empty;

        // Normalize From entity
        if (edge.From is not null)
        {
            NormalizeEntityRef(edge.From);
        }

        // Normalize To entity
        if (edge.To is not null)
        {
            NormalizeEntityRef(edge.To);
        }

        // Generate Id if missing
        if (string.IsNullOrWhiteSpace(edge.Id))
        {
            edge.Id = idGenerator.NewId();
        }
        else
        {
            edge.Id = edge.Id.Trim();
        }

        // Set CreatedAt if not set
        if (edge.CreatedAt == default)
        {
            edge.CreatedAt = DateTimeOffset.UtcNow;
        }

        // Normalize filter if present
        if (edge.Filter is not null)
        {
            NormalizeFilter(edge.Filter);
        }
    }

    private static void NormalizeEntityRef(EntityRefDto entity)
    {
        entity.Kind = entity.Kind?.Trim() ?? string.Empty;
        entity.Type = entity.Type?.Trim() ?? string.Empty;
        entity.Id = entity.Id?.Trim() ?? string.Empty;
        entity.Display = entity.Display?.Trim();
    }

    private static void NormalizeFilter(RelationshipFilterDto filter)
    {
        // Normalize and clean TypeKeys
        if (filter.TypeKeys is not null)
        {
            filter.TypeKeys = filter.TypeKeys
                .Where(tk => !string.IsNullOrWhiteSpace(tk))
                .Select(tk => tk.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // Normalize and clean TypeKeyPrefixes
        if (filter.TypeKeyPrefixes is not null)
        {
            filter.TypeKeyPrefixes = filter.TypeKeyPrefixes
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // Normalize and clean RequiredTagsAny
        if (filter.RequiredTagsAny is not null)
        {
            filter.RequiredTagsAny = filter.RequiredTagsAny
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // Normalize and clean ExcludedTagsAny
        if (filter.ExcludedTagsAny is not null)
        {
            filter.ExcludedTagsAny = filter.ExcludedTagsAny
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
