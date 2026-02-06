using ActivityStream.Abstractions;
using Inbox.Abstractions;
using RelationshipService.Abstractions;

namespace Inbox.Core;

/// <summary>
/// Core service for managing inbox notifications and follow/subscribe requests.
/// </summary>
public sealed class InboxNotificationService : IInboxNotificationService
{
    private readonly IInboxStore _inboxStore;
    private readonly IFollowRequestStore _requestStore;
    private readonly IRelationshipService _relationshipService;
    private readonly IEntityGovernancePolicy _governancePolicy;
    private readonly IIdGenerator _idGenerator;
    private readonly IRecipientExpansionPolicy _recipientExpansion;

    public InboxNotificationService(
        IInboxStore inboxStore,
        IFollowRequestStore requestStore,
        IRelationshipService relationshipService,
        IEntityGovernancePolicy governancePolicy,
        IIdGenerator idGenerator,
        IRecipientExpansionPolicy? recipientExpansion = null)
    {
        _inboxStore = inboxStore ?? throw new ArgumentNullException(nameof(inboxStore));
        _requestStore = requestStore ?? throw new ArgumentNullException(nameof(requestStore));
        _relationshipService = relationshipService ?? throw new ArgumentNullException(nameof(relationshipService));
        _governancePolicy = governancePolicy ?? throw new ArgumentNullException(nameof(governancePolicy));
        _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
        _recipientExpansion = recipientExpansion ?? new DefaultRecipientExpansionPolicy();
    }

    /// <inheritdoc />
    public async Task OnActivityPublishedAsync(ActivityDto activity, CancellationToken ct = default)
    {
        // Step 1: Policy enforcement - check Actor, Targets, and Owner are targetable
        await EnforcePolicyAsync(activity.TenantId, activity, ct);

        // Step 2: Recipient selection - get followers of Actor and subscribers of Targets
        var recipients = await SelectRecipientsAsync(activity, ct);

        // Step 3: For each recipient, check permissions and create inbox item
        foreach (var recipient in recipients)
        {
            // Expand recipient (default: no expansion)
            var expandedRecipients = await _recipientExpansion.ExpandRecipientsAsync(
                activity.TenantId, recipient, ct);

            foreach (var expandedRecipient in expandedRecipients)
            {
                await ProcessRecipientAsync(activity, expandedRecipient, ct);
            }
        }
    }

    /// <inheritdoc />
    public async Task<InboxItemDto> AddAsync(InboxItemDto item, CancellationToken ct = default)
    {
        // Normalize
        item.TenantId = InboxNormalizer.NormalizeTenantId(item.TenantId);
        InboxNormalizer.Normalize(item);
        InboxNormalizer.NormalizeEventRef(item.Event);

        // Validate
        var errors = InboxValidator.ValidateInboxItem(item);
        if (errors.Count > 0)
            throw new InboxValidationException(errors);

        // Generate ID if missing
        if (string.IsNullOrEmpty(item.Id))
            item.Id = _idGenerator.NewId();

        // Set timestamps
        if (item.CreatedAt == default)
            item.CreatedAt = DateTimeOffset.UtcNow;

        // Check dedup
        if (!string.IsNullOrEmpty(item.DedupKey))
        {
            var existing = await _inboxStore.FindByDedupKeyAsync(
                item.TenantId, item.Recipient, item.DedupKey, ct);
            if (existing is not null)
                return existing;
        }

        // Check thread grouping
        if (!string.IsNullOrEmpty(item.ThreadKey))
        {
            var existingThread = await _inboxStore.FindByThreadKeyAsync(
                item.TenantId, item.Recipient, item.ThreadKey, ct);
            if (existingThread is not null)
            {
                // Increment thread count and update
                existingThread.ThreadCount++;
                existingThread.UpdatedAt = DateTimeOffset.UtcNow;
                // Note: Status stays as-is per decision #2 (stay Read, move to top)
                await _inboxStore.UpsertAsync(existingThread, ct);
                return existingThread;
            }
        }

        // Persist
        await _inboxStore.UpsertAsync(item, ct);
        return item;
    }

