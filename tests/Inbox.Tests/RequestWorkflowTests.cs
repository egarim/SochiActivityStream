using ActivityStream.Abstractions;
using ActivityStream.Core;
using Inbox.Abstractions;
using Inbox.Core;
using Inbox.Store.InMemory;
using RelationshipService.Abstractions;
using RelationshipService.Core;
using RelationshipService.Store.InMemory;

namespace Inbox.Tests;

/// <summary>
/// Tests for request workflow: create/approve/deny flow.
/// </summary>
public class RequestWorkflowTests
{
    private readonly InboxNotificationService _service;
    private readonly InMemoryInboxStore _inboxStore;
    private readonly InMemoryFollowRequestStore _requestStore;
    private readonly RelationshipServiceImpl _relationshipService;
    private readonly InMemoryRelationshipStore _relationshipStore;
    private readonly TestGovernancePolicy _policy;

    public RequestWorkflowTests()
    {
        _inboxStore = new InMemoryInboxStore();
        _requestStore = new InMemoryFollowRequestStore();
        _relationshipStore = new InMemoryRelationshipStore();
        _relationshipService = new RelationshipServiceImpl(_relationshipStore, new UlidIdGenerator());
        _policy = new TestGovernancePolicy();

        _service = new InboxNotificationService(
            _inboxStore,
            _requestStore,
            _relationshipService,
            _policy,
            new UlidIdGenerator());
    }

    private static EntityRefDto CreateProfile(string id) => new()
    {
        Kind = "identity",
        Type = "Profile",
        Id = id
    };

    [Fact]
    public async Task CreateFollowRequest_no_approval_creates_edge_immediately()
    {
        var requester = CreateProfile("p_requester");
        var target = CreateProfile("p_target");

        // No approval required by default
        var result = await _service.CreateFollowRequestAsync(new FollowRequestDto
        {
            TenantId = "acme",
            Requester = requester,
            Target = target,
            RequestedKind = RelationshipKind.Follow,
            IdempotencyKey = "key_1"
        });

        Assert.Equal(FollowRequestStatus.Approved, result.Status);

        // Edge should exist
        var edges = await _relationshipService.QueryAsync(new RelationshipQuery
        {
            TenantId = "acme",
            From = requester,
            Kind = RelationshipKind.Follow
        });
        Assert.Single(edges);
    }

    [Fact]
    public async Task CreateFollowRequest_with_approval_stays_pending()
    {
        var requester = CreateProfile("p_requester");
        var target = CreateProfile("p_target");
        var approver = CreateProfile("p_approver");

        _policy.SetRequiresApproval(target);
        _policy.SetApprovers(target, approver);

        var result = await _service.CreateFollowRequestAsync(new FollowRequestDto
        {
            TenantId = "acme",
            Requester = requester,
            Target = target,
            RequestedKind = RelationshipKind.Follow,
            IdempotencyKey = "key_1"
        });

        Assert.Equal(FollowRequestStatus.Pending, result.Status);

        // No edge yet
        var edges = await _relationshipService.QueryAsync(new RelationshipQuery
        {
            TenantId = "acme",
            From = requester,
            Kind = RelationshipKind.Follow
        });
        Assert.Empty(edges);
    }

    [Fact]
    public async Task ApproveRequest_creates_edge()
    {
        var requester = CreateProfile("p_requester");
        var target = CreateProfile("p_target");
        var approver = CreateProfile("p_approver");

        _policy.SetRequiresApproval(target);
        _policy.SetApprovers(target, approver);

        var request = await _service.CreateFollowRequestAsync(new FollowRequestDto
        {
            TenantId = "acme",
            Requester = requester,
            Target = target,
            RequestedKind = RelationshipKind.Follow,
            IdempotencyKey = "key_1"
        });

        var approved = await _service.ApproveRequestAsync("acme", request.Id!, approver, null);

        Assert.Equal(FollowRequestStatus.Approved, approved.Status);
        Assert.NotNull(approved.DecidedAt);
        Assert.Equal("p_approver", approved.DecidedBy?.Id);

        // Edge should now exist
        var edges = await _relationshipService.QueryAsync(new RelationshipQuery
        {
            TenantId = "acme",
            From = requester,
            Kind = RelationshipKind.Follow
        });
        Assert.Single(edges);
    }

    [Fact]
    public async Task DenyRequest_does_not_create_edge()
    {
        var requester = CreateProfile("p_requester");
        var target = CreateProfile("p_target");
        var approver = CreateProfile("p_approver");

        _policy.SetRequiresApproval(target);
        _policy.SetApprovers(target, approver);

        var request = await _service.CreateFollowRequestAsync(new FollowRequestDto
        {
            TenantId = "acme",
            Requester = requester,
            Target = target,
            RequestedKind = RelationshipKind.Follow,
            IdempotencyKey = "key_1"
        });

        var denied = await _service.DenyRequestAsync("acme", request.Id!, approver, null);

        Assert.Equal(FollowRequestStatus.Denied, denied.Status);

        // No edge
        var edges = await _relationshipService.QueryAsync(new RelationshipQuery
        {
            TenantId = "acme",
            From = requester,
            Kind = RelationshipKind.Follow
        });
        Assert.Empty(edges);
    }

    [Fact]
    public async Task IdempotencyKey_returns_existing_request()
    {
        var requester = CreateProfile("p_requester");
        var target = CreateProfile("p_target");

        _policy.SetRequiresApproval(target);

        var first = await _service.CreateFollowRequestAsync(new FollowRequestDto
        {
            TenantId = "acme",
            Requester = requester,
            Target = target,
            RequestedKind = RelationshipKind.Follow,
            IdempotencyKey = "same_key"
        });

        var second = await _service.CreateFollowRequestAsync(new FollowRequestDto
        {
            TenantId = "acme",
            Requester = requester,
            Target = target,
            RequestedKind = RelationshipKind.Follow,
            IdempotencyKey = "same_key"
        });

        Assert.Equal(first.Id, second.Id);
    }

    [Fact]
    public async Task CreateFollowRequest_sends_ActionReply_to_approvers()
    {
        var requester = CreateProfile("p_requester");
        var target = CreateProfile("p_target");
        var approver = CreateProfile("p_approver");

        _policy.SetRequiresApproval(target);
        _policy.SetApprovers(target, approver);

        await _service.CreateFollowRequestAsync(new FollowRequestDto
        {
            TenantId = "acme",
            Requester = requester,
            Target = target,
            RequestedKind = RelationshipKind.Follow,
            IdempotencyKey = "key_1"
        });

        // Approver should have an action-reply in inbox
        var result = await _service.QueryInboxAsync(new InboxQuery
        {
            TenantId = "acme",
            Recipients = new List<EntityRefDto> { approver }
        });

        Assert.Single(result.Items);
        Assert.Equal(InboxItemKind.Request, result.Items[0].Kind);
    }
}
