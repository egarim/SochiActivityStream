using NUnit.Framework;
using Identity.Abstractions;
using Sochi.Navigation.Navigation;
using SocialKit.Components.ViewModels;

namespace BlazorBook.Tests.ViewModels;

[TestFixture]
public class ProfileViewModelTests
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

    private async Task<string> CreateUserAndSignIn(string displayName, string handle, string email)
    {
        var signUpVm = _fixture.GetViewModel<SignUpViewModel>();
        signUpVm.DisplayName = displayName;
        signUpVm.Handle = handle;
        signUpVm.Email = email;
        signUpVm.Password = "password123";
        await signUpVm.SignUpCommand.ExecuteAsync(null);
        return _fixture.CurrentUser.ProfileId!;
    }

    [Test]
    public async Task InitializeAsync_WithOwnProfile_ShouldLoadProfile()
    {
        // Arrange: Create user
        var profileId = await CreateUserAndSignIn("Alice", "alice", "alice@test.com");
        
        var vm = _fixture.GetViewModel<ProfileViewModel>();
        
        // Act: Initialize without profileId parameter (loads own profile)
        var parameters = new NavigationParameters();
        await vm.InitializeAsync(parameters);
        
        // Assert
        Assert.That(vm.Profile, Is.Not.Null);
        Assert.That(vm.Profile!.DisplayName, Is.EqualTo("Alice"));
        Assert.That(vm.IsOwnProfile, Is.True);
    }

    [Test]
    public async Task InitializeAsync_WithOtherProfile_ShouldLoadOtherProfile()
    {
        // Arrange: Create two users
        var aliceId = await CreateUserAndSignIn("Alice", "alice", "alice@test.com");
        await _fixture.CurrentUser.SignOutAsync();
        
        var bobId = await CreateUserAndSignIn("Bob", "bob", "bob@test.com");
        
        var vm = _fixture.GetViewModel<ProfileViewModel>();
        
        // Act: Load Alice's profile while signed in as Bob
        var parameters = new NavigationParameters();
        parameters.Add("profileId", aliceId);
        await vm.InitializeAsync(parameters);
        
        // Assert
        Assert.That(vm.Profile, Is.Not.Null);
        Assert.That(vm.Profile!.DisplayName, Is.EqualTo("Alice"));
        Assert.That(vm.IsOwnProfile, Is.False);
    }

    [Test]
    public async Task FollowCommand_ShouldCreateFollowRelationship()
    {
        // Arrange: Create two users
        var aliceId = await CreateUserAndSignIn("Alice", "alice", "alice@test.com");
        await _fixture.CurrentUser.SignOutAsync();
        
        await CreateUserAndSignIn("Bob", "bob", "bob@test.com");
        
        var vm = _fixture.GetViewModel<ProfileViewModel>();
        var parameters = new NavigationParameters();
        parameters.Add("profileId", aliceId);
        await vm.InitializeAsync(parameters);
        
        Assert.That(vm.IsFollowing, Is.False);
        Assert.That(vm.FollowerCount, Is.EqualTo(0));
        
        // Act: Follow Alice
        await vm.FollowCommand.ExecuteAsync(null);
        
        // Assert
        Assert.That(vm.IsFollowing, Is.True);
        Assert.That(vm.FollowerCount, Is.EqualTo(1));
    }

    [Test]
    public async Task UnfollowCommand_ShouldRemoveFollowRelationship()
    {
        // Arrange: Create two users and follow
        var aliceId = await CreateUserAndSignIn("Alice", "alice", "alice@test.com");
        await _fixture.CurrentUser.SignOutAsync();
        
        await CreateUserAndSignIn("Bob", "bob", "bob@test.com");
        
        var vm = _fixture.GetViewModel<ProfileViewModel>();
        var parameters = new NavigationParameters();
        parameters.Add("profileId", aliceId);
        await vm.InitializeAsync(parameters);
        
        await vm.FollowCommand.ExecuteAsync(null);
        Assert.That(vm.IsFollowing, Is.True);
        Assert.That(vm.FollowerCount, Is.EqualTo(1));
        
        // Act: Unfollow Alice
        await vm.UnfollowCommand.ExecuteAsync(null);
        
        // Assert
        Assert.That(vm.IsFollowing, Is.False);
        Assert.That(vm.FollowerCount, Is.EqualTo(0));
    }

    [Test]
    public async Task FollowCommand_OnOwnProfile_CannotExecute()
    {
        // Arrange: Create user and view own profile
        await CreateUserAndSignIn("Alice", "alice", "alice@test.com");
        
        var vm = _fixture.GetViewModel<ProfileViewModel>();
        await vm.InitializeAsync(new NavigationParameters());
        
        // Assert
        Assert.That(vm.IsOwnProfile, Is.True);
        Assert.That(vm.FollowCommand.CanExecute(null), Is.False);
    }

    [Test]
    public async Task Profile_Posts_ShouldShowOnlyAuthorPosts()
    {
        // Arrange: Create user and some posts
        await CreateUserAndSignIn("Alice", "alice", "alice@test.com");
        
        var feedVm = _fixture.GetViewModel<FeedViewModel>();
        await feedVm.InitializeAsync(new NavigationParameters());
        
        feedVm.NewPostText = "Alice's first post";
        await feedVm.CreatePostCommand.ExecuteAsync(null);
        
        feedVm.NewPostText = "Alice's second post";
        await feedVm.CreatePostCommand.ExecuteAsync(null);
        
        // Act: Load Alice's profile
        var profileVm = _fixture.GetViewModel<ProfileViewModel>();
        await profileVm.InitializeAsync(new NavigationParameters());
        
        // Assert: Should show 2 posts
        Assert.That(profileVm.Posts, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task FollowerCount_ShouldReflectMultipleFollowers()
    {
        // Arrange: Create target user
        var aliceId = await CreateUserAndSignIn("Alice", "alice", "alice@test.com");
        await _fixture.CurrentUser.SignOutAsync();
        
        // Bob follows Alice
        await CreateUserAndSignIn("Bob", "bob", "bob@test.com");
        var vm1 = _fixture.GetViewModel<ProfileViewModel>();
        var params1 = new NavigationParameters();
        params1.Add("profileId", aliceId);
        await vm1.InitializeAsync(params1);
        await vm1.FollowCommand.ExecuteAsync(null);
        await _fixture.CurrentUser.SignOutAsync();
        
        // Charlie follows Alice
        await CreateUserAndSignIn("Charlie", "charlie", "charlie@test.com");
        var vm2 = _fixture.GetViewModel<ProfileViewModel>();
        var params2 = new NavigationParameters();
        params2.Add("profileId", aliceId);
        await vm2.InitializeAsync(params2);
        await vm2.FollowCommand.ExecuteAsync(null);
        
        // Assert: Alice should have 2 followers
        Assert.That(vm2.FollowerCount, Is.EqualTo(2));
    }

    [Test]
    public async Task Title_ShouldUpdateToProfileDisplayName()
    {
        // Arrange
        await CreateUserAndSignIn("Alice Smith", "alice", "alice@test.com");
        
        var vm = _fixture.GetViewModel<ProfileViewModel>();
        Assert.That(vm.Title, Is.EqualTo("Profile"));
        
        // Act
        await vm.InitializeAsync(new NavigationParameters());
        
        // Assert
        Assert.That(vm.Title, Is.EqualTo("Alice Smith"));
    }
}