    /// <inheritdoc />
    public async Task<InboxPageResult> QueryInboxAsync(InboxQuery query, CancellationToken ct = default)
    {
        // Normalize
        query.TenantId = InboxNormalizer.NormalizeTenantId(query.TenantId);

        // Validate
        var errors = InboxValidator.ValidateQuery(query);
        if (errors.Count > 0)
            throw new InboxValidationException(errors);

        // Clamp limit
        if (query.Limit > InboxValidator.MaxQueryLimit)
            query.Limit = InboxValidator.MaxQueryLimit;

        return await _inboxStore.QueryAsync(query, ct);
    }

    /// <inheritdoc />
    public async Task MarkReadAsync(string tenantId, string inboxItemId, CancellationToken ct = default)
    {
        tenantId = InboxNormalizer.NormalizeTenantId(tenantId);
        await _inboxStore.UpdateStatusAsync(tenantId, inboxItemId, InboxItemStatus.Read, ct);
    }

    /// <inheritdoc />
    public async Task ArchiveAsync(string tenantId, string inboxItemId, CancellationToken ct = default)
    {
        tenantId = InboxNormalizer.NormalizeTenantId(tenantId);
        await _inboxStore.UpdateStatusAsync(tenantId, inboxItemId, InboxItemStatus.Archived, ct);
    }

    /// <inheritdoc />
    public async Task<FollowRequestDto> CreateFollowRequestAsync(FollowRequestDto request, CancellationToken ct = default)
    {
        // Normalize
        request.TenantId = InboxNormalizer.NormalizeTenantId(request.TenantId);
        InboxNormalizer.Normalize(request);

        // Validate
        var errors = InboxValidator.ValidateFollowRequest(request);
        if (errors.Count > 0)
            throw new InboxValidationException(errors);

        // Policy enforcement: check if target is targetable
        var isTargetable = await _governancePolicy.IsTargetableAsync(
            request.TenantId, request.Target, ct);
        if (!isTargetable)
            throw new InboxPolicyViolationException(request.Target, "NOT_TARGETABLE");

        // Check idempotency
        if (!string.IsNullOrEmpty(request.IdempotencyKey))
        {
            var existing = await _requestStore.FindByIdempotencyAsync(
                request.TenantId, request.IdempotencyKey, ct);
            if (existing is not null)
                return existing;
        }
        else
        {
            // Generate idempotency key from request data
            request.IdempotencyKey = GenerateIdempotencyKey(request);
            var existing = await _requestStore.FindByIdempotencyAsync(
                request.TenantId, request.IdempotencyKey, ct);
            if (existing is not null)
                return existing;
        }

        // Generate ID
        if (string.IsNullOrEmpty(request.Id))
            request.Id = _idGenerator.NewId();

        if (request.CreatedAt == default)
            request.CreatedAt = DateTimeOffset.UtcNow;

        // Check if approval is required
        var requiresApproval = await _governancePolicy.RequiresApprovalToFollowAsync(
            request.TenantId, request.Requester, request.Target, request.RequestedKind, ct);

        if (!requiresApproval)
        {
            // Create relationship edge immediately
            await CreateRelationshipEdgeAsync(request, ct);

            // Mark as approved
            request.Status = FollowRequestStatus.Approved;
            request.DecidedAt = DateTimeOffset.UtcNow;
            await _requestStore.UpsertAsync(request, ct);

            // Notify requester: "follow enabled"
            await NotifyRequesterAsync(request, "Your follow request was approved automatically.", ct);

            return request;
        }

        // Store as pending
        request.Status = FollowRequestStatus.Pending;
        await _requestStore.UpsertAsync(request, ct);

        // Notify approvers
        var approvers = await _governancePolicy.GetApproversAsync(request.TenantId, request.Target, ct);
        await NotifyApproversAsync(request, approvers, ct);

        return request;
    }

