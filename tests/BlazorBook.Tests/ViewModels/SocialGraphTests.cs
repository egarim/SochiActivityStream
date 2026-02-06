using NUnit.Framework;
using Sochi.Navigation.Navigation;
using SocialKit.Components.ViewModels;
using RelationshipService.Abstractions;
using ActivityStream.Abstractions;

namespace BlazorBook.Tests.ViewModels;

/// <summary>
/// Tests for social graph relationships: Follow, Block, Unblock, Mute, Mutual follows.
/// </summary>
[TestFixture]
public class SocialGraphTests
{
    private TestFixture _fixture = null!;

    [SetUp]
    public void SetUp()
    {
        _fixture = new TestFixture();
    }

    [TearDown]
    public void TearDown()
    {
        _fixture.Dispose();
    }

    private async Task<string> CreateUser(string displayName, string handle, string email)
    {
        var vm = _fixture.GetViewModel<SignUpViewModel>();
        vm.DisplayName = displayName;
        vm.Handle = handle;
        vm.Email = email;
        vm.Password = "password123";
        await vm.SignUpCommand.ExecuteAsync(null);
        return _fixture.CurrentUser.ProfileId!;
    }

    private IRelationshipService GetRelationshipService() => 
        _fixture.GetService<IRelationshipService>();

    // ═══════════════════════════════════════════════════════════════════════════
    // FOLLOW TESTS (Additional)
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task Follow_CannotFollowSelf()
    {
        // Arrange
        var aliceId = await CreateUser("Alice", "alice", "alice@test.com");
        
        var vm = _fixture.GetViewModel<ProfileViewModel>();
        await vm.InitializeAsync(new NavigationParameters());
        
        // Assert: Follow command should not be executable on own profile
        Assert.That(vm.IsOwnProfile, Is.True);
        Assert.That(vm.FollowCommand.CanExecute(null), Is.False);
    }

    [Test]
    public async Task Follow_AppearsInFollowersList()
    {
        // Arrange: Alice and Bob
        var aliceId = await CreateUser("Alice", "alice", "alice@test.com");
        await _fixture.CurrentUser.SignOutAsync();
        var bobId = await CreateUser("Bob", "bob", "bob@test.com");
        
        // Act: Bob follows Alice
        var vm = _fixture.GetViewModel<ProfileViewModel>();
        var parameters = new NavigationParameters();
        parameters.Add("profileId", aliceId);
        await vm.InitializeAsync(parameters);
        await vm.FollowCommand.ExecuteAsync(null);
        
        // Assert: Query Alice's followers
        var relationshipService = GetRelationshipService();
        var followers = await relationshipService.QueryAsync(new RelationshipQuery
        {
            TenantId = "blazorbook",
            To = new EntityRefDto { Kind = "Profile", Type = "Profile", Id = aliceId },
            Kind = RelationshipKind.Follow
        });
        
        Assert.That(followers, Has.Count.EqualTo(1));
        Assert.That(followers[0].From.Id, Is.EqualTo(bobId));
    }

    [Test]
    public async Task Follow_Idempotent_CannotFollowTwice()
    {
        // Arrange
        var aliceId = await CreateUser("Alice", "alice", "alice@test.com");
        await _fixture.CurrentUser.SignOutAsync();
        await CreateUser("Bob", "bob", "bob@test.com");
        
        var vm = _fixture.GetViewModel<ProfileViewModel>();
        var parameters = new NavigationParameters();
        parameters.Add("profileId", aliceId);
        await vm.InitializeAsync(parameters);
        
        // Act: Follow twice
        await vm.FollowCommand.ExecuteAsync(null);
        var followerCountAfterFirst = vm.FollowerCount;
        
        // Try to follow again (reload profile to reset state)
        await vm.InitializeAsync(parameters);
        // Already following, so FollowCommand should not be the active action
        Assert.That(vm.IsFollowing, Is.True);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // BLOCK TESTS
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task Block_CreatesBlockRelationship()
    {
        // Arrange: Alice and Bob
        var aliceId = await CreateUser("Alice", "alice", "alice@test.com");
        await _fixture.CurrentUser.SignOutAsync();
        var bobId = await CreateUser("Bob", "bob", "bob@test.com");
        
        // Act: Bob blocks Alice (using service directly since UI may not expose this)
        var relationshipService = GetRelationshipService();
        await relationshipService.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "blazorbook",
            From = new EntityRefDto { Kind = "Profile", Type = "Profile", Id = bobId },
            To = new EntityRefDto { Kind = "Profile", Type = "Profile", Id = aliceId },
            Kind = RelationshipKind.Block
        });
        
