using Realtime.Abstractions;

namespace Realtime.Core;

/// <summary>
/// Main implementation of IRealtimePublisher.
/// Resolves targets to connections and dispatches events.
/// </summary>
public sealed class RealtimePublisher : IRealtimePublisher
{
    private readonly IEventDispatcher _dispatcher;
    private readonly IConnectionManager _connections;
    private readonly IGroupMembershipResolver _membershipResolver;
    private readonly IIdGenerator _idGenerator;

    public RealtimePublisher(
        IEventDispatcher dispatcher,
        IConnectionManager connections,
        IGroupMembershipResolver membershipResolver,
        IIdGenerator idGenerator)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        _membershipResolver = membershipResolver ?? throw new ArgumentNullException(nameof(membershipResolver));
        _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
    }

    /// <inheritdoc />
    public async Task PublishAsync(RealtimeEvent evt, CancellationToken ct = default)
    {
        EventValidator.Validate(evt);

        // Assign ID and timestamp if not set
        evt.Id ??= _idGenerator.NewId();
        if (evt.Timestamp == default)
            evt.Timestamp = DateTimeOffset.UtcNow;

        // Handle special targets
        if (evt.Target.Kind == TargetKind.Tenant)
        {
            await _dispatcher.DispatchToAllAsync(evt.TenantId, evt, ct);
            return;
        }

        // Resolve target to connection IDs
        var connectionIds = await ResolveTargetConnectionsAsync(evt.TenantId, evt.Target, ct);

        if (connectionIds.Count > 0)
        {
            await _dispatcher.DispatchToConnectionsAsync(connectionIds, evt, ct);
        }
    }

    /// <inheritdoc />
    public async Task PublishBatchAsync(IEnumerable<RealtimeEvent> events, CancellationToken ct = default)
    {
        foreach (var evt in events)
        {
            await PublishAsync(evt, ct);
        }
    }

    /// <inheritdoc />
    public Task SendToProfileAsync(
        string tenantId,
        EntityRefDto profile,
        string eventType,
        object payload,
        CancellationToken ct = default)
    {
        var evt = new RealtimeEvent
        {
            TenantId = tenantId,
            Type = eventType,
            Payload = payload,
            Target = EventTarget.ToProfile(profile)
        };
        return PublishAsync(evt, ct);
    }

    /// <inheritdoc />
    public Task SendToProfilesAsync(
        string tenantId,
        IEnumerable<EntityRefDto> profiles,
        string eventType,
        object payload,
        CancellationToken ct = default)
    {
        var profileList = profiles.ToList();
        if (profileList.Count == 0)
            return Task.CompletedTask;

        var evt = new RealtimeEvent
        {
            TenantId = tenantId,
            Type = eventType,
            Payload = payload,
            Target = EventTarget.ToProfiles(profileList)
        };
        return PublishAsync(evt, ct);
    }

    /// <inheritdoc />
    public Task SendToConversationAsync(
        string tenantId,
        string conversationId,
        string eventType,
        object payload,
        CancellationToken ct = default)
    {
        var evt = new RealtimeEvent
        {
            TenantId = tenantId,
            Type = eventType,
            Payload = payload,
            Target = EventTarget.ToConversation(conversationId)
        };
        return PublishAsync(evt, ct);
    }

    /// <inheritdoc />
    public Task SendToGroupAsync(
        string tenantId,
        string groupId,
        string eventType,
        object payload,
        CancellationToken ct = default)
    {
        var evt = new RealtimeEvent
        {
            TenantId = tenantId,
            Type = eventType,
            Payload = payload,
            Target = EventTarget.ToGroup(groupId)
        };
        return PublishAsync(evt, ct);
    }

    /// <inheritdoc />
    public Task BroadcastAsync(
        string tenantId,
        string eventType,
        object payload,
        CancellationToken ct = default)
    {
        var evt = new RealtimeEvent
        {
            TenantId = tenantId,
            Type = eventType,
            Payload = payload,
            Target = EventTarget.ToTenant()
        };
        return PublishAsync(evt, ct);
    }

    private async Task<IReadOnlyList<string>> ResolveTargetConnectionsAsync(
        string tenantId,
        EventTarget target,
        CancellationToken ct)
    {
        return target.Kind switch
        {
            TargetKind.Profile => await GetProfileConnectionsAsync(tenantId, target.Profiles!.First(), ct),
            TargetKind.Profiles => await GetMultiProfileConnectionsAsync(tenantId, target.Profiles!, ct),
            TargetKind.Conversation => await GetConversationConnectionsAsync(tenantId, target.ConversationId!, ct),
            TargetKind.Group => await GetGroupConnectionsAsync(tenantId, target.GroupId!, ct),
            TargetKind.Connection => [target.ConnectionId!],
            _ => []
        };
    }

    private async Task<IReadOnlyList<string>> GetProfileConnectionsAsync(
        string tenantId,
        EntityRefDto profile,
        CancellationToken ct)
    {
        var connections = await _connections.GetConnectionsForProfileAsync(tenantId, profile, ct);
        return connections.Select(c => c.ConnectionId).ToList();
    }

    private async Task<IReadOnlyList<string>> GetMultiProfileConnectionsAsync(
        string tenantId,
        IEnumerable<EntityRefDto> profiles,
        CancellationToken ct)
    {
        var allConnections = new List<string>();

        foreach (var profile in profiles)
        {
            var connections = await _connections.GetConnectionsForProfileAsync(tenantId, profile, ct);
            allConnections.AddRange(connections.Select(c => c.ConnectionId));
        }

        return allConnections.Distinct().ToList();
    }

    private async Task<IReadOnlyList<string>> GetConversationConnectionsAsync(
        string tenantId,
        string conversationId,
        CancellationToken ct)
    {
        var members = await _membershipResolver.GetConversationMembersAsync(tenantId, conversationId, ct);
        return await GetMultiProfileConnectionsAsync(tenantId, members, ct);
    }

    private async Task<IReadOnlyList<string>> GetGroupConnectionsAsync(
        string tenantId,
        string groupId,
        CancellationToken ct)
    {
        var members = await _membershipResolver.GetGroupMembersAsync(tenantId, groupId, ct);
        return await GetMultiProfileConnectionsAsync(tenantId, members, ct);
    }
}
