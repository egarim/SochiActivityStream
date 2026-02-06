using System.Collections.Concurrent;
using ActivityStream.Abstractions;
using Inbox.Abstractions;

namespace Inbox.Store.InMemory;

/// <summary>
/// In-memory implementation of IInboxStore for testing and development.
/// Thread-safe using ConcurrentDictionary.
/// </summary>
public class InMemoryInboxStore : IInboxStore
{
    // Primary storage: tenantId -> (itemId -> item)
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, InboxItemDto>> _tenantItems = new();

    // Secondary index for dedup: "{tenant}|{recipientKey}|{dedupKey}" -> itemId
    private readonly ConcurrentDictionary<string, string> _dedupIndex = new();

    // Secondary index for thread: "{tenant}|{recipientKey}|{threadKey}" -> itemId
    private readonly ConcurrentDictionary<string, string> _threadIndex = new();

    private readonly object _lock = new();

    /// <inheritdoc />
    public Task<InboxItemDto?> GetByIdAsync(string tenantId, string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(id))
            return Task.FromResult<InboxItemDto?>(null);

        var normalizedTenantId = NormalizeTenantId(tenantId);

        if (_tenantItems.TryGetValue(normalizedTenantId, out var items) &&
            items.TryGetValue(id, out var item))
        {
            return Task.FromResult<InboxItemDto?>(CloneItem(item));
        }