    /// <inheritdoc />
    public async Task<FollowRequestDto> ApproveRequestAsync(
        string tenantId,
        string requestId,
        EntityRefDto decidedBy,
        string? reason,
        CancellationToken ct = default)
    {
        tenantId = InboxNormalizer.NormalizeTenantId(tenantId);

        var request = await _requestStore.GetByIdAsync(tenantId, requestId, ct);
        if (request is null)
            throw new InboxValidationException("NOT_FOUND", "Follow request not found.", "requestId");

        if (request.Status != FollowRequestStatus.Pending)
            throw new InboxValidationException("INVALID_STATUS", "Request is not pending.", "Status");

        // Update request
        request.Status = FollowRequestStatus.Approved;
        request.DecidedBy = decidedBy;
        request.DecidedAt = DateTimeOffset.UtcNow;
        request.DecisionReason = reason;

        // Create relationship edge
        await CreateRelationshipEdgeAsync(request, ct);

        await _requestStore.UpsertAsync(request, ct);

        // Notify requester
        await NotifyRequesterAsync(request, reason ?? "Your follow request was approved.", ct);

        return request;
    }

    /// <inheritdoc />
    public async Task<FollowRequestDto> DenyRequestAsync(
        string tenantId,
        string requestId,
        EntityRefDto decidedBy,
        string? reason,
        CancellationToken ct = default)
    {
        tenantId = InboxNormalizer.NormalizeTenantId(tenantId);

        var request = await _requestStore.GetByIdAsync(tenantId, requestId, ct);
        if (request is null)
            throw new InboxValidationException("NOT_FOUND", "Follow request not found.", "requestId");

        if (request.Status != FollowRequestStatus.Pending)
            throw new InboxValidationException("INVALID_STATUS", "Request is not pending.", "Status");

        // Update request
        request.Status = FollowRequestStatus.Denied;
        request.DecidedBy = decidedBy;
        request.DecidedAt = DateTimeOffset.UtcNow;
        request.DecisionReason = reason;

        await _requestStore.UpsertAsync(request, ct);

        // Notify requester
        await NotifyRequesterAsync(request, reason ?? "Your follow request was denied.", ct);

        return request;
    }

    #region Private Helper Methods

    private async Task EnforcePolicyAsync(string tenantId, ActivityDto activity, CancellationToken ct)
    {
        // Check Actor
        if (!await _governancePolicy.IsTargetableAsync(tenantId, activity.Actor, ct))
            throw new InboxPolicyViolationException(activity.Actor, "Actor is not targetable");

        // Check each Target
        foreach (var target in activity.Targets)
        {
            if (!await _governancePolicy.IsTargetableAsync(tenantId, target, ct))
                throw new InboxPolicyViolationException(target, "Target is not targetable");
        }

        // Check Owner if present
        if (activity.Owner is not null)
        {
            if (!await _governancePolicy.IsTargetableAsync(tenantId, activity.Owner, ct))
                throw new InboxPolicyViolationException(activity.Owner, "Owner is not targetable");
        }
    }

    private async Task<HashSet<EntityRefDto>> SelectRecipientsAsync(ActivityDto activity, CancellationToken ct)
    {
        var recipients = new HashSet<EntityRefDto>(new EntityRefComparer());

        // Get followers of Actor
        var followers = await _relationshipService.QueryAsync(new RelationshipQuery
        {
            TenantId = activity.TenantId,
            To = activity.Actor,
            Kind = RelationshipKind.Follow,
            IsActive = true
        }, ct);

        foreach (var edge in followers)
        {
            recipients.Add(edge.From);
        }

        // Get subscribers of each Target
        foreach (var target in activity.Targets)
        {
            var subscribers = await _relationshipService.QueryAsync(new RelationshipQuery
            {
                TenantId = activity.TenantId,
                To = target,
                Kind = RelationshipKind.Subscribe,
                IsActive = true
            }, ct);

            foreach (var edge in subscribers)
            {
                recipients.Add(edge.From);
            }
        }

        // Optionally get subscribers of Owner
        if (activity.Owner is not null)
        {
            var ownerSubscribers = await _relationshipService.QueryAsync(new RelationshipQuery
            {
                TenantId = activity.TenantId,
                To = activity.Owner,
                Kind = RelationshipKind.Subscribe,
                IsActive = true
            }, ct);

            foreach (var edge in ownerSubscribers)
            {
                recipients.Add(edge.From);
            }
        }

        return recipients;
    }

