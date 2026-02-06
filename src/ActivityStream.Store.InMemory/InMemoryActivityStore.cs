using System.Collections.Concurrent;
using ActivityStream.Abstractions;

namespace ActivityStream.Store.InMemory;

/// <summary>
/// In-memory implementation of IActivityStore for testing and development.
/// Thread-safe, correctness-focused reference implementation.
/// </summary>
public sealed class InMemoryActivityStore : IActivityStore
{
    private readonly ConcurrentDictionary<string, List<ActivityDto>> _tenantActivities = new();
    private readonly ConcurrentDictionary<string, string> _idempotencyIndex = new();
    private readonly object _lock = new();

    public Task<ActivityDto?> GetByIdAsync(string tenantId, string id, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(id))
            return Task.FromResult<ActivityDto?>(null);

        lock (_lock)
        {
            if (_tenantActivities.TryGetValue(tenantId, out var activities))
            {
                var activity = activities.FirstOrDefault(a => a.Id == id);
                return Task.FromResult(activity);
            }
        }

        return Task.FromResult<ActivityDto?>(null);
    }

    public Task<ActivityDto?> FindByIdempotencyAsync(
        string tenantId,
        string sourceSystem,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(tenantId) ||
            string.IsNullOrEmpty(sourceSystem) ||
            string.IsNullOrEmpty(idempotencyKey))
        {
            return Task.FromResult<ActivityDto?>(null);
        }

        var lookupKey = BuildIdempotencyKey(tenantId, sourceSystem, idempotencyKey);

        if (_idempotencyIndex.TryGetValue(lookupKey, out var activityId))
        {
            return GetByIdAsync(tenantId, activityId, ct);
        }

        return Task.FromResult<ActivityDto?>(null);
    }

    public Task AppendAsync(ActivityDto activity, CancellationToken ct = default)
    {
        if (activity is null)
            throw new ArgumentNullException(nameof(activity));

        if (string.IsNullOrEmpty(activity.Id))
            throw new ArgumentException("Activity must have an Id.", nameof(activity));

        lock (_lock)
        {
            var activities = _tenantActivities.GetOrAdd(activity.TenantId, _ => new List<ActivityDto>());
            activities.Add(activity);

            // Index for idempotency lookup
            if (!string.IsNullOrEmpty(activity.Source?.System) &&
                !string.IsNullOrEmpty(activity.Source?.IdempotencyKey))
            {
                var lookupKey = BuildIdempotencyKey(
                    activity.TenantId,
                    activity.Source.System,
                    activity.Source.IdempotencyKey);
                _idempotencyIndex.TryAdd(lookupKey, activity.Id);
            }
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ActivityDto>> QueryAsync(ActivityQuery query, CancellationToken ct = default)
    {
        if (query is null)
            throw new ArgumentNullException(nameof(query));

        lock (_lock)
        {
            if (!_tenantActivities.TryGetValue(query.TenantId, out var activities))
            {
                return Task.FromResult<IReadOnlyList<ActivityDto>>(Array.Empty<ActivityDto>());
            }

            IEnumerable<ActivityDto> results = activities;

            // Filter by TypeKey
            if (!string.IsNullOrEmpty(query.TypeKey))
            {
                results = results.Where(a => a.TypeKey == query.TypeKey);
            }

            // Filter by Actor (exact match on Kind+Type+Id, case-sensitive)
            if (query.Actor is not null)
            {
                results = results.Where(a =>
                    a.Actor is not null &&
                    a.Actor.Kind == query.Actor.Kind &&
                    a.Actor.Type == query.Actor.Type &&
                    a.Actor.Id == query.Actor.Id);
            }

            // Filter by Target (matches any target in Targets list, case-sensitive)
            if (query.Target is not null)
            {
                results = results.Where(a =>
                    a.Targets.Any(t =>
                        t.Kind == query.Target.Kind &&
                        t.Type == query.Target.Type &&
                        t.Id == query.Target.Id));
            }

            // Filter by From (inclusive)
            if (query.From.HasValue)
            {
                results = results.Where(a => a.OccurredAt >= query.From.Value);
            }

            // Filter by To (exclusive)
            if (query.To.HasValue)
            {
                results = results.Where(a => a.OccurredAt < query.To.Value);
            }

            // Order by OccurredAt desc, then Id desc
            var orderedResults = results
                .OrderByDescending(a => a.OccurredAt)
                .ThenByDescending(a => a.Id, StringComparer.Ordinal)
                .ToList();

            return Task.FromResult<IReadOnlyList<ActivityDto>>(orderedResults);
        }
    }

    /// <summary>
    /// Clears all data in the store. Useful for testing.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _tenantActivities.Clear();
            _idempotencyIndex.Clear();
        }
    }

    /// <summary>
    /// Gets the count of activities for a tenant. Useful for testing.
    /// </summary>
    public int GetCount(string tenantId)
    {
        lock (_lock)
        {
            if (_tenantActivities.TryGetValue(tenantId, out var activities))
            {
                return activities.Count;
            }
            return 0;
        }
    }

    private static string BuildIdempotencyKey(string tenantId, string system, string idempotencyKey)
    {
        return $"{tenantId}|{system}|{idempotencyKey}";
    }
}