        return Task.FromResult<InboxItemDto?>(null);
    }

    /// <inheritdoc />
    public Task<InboxItemDto?> FindByDedupKeyAsync(
        string tenantId,
        EntityRefDto recipient,
        string dedupKey,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dedupKey))
            return Task.FromResult<InboxItemDto?>(null);

        var indexKey = BuildDedupIndexKey(tenantId, recipient, dedupKey);

        if (_dedupIndex.TryGetValue(indexKey, out var itemId))
        {
            return GetByIdAsync(tenantId, itemId, ct);
        }

        return Task.FromResult<InboxItemDto?>(null);
    }

    /// <inheritdoc />
    public Task<InboxItemDto?> FindByThreadKeyAsync(
        string tenantId,
        EntityRefDto recipient,
        string threadKey,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(threadKey))
            return Task.FromResult<InboxItemDto?>(null);

        var indexKey = BuildThreadIndexKey(tenantId, recipient, threadKey);

        if (_threadIndex.TryGetValue(indexKey, out var itemId))
        {
            return GetByIdAsync(tenantId, itemId, ct);
        }

        return Task.FromResult<InboxItemDto?>(null);
    }

    /// <inheritdoc />
    public Task UpsertAsync(InboxItemDto item, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(item.Id);

        var normalizedTenantId = NormalizeTenantId(item.TenantId);

        lock (_lock)
        {
            // Get or create tenant dictionary
            var items = _tenantItems.GetOrAdd(normalizedTenantId, _ => new ConcurrentDictionary<string, InboxItemDto>());

            // Store the item
            items[item.Id] = CloneItem(item);

            // Update dedup index
            if (!string.IsNullOrEmpty(item.DedupKey))
            {
                var dedupIndexKey = BuildDedupIndexKey(item.TenantId, item.Recipient, item.DedupKey);
                _dedupIndex[dedupIndexKey] = item.Id;
            }

            // Update thread index
            if (!string.IsNullOrEmpty(item.ThreadKey))
            {
                var threadIndexKey = BuildThreadIndexKey(item.TenantId, item.Recipient, item.ThreadKey);
                _threadIndex[threadIndexKey] = item.Id;
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<InboxPageResult> QueryAsync(InboxQuery query, CancellationToken ct = default)
    {
        var normalizedTenantId = NormalizeTenantId(query.TenantId);
        var result = new InboxPageResult();

        if (!_tenantItems.TryGetValue(normalizedTenantId, out var items))
            return Task.FromResult(result);

        // Collect all matching items
        var matchingItems = items.Values
            .Where(item => MatchesQuery(item, query))
            .ToList();

        // Sort by CreatedAt DESC, then Id DESC (for stable ordering)
        // But for threads, we want UpdatedAt to determine position
        matchingItems = matchingItems
            .OrderByDescending(i => i.UpdatedAt ?? i.CreatedAt)
            .ThenByDescending(i => i.Id)
            .ToList();

        // Apply cursor
        if (!string.IsNullOrEmpty(query.Cursor))
        {
            if (TryDecodeCursor(query.Cursor, out var cursorTime, out var cursorId))
            {
                matchingItems = matchingItems
                    .Where(i =>
                    {
                        var itemTime = i.UpdatedAt ?? i.CreatedAt;
                        return itemTime < cursorTime ||
                               (itemTime == cursorTime && string.Compare(i.Id, cursorId, StringComparison.Ordinal) < 0);
                    })
                    .ToList();
            }
        }

        // Apply limit + 1 to determine if there are more
        var pageItems = matchingItems.Take(query.Limit + 1).ToList();

        if (pageItems.Count > query.Limit)
        {
            pageItems = pageItems.Take(query.Limit).ToList();
            var lastItem = pageItems.Last();
            result.NextCursor = EncodeCursor(lastItem.UpdatedAt ?? lastItem.CreatedAt, lastItem.Id!);
        }

        result.Items = pageItems.Select(CloneItem).ToList();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task UpdateStatusAsync(string tenantId, string id, InboxItemStatus status, CancellationToken ct = default)
    {
        var normalizedTenantId = NormalizeTenantId(tenantId);

        if (_tenantItems.TryGetValue(normalizedTenantId, out var items) &&
            items.TryGetValue(id, out var item))
        {
            lock (_lock)
            {
                item.Status = status;
                item.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        return Task.CompletedTask;
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

    private static string BuildDedupIndexKey(string tenantId, EntityRefDto recipient, string dedupKey)
    {
        return $"{NormalizeTenantId(tenantId)}|{ToEntityKey(recipient)}|dedup|{dedupKey.Trim().ToLowerInvariant()}";
    }

    private static string BuildThreadIndexKey(string tenantId, EntityRefDto recipient, string threadKey)
    {
        return $"{NormalizeTenantId(tenantId)}|{ToEntityKey(recipient)}|thread|{threadKey.Trim().ToLowerInvariant()}";
    }

    private static bool MatchesQuery(InboxItemDto item, InboxQuery query)
    {
        // Filter by recipients
        if (query.Recipients.Count > 0)
        {
            var itemRecipientKey = ToEntityKey(item.Recipient);
            var matched = query.Recipients.Any(r => ToEntityKey(r) == itemRecipientKey);
            if (!matched)
                return false;
        }

        // Filter by status
        if (query.Status.HasValue && item.Status != query.Status.Value)
            return false;

        // Filter by kind
        if (query.Kind.HasValue && item.Kind != query.Kind.Value)
            return false;

        // Filter by time range
        if (query.From.HasValue && item.CreatedAt < query.From.Value)
            return false;

        if (query.To.HasValue && item.CreatedAt >= query.To.Value)
            return false;

        return true;
    }

    private static string EncodeCursor(DateTimeOffset time, string id)
    {
        var raw = $"{time:O}|{id}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(raw);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static bool TryDecodeCursor(string cursor, out DateTimeOffset time, out string id)
    {
        time = default;
        id = string.Empty;

        try
        {
            var base64 = cursor.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            var bytes = Convert.FromBase64String(base64);
            var raw = System.Text.Encoding.UTF8.GetString(bytes);
            var separatorIndex = raw.IndexOf('|');
            if (separatorIndex < 0)
                return false;

            if (!DateTimeOffset.TryParse(raw[..separatorIndex], out time))
                return false;

            id = raw[(separatorIndex + 1)..];
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static InboxItemDto CloneItem(InboxItemDto item)
    {
        return new InboxItemDto
        {
            Id = item.Id,
            TenantId = item.TenantId,
            Recipient = CloneEntityRef(item.Recipient),
            Kind = item.Kind,
            Event = new InboxEventRefDto
            {
                Kind = item.Event.Kind,
                Id = item.Event.Id,
                TypeKey = item.Event.TypeKey,
                OccurredAt = item.Event.OccurredAt
            },
            Title = item.Title,
            Body = item.Body,
            Targets = item.Targets.Select(CloneEntityRef).ToList(),
            Data = item.Data is not null ? new Dictionary<string, object?>(item.Data) : null,
            Status = item.Status,
            DedupKey = item.DedupKey,
            ThreadKey = item.ThreadKey,
            ThreadCount = item.ThreadCount,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
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

    #endregion
}