    private async Task ProcessRecipientAsync(ActivityDto activity, EntityRefDto recipient, CancellationToken ct)
    {
        // Permission check
        var decision = await _relationshipService.CanSeeAsync(
            activity.TenantId, recipient, activity, ct);

        if (!decision.Allowed)
            return; // Skip - Hidden or Denied

        // Build inbox item
        var dedupKey = EntityRefKeyHelper.BuildActivityDedupKey(activity.Id!, recipient);
        var firstTarget = activity.Targets.Count > 0 ? activity.Targets[0] : null;
        var threadKey = EntityRefKeyHelper.BuildThreadKey(firstTarget, activity.Actor, activity.TypeKey);

        var item = new InboxItemDto
        {
            TenantId = activity.TenantId,
            Recipient = recipient,
            Kind = InboxItemKind.Notification,
            Event = new InboxEventRefDto
            {
                Kind = "activity",
                Id = activity.Id!,
                TypeKey = activity.TypeKey,
                OccurredAt = activity.OccurredAt
            },
            Title = activity.Summary,
            Targets = new List<EntityRefDto>(activity.Targets),
            DedupKey = dedupKey,
            ThreadKey = threadKey
        };

        // Add actor to targets for "open" actions
        if (!item.Targets.Any(t => EntityRefKeyHelper.AreEqual(t, activity.Actor)))
        {
            item.Targets.Insert(0, activity.Actor);
        }

        await AddAsync(item, ct);
    }

    private async Task CreateRelationshipEdgeAsync(FollowRequestDto request, CancellationToken ct)
    {
        var edge = new RelationshipEdgeDto
        {
            TenantId = request.TenantId,
            From = request.Requester,
            To = request.Target,
            Kind = request.RequestedKind,
            Scope = request.Scope,
            Filter = request.Filter,
            IsActive = true
        };

        await _relationshipService.UpsertAsync(edge, ct);
    }

    private async Task NotifyApproversAsync(
        FollowRequestDto request,
        IReadOnlyList<EntityRefDto> approvers,
        CancellationToken ct)
    {
        foreach (var approver in approvers)
        {
            var item = new InboxItemDto
            {
                TenantId = request.TenantId,
                Recipient = approver,
                Kind = InboxItemKind.Request,
                Event = new InboxEventRefDto
                {
                    Kind = "follow-request",
                    Id = request.Id!,
                    OccurredAt = request.CreatedAt
                },
                Title = $"Follow request from {request.Requester.Display ?? request.Requester.Id}",
                Body = $"Requesting to {request.RequestedKind.ToString().ToLowerInvariant()} {request.Target.Display ?? request.Target.Id}",
                Targets = new List<EntityRefDto> { request.Requester, request.Target },
                DedupKey = $"follow-request:{request.Id}:approver:{EntityRefKeyHelper.ToKey(approver)}"
            };

            await AddAsync(item, ct);
        }
    }

    private async Task NotifyRequesterAsync(FollowRequestDto request, string message, CancellationToken ct)
    {
        var item = new InboxItemDto
        {
            TenantId = request.TenantId,
            Recipient = request.Requester,
            Kind = InboxItemKind.Notification,
            Event = new InboxEventRefDto
            {
                Kind = "follow-request",
                Id = request.Id!,
                OccurredAt = request.DecidedAt ?? request.CreatedAt
            },
            Title = $"Follow request {request.Status.ToString().ToLowerInvariant()}",
            Body = message,
            Targets = new List<EntityRefDto> { request.Target },
            DedupKey = $"follow-request:{request.Id}:result:{EntityRefKeyHelper.ToKey(request.Requester)}"
        };

        await AddAsync(item, ct);
    }

    private static string GenerateIdempotencyKey(FollowRequestDto request)
    {
        var requesterKey = EntityRefKeyHelper.ToKey(request.Requester);
        var targetKey = EntityRefKeyHelper.ToKey(request.Target);
        return $"{requesterKey}:{targetKey}:{(int)request.RequestedKind}:{(int)request.Scope}";
    }

    #endregion

    /// <summary>
    /// Comparer for EntityRefDto using normalized keys.
    /// </summary>
    private sealed class EntityRefComparer : IEqualityComparer<EntityRefDto>
    {
        public bool Equals(EntityRefDto? x, EntityRefDto? y) => EntityRefKeyHelper.AreEqual(x, y);
        public int GetHashCode(EntityRefDto obj) => EntityRefKeyHelper.ToKey(obj).GetHashCode();
    }
}
