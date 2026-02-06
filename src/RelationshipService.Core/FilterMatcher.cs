using ActivityStream.Abstractions;
using RelationshipService.Abstractions;

namespace RelationshipService.Core;

/// <summary>
/// Evaluates whether a RelationshipFilterDto matches an ActivityDto.
/// </summary>
public static class FilterMatcher
{
    /// <summary>
    /// Determines if the filter matches the activity.
    /// A null filter matches everything.
    /// </summary>
    public static bool Matches(RelationshipFilterDto? filter, ActivityDto activity)
    {
        if (filter is null)
        {
            return true;
        }

        // TypeKeys exact match (case-insensitive)
        if (filter.TypeKeys is { Count: > 0 })
        {
            var typeKeyMatches = filter.TypeKeys.Any(tk =>
                string.Equals(tk?.Trim(), activity.TypeKey?.Trim(), StringComparison.OrdinalIgnoreCase));

            if (!typeKeyMatches && filter.TypeKeyPrefixes is not { Count: > 0 })
            {
                // If TypeKeys are specified and none match, and no prefixes to try, fail
                return false;
            }

            if (typeKeyMatches)
            {
                // TypeKey matched, continue to other checks
            }
            else
            {
                // Check prefixes if TypeKeys didn't match
                if (!MatchesPrefixes(filter.TypeKeyPrefixes, activity.TypeKey))
                {
                    return false;
                }
            }
        }
        else if (filter.TypeKeyPrefixes is { Count: > 0 })
        {
            // Only prefixes specified
            if (!MatchesPrefixes(filter.TypeKeyPrefixes, activity.TypeKey))
            {
                return false;
            }
        }

        // RequiredTagsAny - at least one must be present
        if (filter.RequiredTagsAny is { Count: > 0 })
        {
            var activityTags = activity.Tags?.Select(t => t?.Trim().ToLowerInvariant()).ToHashSet() 
                ?? new HashSet<string?>();
            
            var hasRequiredTag = filter.RequiredTagsAny.Any(rt =>
                activityTags.Contains(rt?.Trim().ToLowerInvariant()));

            if (!hasRequiredTag)
            {
                return false;
            }
        }

        // ExcludedTagsAny - none must be present
        if (filter.ExcludedTagsAny is { Count: > 0 })
        {
            var activityTags = activity.Tags?.Select(t => t?.Trim().ToLowerInvariant()).ToHashSet() 
                ?? new HashSet<string?>();

            var hasExcludedTag = filter.ExcludedTagsAny.Any(et =>
                activityTags.Contains(et?.Trim().ToLowerInvariant()));

            if (hasExcludedTag)
            {
                return false;
            }
        }

        // AllowedVisibilities - activity visibility must be in list
        if (filter.AllowedVisibilities is { Count: > 0 })
        {
            if (!filter.AllowedVisibilities.Contains(activity.Visibility))
            {
                return false;
            }
        }

        return true;
    }

    private static bool MatchesPrefixes(List<string>? prefixes, string? typeKey)
    {
        if (prefixes is null || prefixes.Count == 0)
        {
            return true;
        }

        var normalizedTypeKey = typeKey?.Trim().ToLowerInvariant() ?? string.Empty;

        return prefixes.Any(prefix =>
            normalizedTypeKey.StartsWith(prefix?.Trim().ToLowerInvariant() ?? string.Empty, StringComparison.Ordinal));
    }
}
