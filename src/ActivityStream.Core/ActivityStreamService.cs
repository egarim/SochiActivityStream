using ActivityStream.Abstractions;

namespace ActivityStream.Core;

/// <summary>
/// Core implementation of the activity stream service.
/// Handles normalization, validation, id generation, idempotency, and query pagination.
/// </summary>
public sealed class ActivityStreamService : IActivityStreamService
{
    private readonly IActivityStore _store;
    private readonly IIdGenerator _idGenerator;
    private readonly IActivityValidator _validator;

    /// <summary>
    /// Hard maximum for query limit.
    /// </summary>
    public const int MaxLimit = 200;

    /// <summary>
    /// Minimum for query limit.
    /// </summary>
    public const int MinLimit = 1;

    public ActivityStreamService(
        IActivityStore store,
        IIdGenerator idGenerator,
        IActivityValidator validator)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<ActivityDto> PublishAsync(ActivityDto activity, CancellationToken ct = default)
    {
        // Normalize
        Normalize(activity);

        // Validate
        var errors = _validator.Validate(activity);
        if (errors.Count > 0)
        {
            throw new ActivityValidationException(errors);
        }

        // Check idempotency
        if (!string.IsNullOrWhiteSpace(activity.Source?.System) &&
            !string.IsNullOrWhiteSpace(activity.Source?.IdempotencyKey))
        {
            var existing = await _store.FindByIdempotencyAsync(
                activity.TenantId,
                activity.Source.System,
                activity.Source.IdempotencyKey,
                ct);

            if (existing is not null)
            {
                return existing;
            }
        }

        // Generate Id if missing
        if (string.IsNullOrWhiteSpace(activity.Id))
        {
            activity.Id = _idGenerator.NewId();
        }

        // Set CreatedAt if default
        if (activity.CreatedAt == default)
        {
            activity.CreatedAt = DateTimeOffset.UtcNow;
        }

        // Append to store
        await _store.AppendAsync(activity, ct);

        return activity;
    }

    public async Task<IReadOnlyList<ActivityDto>> PublishBatchAsync(
        IReadOnlyList<ActivityDto> activities,
        CancellationToken ct = default)
    {
        var results = new List<ActivityDto>(activities.Count);

        foreach (var activity in activities)
        {
            var result = await PublishAsync(activity, ct);
            results.Add(result);
        }

        return results;
    }

    public async Task<ActivityPageResult> QueryAsync(ActivityQuery query, CancellationToken ct = default)
    {
        // Normalize query
        var limit = Math.Clamp(query.Limit, MinLimit, MaxLimit);

        // Request one extra item to determine if there's a next page
        var requestLimit = limit + 1;

        // Parse cursor if provided
        DateTimeOffset? cursorOccurredAt = null;
        string? cursorId = null;
        if (CursorHelper.TryDecode(query.Cursor, out var decodedOccurredAt, out var decodedId))
        {
            cursorOccurredAt = decodedOccurredAt;
            cursorId = decodedId;
        }

        // Query store with modified limit
        var modifiedQuery = new ActivityQuery
        {
            TenantId = query.TenantId,
            TypeKey = query.TypeKey,
            Actor = query.Actor,
            Target = query.Target,
            From = query.From,
            To = query.To,
            Limit = requestLimit,
            Cursor = query.Cursor
        };

        var allItems = await _store.QueryAsync(modifiedQuery, ct);

        // Apply cursor filtering (items strictly after cursor position)
        var filteredItems = cursorOccurredAt.HasValue
            ? ApplyCursor(allItems, cursorOccurredAt.Value, cursorId!)
            : allItems;

        // Apply ordering (OccurredAt desc, Id desc) - store should return sorted but ensure
        var orderedItems = filteredItems
            .OrderByDescending(a => a.OccurredAt)
            .ThenByDescending(a => a.Id, StringComparer.Ordinal)
            .ToList();

        // Check if there are more items
        var hasMore = orderedItems.Count > limit;
        var pageItems = hasMore
            ? orderedItems.Take(limit).ToList()
            : orderedItems;

        // Generate next cursor from last item
        string? nextCursor = null;
        if (hasMore && pageItems.Count > 0)
        {
            var lastItem = pageItems[^1];
            nextCursor = CursorHelper.Encode(lastItem.OccurredAt, lastItem.Id!);
        }

        return new ActivityPageResult
        {
            Items = pageItems,
            NextCursor = nextCursor
        };
    }

    private static IReadOnlyList<ActivityDto> ApplyCursor(
        IReadOnlyList<ActivityDto> items,
        DateTimeOffset cursorOccurredAt,
        string cursorId)
    {
        // For descending order, we want items "after" the cursor
        // which means items with (OccurredAt < cursor) OR (OccurredAt == cursor AND Id < cursor)
        return items.Where(a =>
            a.OccurredAt < cursorOccurredAt ||
            (a.OccurredAt == cursorOccurredAt && string.Compare(a.Id, cursorId, StringComparison.Ordinal) < 0))
            .ToList();
    }

    private void Normalize(ActivityDto activity)
    {
        // Trim strings
        activity.TenantId = activity.TenantId?.Trim() ?? string.Empty;
        activity.TypeKey = activity.TypeKey?.Trim() ?? string.Empty;
        activity.Summary = activity.Summary?.Trim();
        activity.Id = activity.Id?.Trim();

        // Normalize Actor
        if (activity.Actor is not null)
        {
            NormalizeEntityRef(activity.Actor);
        }

        // Normalize Owner
        if (activity.Owner is not null)
        {
            NormalizeEntityRef(activity.Owner);
        }

        // Normalize Targets
        activity.Targets ??= new List<EntityRefDto>();
        foreach (var target in activity.Targets)
        {
            NormalizeEntityRef(target);
        }

        // Normalize Tags: trim, remove empty, dedupe case-insensitively
        activity.Tags ??= new List<string>();
        activity.Tags = activity.Tags
            .Select(t => t?.Trim() ?? string.Empty)
            .Where(t => !string.IsNullOrEmpty(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Normalize Source
        if (activity.Source is not null)
        {
            activity.Source.System = activity.Source.System?.Trim();
            activity.Source.CorrelationId = activity.Source.CorrelationId?.Trim();
            activity.Source.IdempotencyKey = activity.Source.IdempotencyKey?.Trim();
        }
    }

    private static void NormalizeEntityRef(EntityRefDto entityRef)
    {
        entityRef.Kind = entityRef.Kind?.Trim() ?? string.Empty;
        entityRef.Type = entityRef.Type?.Trim() ?? string.Empty;
        entityRef.Id = entityRef.Id?.Trim() ?? string.Empty;
        entityRef.Display = entityRef.Display?.Trim();
    }
}