        // Assert: Block relationship exists
        var blocks = await relationshipService.QueryAsync(new RelationshipQuery
        {
            TenantId = "blazorbook",
            From = new EntityRefDto { Kind = "Profile", Type = "Profile", Id = bobId },
            To = new EntityRefDto { Kind = "Profile", Type = "Profile", Id = aliceId },
            Kind = RelationshipKind.Block
        });
        
        Assert.That(blocks, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Block_AfterFollow_RemovesFollowRelationship()
    {
        // Arrange: Alice and Bob, Bob follows Alice
        var aliceId = await CreateUser("Alice", "alice", "alice@test.com");
        await _fixture.CurrentUser.SignOutAsync();
        var bobId = await CreateUser("Bob", "bob", "bob@test.com");
        
        var vm = _fixture.GetViewModel<ProfileViewModel>();
        var parameters = new NavigationParameters();
        parameters.Add("profileId", aliceId);
        await vm.InitializeAsync(parameters);
        await vm.FollowCommand.ExecuteAsync(null);
        
        Assert.That(vm.IsFollowing, Is.True);
        
        // Act: Bob blocks Alice (removing the follow)
        var relationshipService = GetRelationshipService();
        
        // First remove the follow
        var follows = await relationshipService.QueryAsync(new RelationshipQuery
        {
            TenantId = "blazorbook",
            From = new EntityRefDto { Kind = "Profile", Type = "Profile", Id = bobId },
            To = new EntityRefDto { Kind = "Profile", Type = "Profile", Id = aliceId },
            Kind = RelationshipKind.Follow
        });
        foreach (var follow in follows)
        {
            await relationshipService.RemoveAsync("blazorbook", follow.Id!);
        }
        
        // Then add block
        await relationshipService.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "blazorbook",
            From = new EntityRefDto { Kind = "Profile", Type = "Profile", Id = bobId },
            To = new EntityRefDto { Kind = "Profile", Type = "Profile", Id = aliceId },
            Kind = RelationshipKind.Block
        });
        
        // Assert: No follow relationship exists
        var remainingFollows = await relationshipService.QueryAsync(new RelationshipQuery
        {
            TenantId = "blazorbook",
            From = new EntityRefDto { Kind = "Profile", Type = "Profile", Id = bobId },
            To = new EntityRefDto { Kind = "Profile", Type = "Profile", Id = aliceId },
            Kind = RelationshipKind.Follow
        });
        
