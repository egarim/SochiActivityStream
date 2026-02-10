using ActivityStream.Abstractions;
using ActivityStream.Core;
using Inbox.Abstractions;
using Inbox.Core;
using Inbox.Store.InMemory;
using RelationshipService.Abstractions;
using RelationshipService.Core;
using RelationshipService.Store.InMemory;

namespace ActivityStream.Tests.Inbox;

/// <summary>
/// Tests for policy violations: non-targetable entity throws InboxPolicyViolationException.
/// </summary>
public class PolicyViolationTests
{
    private readonly InboxNotificationService _service;
    private readonly TestGovernancePolicy _policy;

    public PolicyViolationTests()
    {
        var inboxStore = new InMemoryInboxStore();
        var requestStore = new InMemoryFollowRequestStore();
        var relationshipStore = new InMemoryRelationshipStore();
        var relationshipService = new RelationshipServiceImpl(relationshipStore, new UlidIdGenerator());
        _policy = new TestGovernancePolicy();

        _service = new InboxNotificationService(
            inboxStore,
            requestStore,
            relationshipService,
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
    public async Task FollowRequest_to_non_targetable_throws()
    {
        var requester = CreateProfile("p_requester");
        var target = CreateProfile("p_target");

        // Mark target as non-targetable
        _policy.SetNonTargetable(target);

        // Attempt to follow
        var ex = await Assert.ThrowsAsync<InboxPolicyViolationException>(async () =>
        {
            await _service.CreateFollowRequestAsync(new FollowRequestDto
            {
                TenantId = "acme",
                Requester = requester,
                Target = target,
                RequestedKind = RelationshipKind.Follow,
                IdempotencyKey = "key_1"
            });
        });

        Assert.Equal("NOT_TARGETABLE", ex.Reason);
    }

    [Fact]
    public async Task FollowRequest_to_targetable_succeeds()
    {
        var requester = CreateProfile("p_requester");
        var target = CreateProfile("p_target");

        // Target is targetable by default
        var result = await _service.CreateFollowRequestAsync(new FollowRequestDto
        {
            TenantId = "acme",
            Requester = requester,
            Target = target,
            RequestedKind = RelationshipKind.Follow,
            IdempotencyKey = "key_1"
        });

        Assert.NotNull(result);
        Assert.NotNull(result.Id);
    }
}
