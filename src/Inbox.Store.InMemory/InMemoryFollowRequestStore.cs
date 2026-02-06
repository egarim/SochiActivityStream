using System.Collections.Concurrent;
using ActivityStream.Abstractions;
using Inbox.Abstractions;

namespace Inbox.Store.InMemory;

/// <summary>
/// In-memory implementation of IFollowRequestStore for testing and development.
/// Thread-safe using ConcurrentDictionary.
/// </summary>
public class InMemoryFollowRequestStore : IFollowRequestStore
{
    // Primary storage: tenantId -> (requestId -> request)
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, FollowRequestDto>> _tenantRequests = new();

    // Secondary index for idempotency: "{tenant}|{idempotencyKey}" -> requestId
    private readonly ConcurrentDictionary<string, string> _idempotencyIndex = new();

    private readonly object _lock = new();

    /// <inheritdoc />
    public Task<FollowRequestDto?> GetByIdAsync(string tenantId, string requestId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(requestId))
            return Task.FromResult<FollowRequestDto?>(null);

        var normalizedTenantId = NormalizeTenantId(tenantId);

        if (_tenantRequests.TryGetValue(normalizedTenantId, out var requests) &&
            requests.TryGetValue(requestId, out var request))
        {
            return Task.FromResult<FollowRequestDto?>(CloneRequest(request));
        }

        return Task.FromResult<FollowRequestDto?>(null);
    }

    /// <inheritdoc />
    public Task<FollowRequestDto?> FindByIdempotencyAsync(
        string tenantId,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return Task.FromResult<FollowRequestDto?>(null);

        var indexKey = BuildIdempotencyKey(tenantId, idempotencyKey);

        if (_idempotencyIndex.TryGetValue(indexKey, out var requestId))
        {
            return GetByIdAsync(tenantId, requestId, ct);
        }

        return Task.FromResult<FollowRequestDto?>(null);
    }

    /// <inheritdoc />
    public Task UpsertAsync(FollowRequestDto request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Id);

        var normalizedTenantId = NormalizeTenantId(request.TenantId);

        lock (_lock)
        {
            // Get or create tenant dictionary
            var requests = _tenantRequests.GetOrAdd(normalizedTenantId, _ => new ConcurrentDictionary<string, FollowRequestDto>());

            // Store the request
            requests[request.Id] = CloneRequest(request);

            // Update idempotency index
            if (!string.IsNullOrEmpty(request.IdempotencyKey))
            {
                var idempotencyIndexKey = BuildIdempotencyKey(request.TenantId, request.IdempotencyKey);
                _idempotencyIndex[idempotencyIndexKey] = request.Id;
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<FollowRequestDto>> QueryPendingForTargetAsync(
        string tenantId,
        EntityRefDto target,
        CancellationToken ct = default)
    {
        var normalizedTenantId = NormalizeTenantId(tenantId);
        var targetKey = ToEntityKey(target);

        if (!_tenantRequests.TryGetValue(normalizedTenantId, out var requests))
            return Task.FromResult<IReadOnlyList<FollowRequestDto>>(Array.Empty<FollowRequestDto>());

        var pending = requests.Values
            .Where(r => r.Status == FollowRequestStatus.Pending && ToEntityKey(r.Target) == targetKey)
            .Select(CloneRequest)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<FollowRequestDto>>(pending);
    }

    #region Private Helpers

    private static string NormalizeTenantId(string tenantId)
    {
        return tenantId?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private static string ToEntityKey(EntityRefDto entity)
    {
        var kind = entity.Kind?.Trim().ToLowerInvariant() ?? string.Empty;
        var type = entity.Type?.Trim().ToLowerInvariant() ?? string.Empty;
        var id = entity.Id?.Trim().ToLowerInvariant() ?? string.Empty;
        return $"{kind}|{type}|{id}";
    }

    private static string BuildIdempotencyKey(string tenantId, string idempotencyKey)
    {
        return $"{NormalizeTenantId(tenantId)}|{idempotencyKey.Trim().ToLowerInvariant()}";
    }

    private static FollowRequestDto CloneRequest(FollowRequestDto request)
    {
        return new FollowRequestDto
        {
            Id = request.Id,
            TenantId = request.TenantId,
            Requester = CloneEntityRef(request.Requester),
            Target = CloneEntityRef(request.Target),
            RequestedKind = request.RequestedKind,
            Scope = request.Scope,
            Filter = request.Filter is not null ? CloneFilter(request.Filter) : null,
            Status = request.Status,
            DecidedBy = request.DecidedBy is not null ? CloneEntityRef(request.DecidedBy) : null,
            DecidedAt = request.DecidedAt,
            DecisionReason = request.DecisionReason,
            CreatedAt = request.CreatedAt,
            IdempotencyKey = request.IdempotencyKey
        };
    }

    private static EntityRefDto CloneEntityRef(EntityRefDto entity)
    {
        return new EntityRefDto
        {
            Kind = entity.Kind,
            Type = entity.Type,
            Id = entity.Id,
            Display = entity.Display,
            Meta = entity.Meta is not null ? new Dictionary<string, object?>(entity.Meta) : null
        };
    }

    private static RelationshipService.Abstractions.RelationshipFilterDto CloneFilter(
        RelationshipService.Abstractions.RelationshipFilterDto filter)
    {
        return new RelationshipService.Abstractions.RelationshipFilterDto
        {
            TypeKeys = filter.TypeKeys is not null ? new List<string>(filter.TypeKeys) : null,
            TypeKeyPrefixes = filter.TypeKeyPrefixes is not null ? new List<string>(filter.TypeKeyPrefixes) : null,
            RequiredTagsAny = filter.RequiredTagsAny is not null ? new List<string>(filter.RequiredTagsAny) : null,
            ExcludedTagsAny = filter.ExcludedTagsAny is not null ? new List<string>(filter.ExcludedTagsAny) : null,
            AllowedVisibilities = filter.AllowedVisibilities is not null
                ? new List<ActivityStream.Abstractions.ActivityVisibility>(filter.AllowedVisibilities)
                : null
        };
    }

    #endregion
}