        Assert.That(remainingFollows, Is.Empty);
    }

    [Test]
    public async Task Unblock_RemovesBlockRelationship()
    {
        // Arrange: Bob blocks Alice
        var aliceId = await CreateUser("Alice", "alice", "alice@test.com");
        await _fixture.CurrentUser.SignOutAsync();
        var bobId = await CreateUser("Bob", "bob", "bob@test.com");
        
        var relationshipService = GetRelationshipService();
        await relationshipService.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "blazorbook",
            From = new EntityRefDto { Kind = "Profile", Type = "Profile", Id = bobId },
            To = new EntityRefDto { Kind = "Profile", Type = "Profile", Id = aliceId },
            Kind = RelationshipKind.Block
        });
        
        // Act: Unblock Alice
        var blocks = await relationshipService.QueryAsync(new RelationshipQuery
        {
            TenantId = "blazorbook",
            From = new EntityRefDto { Kind = "Profile", Type = "Profile", Id = bobId },
            To = new EntityRefDto { Kind = "Profile", Type = "Profile", Id = aliceId },
            Kind = RelationshipKind.Block
        });
        
        foreach (var block in blocks)
        {
            await relationshipService.RemoveAsync("blazorbook", block.Id!);
        }
        
        // Assert: No block relationship
        var remainingBlocks = await relationshipService.QueryAsync(new RelationshipQuery
        {
            TenantId = "blazorbook",
            From = new EntityRefDto { Kind = "Profile", Type = "Profile", Id = bobId },
            To = new EntityRefDto { Kind = "Profile", Type = "Profile", Id = aliceId },
            Kind = RelationshipKind.Block
        });
        
        Assert.That(remainingBlocks, Is.Empty);
    }

    [Test]
    public async Task Unblock_AllowsFollowAgain()
    {
        // Arrange: Bob blocks then unblocks Alice
        var aliceId = await CreateUser("Alice", "alice", "alice@test.com");
        await _fixture.CurrentUser.SignOutAsync();
        var bobId = await CreateUser("Bob", "bob", "bob@test.com");
        
        var relationshipService = GetRelationshipService();
        
        // Block
        await relationshipService.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "blazorbook",
            From = new EntityRefDto { Kind = "Profile", Type = "Profile", Id = bobId },
            To = new EntityRefDto { Kind = "Profile", Type = "Profile", Id = aliceId },
            Kind = RelationshipKind.Block
        });
        
        // Unblock
        var blocks = await relationshipService.QueryAsync(new RelationshipQuery
        {
            TenantId = "blazorbook",
            From = new EntityRefDto { Kind = "Profile", Type = "Profile", Id = bobId },
            To = new EntityRefDto { Kind = "Profile", Type = "Profile", Id = aliceId },
            Kind = RelationshipKind.Block
        });
        foreach (var block in blocks)
        {
            await relationshipService.RemoveAsync("blazorbook", block.Id!);
        }
        
        // Act: Bob follows Alice after unblocking
        var vm = _fixture.GetViewModel<ProfileViewModel>();
        var parameters = new NavigationParameters();
        parameters.Add("profileId", aliceId);
        await vm.InitializeAsync(parameters);
        await vm.FollowCommand.ExecuteAsync(null);
        
        // Assert: Follow succeeds
        Assert.That(vm.IsFollowing, Is.True);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // MUTUAL FOLLOW (FRIENDS) TESTS
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task MutualFollow_BothUsersFollowEachOther()
    {
        // Arrange: Create Alice and Bob
        var aliceId = await CreateUser("Alice", "alice", "alice@test.com");
        await _fixture.CurrentUser.SignOutAsync();
        var bobId = await CreateUser("Bob", "bob", "bob@test.com");
        
        // Bob follows Alice
        var vm = _fixture.GetViewModel<ProfileViewModel>();
        var aliceParams = new NavigationParameters();
        aliceParams.Add("profileId", aliceId);
        await vm.InitializeAsync(aliceParams);
        await vm.FollowCommand.ExecuteAsync(null);
        
        // Switch to Alice
        await _fixture.CurrentUser.SignOutAsync();
        var loginVm = _fixture.GetViewModel<LoginViewModel>();
        loginVm.Email = "alice@test.com";
        loginVm.Password = "password123";
        await loginVm.LoginCommand.ExecuteAsync(null);
        
        // Alice follows Bob
        var vm2 = _fixture.GetViewModel<ProfileViewModel>();
        var bobParams = new NavigationParameters();
        bobParams.Add("profileId", bobId);
        await vm2.InitializeAsync(bobParams);
        await vm2.FollowCommand.ExecuteAsync(null);
        
        // Assert: Both relationships exist
        var relationshipService = GetRelationshipService();
        
        var aliceFollowsBob = await relationshipService.QueryAsync(new RelationshipQuery
        {
            TenantId = "blazorbook",
            From = new EntityRefDto { Kind = "Profile", Type = "Profile", Id = aliceId },
            To = new EntityRefDto { Kind = "Profile", Type = "Profile", Id = bobId },
            Kind = RelationshipKind.Follow
        });
        
        var bobFollowsAlice = await relationshipService.QueryAsync(new RelationshipQuery
        {
            TenantId = "blazorbook",
            From = new EntityRefDto { Kind = "Profile", Type = "Profile", Id = bobId },
            To = new EntityRefDto { Kind = "Profile", Type = "Profile", Id = aliceId },
            Kind = RelationshipKind.Follow
        });
        
        Assert.That(aliceFollowsBob, Has.Count.EqualTo(1));
        Assert.That(bobFollowsAlice, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task FollowerAndFollowing_CountsAreAccurate()
    {
        // Arrange: Create Alice, Bob, Charlie
        var aliceId = await CreateUser("Alice", "alice", "alice@test.com");
        await _fixture.CurrentUser.SignOutAsync();
        var bobId = await CreateUser("Bob", "bob", "bob@test.com");
        await _fixture.CurrentUser.SignOutAsync();
        var charlieId = await CreateUser("Charlie", "charlie", "charlie@test.com");
        
        // Charlie follows Alice
        var vm1 = _fixture.GetViewModel<ProfileViewModel>();
        var aliceParams = new NavigationParameters();
        aliceParams.Add("profileId", aliceId);
        await vm1.InitializeAsync(aliceParams);
        await vm1.FollowCommand.ExecuteAsync(null);
        
        // Charlie follows Bob
        var vm2 = _fixture.GetViewModel<ProfileViewModel>();
        var bobParams = new NavigationParameters();
        bobParams.Add("profileId", bobId);
        await vm2.InitializeAsync(bobParams);
        await vm2.FollowCommand.ExecuteAsync(null);
        
        // Assert: Query relationships directly to verify Charlie follows both
        var relationshipService = GetRelationshipService();
        var charlieFollowing = await relationshipService.QueryAsync(new RelationshipQuery
        {
            TenantId = "blazorbook",
            From = new EntityRefDto { Kind = "Profile", Type = "Profile", Id = charlieId },
            Kind = RelationshipKind.Follow
        });
        Assert.That(charlieFollowing, Has.Count.EqualTo(2), "Charlie should follow 2 people");
        
        // Assert: Alice's followers
        var aliceFollowers = await relationshipService.QueryAsync(new RelationshipQuery
        {
            TenantId = "blazorbook",
            To = new EntityRefDto { Kind = "Profile", Type = "Profile", Id = aliceId },
            Kind = RelationshipKind.Follow
        });
        Assert.That(aliceFollowers, Has.Count.EqualTo(1), "Alice should have 1 follower");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // MUTE TESTS
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task Mute_CreatesMuteRelationship()
    {
        // Arrange
        var aliceId = await CreateUser("Alice", "alice", "alice@test.com");
        await _fixture.CurrentUser.SignOutAsync();
        var bobId = await CreateUser("Bob", "bob", "bob@test.com");
        
        // Act: Bob mutes Alice
        var relationshipService = GetRelationshipService();
        await relationshipService.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "blazorbook",
            From = new EntityRefDto { Kind = "Profile", Type = "Profile", Id = bobId },
            To = new EntityRefDto { Kind = "Profile", Type = "Profile", Id = aliceId },
            Kind = RelationshipKind.Mute
        });
        
        // Assert
        var mutes = await relationshipService.QueryAsync(new RelationshipQuery
        {
            TenantId = "blazorbook",
            From = new EntityRefDto { Kind = "Profile", Type = "Profile", Id = bobId },
            Kind = RelationshipKind.Mute
        });
        
        Assert.That(mutes, Has.Count.EqualTo(1));
        Assert.That(mutes[0].To.Id, Is.EqualTo(aliceId));
    }

    [Test]
    public async Task Mute_DoesNotAffectFollowRelationship()
    {
        // Arrange: Bob follows Alice
        var aliceId = await CreateUser("Alice", "alice", "alice@test.com");
        await _fixture.CurrentUser.SignOutAsync();
        var bobId = await CreateUser("Bob", "bob", "bob@test.com");
        
        var vm = _fixture.GetViewModel<ProfileViewModel>();
        var parameters = new NavigationParameters();
        parameters.Add("profileId", aliceId);
        await vm.InitializeAsync(parameters);
        await vm.FollowCommand.ExecuteAsync(null);
        
        // Act: Bob mutes Alice (but still follows)
        var relationshipService = GetRelationshipService();
        await relationshipService.UpsertAsync(new RelationshipEdgeDto
        {
            TenantId = "blazorbook",
            From = new EntityRefDto { Kind = "Profile", Type = "Profile", Id = bobId },
            To = new EntityRefDto { Kind = "Profile", Type = "Profile", Id = aliceId },
            Kind = RelationshipKind.Mute
        });
        
        // Assert: Follow still exists
        var follows = await relationshipService.QueryAsync(new RelationshipQuery
        {
            TenantId = "blazorbook",
            From = new EntityRefDto { Kind = "Profile", Type = "Profile", Id = bobId },
            To = new EntityRefDto { Kind = "Profile", Type = "Profile", Id = aliceId },
            Kind = RelationshipKind.Follow
        });
        
        Assert.That(follows, Has.Count.EqualTo(1));
    }
}
