using ActivityStream.Abstractions;
using Inbox.Abstractions;

namespace Inbox.Core;

/// <summary>
/// Static methods for validating inbox DTOs.
/// </summary>
public static class InboxValidator
{
    /// <summary>Maximum title length.</summary>
    public const int MaxTitleLength = 500;

    /// <summary>Maximum body length.</summary>
    public const int MaxBodyLength = 2000;

    /// <summary>Maximum tenant ID length.</summary>
    public const int MaxTenantIdLength = 100;

    /// <summary>Maximum targets per inbox item.</summary>
    public const int MaxTargets = 50;

    /// <summary>Maximum limit for queries.</summary>
    public const int MaxQueryLimit = 200;

    /// <summary>
    /// Validates an inbox item.
    /// </summary>
    public static IReadOnlyList<InboxValidationError> ValidateInboxItem(InboxItemDto item)
    {
        var errors = new List<InboxValidationError>();

        ValidateTenantId(item.TenantId, errors);
        ValidateEntityRef(item.Recipient, "Recipient", errors);
        ValidateEventRef(item.Event, errors);

        if (item.Title is not null && item.Title.Length > MaxTitleLength)
            errors.Add(new("MAX_LENGTH", $"Title exceeds {MaxTitleLength} characters.", "Title"));

        if (item.Body is not null && item.Body.Length > MaxBodyLength)
            errors.Add(new("MAX_LENGTH", $"Body exceeds {MaxBodyLength} characters.", "Body"));

        if (item.Targets.Count > MaxTargets)
            errors.Add(new("MAX_COUNT", $"Targets exceeds {MaxTargets} items.", "Targets"));

        for (var i = 0; i < item.Targets.Count; i++)
        {
            ValidateEntityRef(item.Targets[i], $"Targets[{i}]", errors);
        }

        if (item.ThreadCount < 1)
            errors.Add(new("INVALID_VALUE", "ThreadCount must be at least 1.", "ThreadCount"));

        return errors;
    }

    /// <summary>
    /// Validates a follow request.
    /// </summary>
    public static IReadOnlyList<InboxValidationError> ValidateFollowRequest(FollowRequestDto request)
    {
        var errors = new List<InboxValidationError>();

        ValidateTenantId(request.TenantId, errors);
        ValidateEntityRef(request.Requester, "Requester", errors);
        ValidateEntityRef(request.Target, "Target", errors);

        // RequestedKind must be Follow or Subscribe
        if (request.RequestedKind != RelationshipService.Abstractions.RelationshipKind.Follow &&
            request.RequestedKind != RelationshipService.Abstractions.RelationshipKind.Subscribe)
        {
            errors.Add(new("INVALID_VALUE", "RequestedKind must be Follow or Subscribe.", "RequestedKind"));
        }

        return errors;
    }

    /// <summary>
    /// Validates an inbox query.
    /// </summary>
    public static IReadOnlyList<InboxValidationError> ValidateQuery(InboxQuery query)
    {
        var errors = new List<InboxValidationError>();

        ValidateTenantId(query.TenantId, errors);

        if (query.Limit < 1)
            errors.Add(new("INVALID_VALUE", "Limit must be at least 1.", "Limit"));

        if (query.Limit > MaxQueryLimit)
            errors.Add(new("MAX_VALUE", $"Limit exceeds maximum of {MaxQueryLimit}.", "Limit"));

        for (var i = 0; i < query.Recipients.Count; i++)
        {
            ValidateEntityRef(query.Recipients[i], $"Recipients[{i}]", errors);
        }

        return errors;
    }

    private static void ValidateTenantId(string? tenantId, List<InboxValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            errors.Add(new("REQUIRED", "TenantId is required.", "TenantId"));
        else if (tenantId.Length > MaxTenantIdLength)
            errors.Add(new("MAX_LENGTH", $"TenantId exceeds {MaxTenantIdLength} characters.", "TenantId"));
    }

    private static void ValidateEntityRef(EntityRefDto? entity, string path, List<InboxValidationError> errors)
    {
        if (entity is null)
        {
            errors.Add(new("REQUIRED", $"{path} is required.", path));
            return;
        }

        if (string.IsNullOrWhiteSpace(entity.Kind))
            errors.Add(new("REQUIRED", $"{path}.Kind is required.", $"{path}.Kind"));

        if (string.IsNullOrWhiteSpace(entity.Type))
            errors.Add(new("REQUIRED", $"{path}.Type is required.", $"{path}.Type"));

        if (string.IsNullOrWhiteSpace(entity.Id))
            errors.Add(new("REQUIRED", $"{path}.Id is required.", $"{path}.Id"));
    }

    private static void ValidateEventRef(InboxEventRefDto? eventRef, List<InboxValidationError> errors)
    {
        if (eventRef is null)
        {
            errors.Add(new("REQUIRED", "Event is required.", "Event"));
            return;
        }

        if (string.IsNullOrWhiteSpace(eventRef.Kind))
            errors.Add(new("REQUIRED", "Event.Kind is required.", "Event.Kind"));

        if (string.IsNullOrWhiteSpace(eventRef.Id))
            errors.Add(new("REQUIRED", "Event.Id is required.", "Event.Id"));
    }
}
