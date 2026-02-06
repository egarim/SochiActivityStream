using ActivityStream.Abstractions;
using RelationshipService.Abstractions;

namespace RelationshipService.Core;

/// <summary>
/// Validates RelationshipEdgeDto instances.
/// </summary>
public static class RelationshipEdgeValidator
{
    private const int MaxTypeKeyLength = 200;
    private const int MaxTagLength = 100;

    /// <summary>
    /// Validates a relationship edge and returns any validation errors.
    /// </summary>
    public static IReadOnlyList<RelationshipValidationError> Validate(RelationshipEdgeDto edge)
    {
        var errors = new List<RelationshipValidationError>();

        // TenantId required
        if (string.IsNullOrWhiteSpace(edge.TenantId))
        {
            errors.Add(new RelationshipValidationError("TenantIdRequired", "TenantId is required.", "TenantId"));
        }

        // From required with valid fields
        if (edge.From is null)
        {
            errors.Add(new RelationshipValidationError("FromRequired", "From entity is required.", "From"));
        }
        else
        {
            ValidateEntityRef(edge.From, "From", errors);
        }

        // To required with valid fields
        if (edge.To is null)
        {
            errors.Add(new RelationshipValidationError("ToRequired", "To entity is required.", "To"));
        }
        else
        {
            ValidateEntityRef(edge.To, "To", errors);
        }

        // Kind must be defined enum value
        if (!Enum.IsDefined(typeof(RelationshipKind), edge.Kind))
        {
            errors.Add(new RelationshipValidationError("InvalidKind", "Kind is not a valid RelationshipKind value.", "Kind"));
        }

        // Scope must be defined enum value
        if (!Enum.IsDefined(typeof(RelationshipScope), edge.Scope))
        {
            errors.Add(new RelationshipValidationError("InvalidScope", "Scope is not a valid RelationshipScope value.", "Scope"));
        }

        // Validate filter if present
        if (edge.Filter is not null)
        {
            ValidateFilter(edge.Filter, errors);
        }

        return errors;
    }

    private static void ValidateEntityRef(EntityRefDto entity, string path, List<RelationshipValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(entity.Kind))
        {
            errors.Add(new RelationshipValidationError("EntityKindRequired", $"{path}.Kind is required.", $"{path}.Kind"));
        }

        if (string.IsNullOrWhiteSpace(entity.Type))
        {
            errors.Add(new RelationshipValidationError("EntityTypeRequired", $"{path}.Type is required.", $"{path}.Type"));
        }

        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            errors.Add(new RelationshipValidationError("EntityIdRequired", $"{path}.Id is required.", $"{path}.Id"));
        }
    }

    private static void ValidateFilter(RelationshipFilterDto filter, List<RelationshipValidationError> errors)
    {
        // Validate TypeKeys length
        if (filter.TypeKeys is not null)
        {
            for (int i = 0; i < filter.TypeKeys.Count; i++)
            {
                var tk = filter.TypeKeys[i];
                if (tk != null && tk.Length > MaxTypeKeyLength)
                {
                    errors.Add(new RelationshipValidationError(
                        "TypeKeyTooLong",
                        $"Filter.TypeKeys[{i}] exceeds maximum length of {MaxTypeKeyLength}.",
                        $"Filter.TypeKeys[{i}]"));
                }
            }
        }

        // Validate TypeKeyPrefixes length
        if (filter.TypeKeyPrefixes is not null)
        {
            for (int i = 0; i < filter.TypeKeyPrefixes.Count; i++)
            {
                var prefix = filter.TypeKeyPrefixes[i];
                if (prefix != null && prefix.Length > MaxTypeKeyLength)
                {
                    errors.Add(new RelationshipValidationError(
                        "TypeKeyPrefixTooLong",
                        $"Filter.TypeKeyPrefixes[{i}] exceeds maximum length of {MaxTypeKeyLength}.",
                        $"Filter.TypeKeyPrefixes[{i}]"));
                }
            }
        }

        // Validate RequiredTagsAny length
        if (filter.RequiredTagsAny is not null)
        {
            for (int i = 0; i < filter.RequiredTagsAny.Count; i++)
            {
                var tag = filter.RequiredTagsAny[i];
                if (tag != null && tag.Length > MaxTagLength)
                {
                    errors.Add(new RelationshipValidationError(
                        "RequiredTagTooLong",
                        $"Filter.RequiredTagsAny[{i}] exceeds maximum length of {MaxTagLength}.",
                        $"Filter.RequiredTagsAny[{i}]"));
                }
            }
        }

        // Validate ExcludedTagsAny length
        if (filter.ExcludedTagsAny is not null)
        {
            for (int i = 0; i < filter.ExcludedTagsAny.Count; i++)
            {
                var tag = filter.ExcludedTagsAny[i];
                if (tag != null && tag.Length > MaxTagLength)
                {
                    errors.Add(new RelationshipValidationError(
                        "ExcludedTagTooLong",
                        $"Filter.ExcludedTagsAny[{i}] exceeds maximum length of {MaxTagLength}.",
                        $"Filter.ExcludedTagsAny[{i}]"));
                }
            }
        }
    }
}
