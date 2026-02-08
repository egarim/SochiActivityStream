using Identity.Abstractions;
using Content.Abstractions;
using RelationshipService.Abstractions;
using Chat.Abstractions;
using Search.Abstractions;
using ContentEntityRef = Content.Abstractions.EntityRefDto;
using ChatEntityRef = Chat.Abstractions.EntityRefDto;
using RelEntityRef = ActivityStream.Abstractions.EntityRefDto;

namespace BlazorBook.Web.Data;

/// <summary>
/// Seeds demo data for investor demonstrations.
/// Creates 10 users, 50 posts, 200+ comments, reactions, and conversations.
/// </summary>
public class DemoDataSeeder
{
    private const string TenantId = "blazorbook";
    
    private readonly IAuthService _authService;
    private readonly IContentService _contentService;
    private readonly IRelationshipService _relationshipService;
    private readonly IChatService _chatService;
    private readonly ISearchIndex _searchIndex;
    private readonly IUserStore _userStore;
    private readonly ILogger<DemoDataSeeder> _logger;

    private static readonly Random _random = new(42); // Deterministic seed

    public DemoDataSeeder(
        IAuthService authService,
        IContentService contentService,
        IRelationshipService relationshipService,
        IChatService chatService,
        ISearchIndex searchIndex,
        IUserStore userStore,
        ILogger<DemoDataSeeder> logger)
    {
        _authService = authService;
        _contentService = contentService;
        _relationshipService = relationshipService;
        _chatService = chatService;
        _searchIndex = searchIndex;
        _userStore = userStore;
        _logger = logger;
    }

    /// <summary>
    /// Seeds all demo data. Safe to call multiple times - checks for existing data.
    /// </summary>
    public async Task SeedAsync()
    {
        _logger.LogInformation("Starting demo data seeding...");

        // Check if already seeded by looking for alice user
        var existingUser = await _userStore.FindByUsernameAsync("alice");
        if (existingUser != null)
        {
            _logger.LogInformation("Demo data already seeded, skipping.");
            return;
        }

        // 1. Create users and profiles
        var signUpResults = await CreateUsersAndProfilesAsync();
        _logger.LogInformation("Created {Count} demo users with profiles", signUpResults.Count);

        // 2. Create follow relationships
        await CreateFollowRelationshipsAsync(signUpResults);
        _logger.LogInformation("Created follow relationships");

        // 3. Create posts
        var posts = await CreatePostsAsync(signUpResults);
        _logger.LogInformation("Created {Count} demo posts", posts.Count);

        // 4. Create comments
        await CreateCommentsAsync(signUpResults, posts);
        _logger.LogInformation("Created demo comments");

        // 5. Create reactions
        await CreateReactionsAsync(signUpResults, posts);
        _logger.LogInformation("Created demo reactions");

        // 6. Create conversations and messages
        await CreateConversationsAsync(signUpResults);
        _logger.LogInformation("Created demo conversations");

        // 7. Index users for search
        await IndexUsersForSearchAsync(signUpResults);
        _logger.LogInformation("Indexed users for search");

        _logger.LogInformation("Demo data seeding complete!");
    }

    private async Task<List<SignUpResult>> CreateUsersAndProfilesAsync()
    {
        var users = new (string Username, string Name, string Bio)[]
        {
            ("alice", "Alice Anderson", "Software Engineer @ ACME Tech | Building the future"),
            ("bob", "Bob Builder", "Product Designer | UX Enthusiast | Design Systems Lead"),
            ("charlie", "Charlie Chen", "DevOps Engineer | Kubernetes Expert | Cloud Native"),
            ("diana", "Diana Davis", "Frontend Developer | React & Blazor | Accessibility Advocate"),
            ("eve", "Eve Evans", "Backend Specialist | .NET | Distributed Systems"),
            ("frank", "Frank Foster", "Full-Stack Developer | Open Source Contributor"),
            ("grace", "Grace Green", "Tech Lead @ StartupXYZ | Mentor | Speaker"),
            ("henry", "Henry Harris", "Mobile Developer | MAUI | Cross-platform Expert"),
            ("iris", "Iris Ivanov", "Data Engineer | ML Enthusiast | Python & SQL"),
            ("jack", "Jack Jackson", "Cloud Architect | Azure MVP | Security Focus")
        };

        var results = new List<SignUpResult>();
        
        foreach (var (username, name, bio) in users)
        {
            try
            {
                var result = await _authService.SignUpAsync(TenantId, new SignUpRequest
                {
                    Username = username,
                    Email = $"{username}@example.com",
                    Password = "Demo123!@#",
                    DisplayName = name
                });

                results.Add(result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create user {Username}", username);
            }
        }

        return results;
    }

    private async Task CreateFollowRelationshipsAsync(List<SignUpResult> signUpResults)
    {
        var profiles = signUpResults.ToDictionary(r => r.Profile.DisplayName ?? "", r => r.Profile);

        // Create a realistic social graph using UpsertAsync with RelationshipKind.Follow
        var followPatterns = new Dictionary<string, string[]>
        {
            // Alice follows everyone
            ["Alice Anderson"] = profiles.Keys.Where(k => k != "Alice Anderson").ToArray(),
            // Bob follows Alice, Charlie, Diana
            ["Bob Builder"] = new[] { "Alice Anderson", "Charlie Chen", "Diana Davis" },
            // Charlie follows Alice, Bob, Eve
            ["Charlie Chen"] = new[] { "Alice Anderson", "Bob Builder", "Eve Evans" },
            // Diana follows Alice, Grace
            ["Diana Davis"] = new[] { "Alice Anderson", "Grace Green" },
            // Eve follows Alice, Charlie, Frank
            ["Eve Evans"] = new[] { "Alice Anderson", "Charlie Chen", "Frank Foster" },
            // Everyone follows Grace (she's the tech lead/influencer)
            ["Frank Foster"] = new[] { "Grace Green" },
            ["Henry Harris"] = new[] { "Grace Green", "Alice Anderson" },
            ["Iris Ivanov"] = new[] { "Grace Green", "Eve Evans" },
            ["Jack Jackson"] = new[] { "Grace Green", "Charlie Chen" }
        };

        foreach (var (followerName, followedNames) in followPatterns)
        {
            if (!profiles.TryGetValue(followerName, out var follower)) continue;

            foreach (var followedName in followedNames)
            {
                if (!profiles.TryGetValue(followedName, out var followed)) continue;

                try
                {
                    // Create a Follow relationship edge
                    await _relationshipService.UpsertAsync(new RelationshipEdgeDto
                    {
                        TenantId = TenantId,
                        From = new RelEntityRef { Kind = "user", Type = "Profile", Id = follower.Id! },
                        To = new RelEntityRef { Kind = "user", Type = "Profile", Id = followed.Id! },
                        Kind = RelationshipKind.Follow,
                        IsActive = true
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create follow: {Follower} -> {Followed}", followerName, followedName);
                }
            }
        }
    }

    private async Task<List<PostDto>> CreatePostsAsync(List<SignUpResult> signUpResults)
    {
        var posts = new List<PostDto>();
        var postBodies = GetPostBodies();

        for (int i = 0; i < 50 && i < postBodies.Length; i++)
        {
            var author = signUpResults[_random.Next(signUpResults.Count)];
            var body = postBodies[i];

            try
            {
                var post = await _contentService.CreatePostAsync(new CreatePostRequest
                {
                    TenantId = TenantId,
                    Author = new ContentEntityRef
                    {
                        Type = "Profile",
                        Id = author.Profile.Id!,
                        DisplayName = author.Profile.DisplayName,
                        ImageUrl = author.Profile.AvatarUrl
                    },
                    Body = body,
                    Visibility = ContentVisibility.Public
                });

                posts.Add(post);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create post");
            }
        }

        return posts;
    }

    private async Task CreateCommentsAsync(List<SignUpResult> signUpResults, List<PostDto> posts)
    {
        var commentBodies = GetCommentBodies();
        int commentCount = 0;

        foreach (var post in posts)
        {
            // Each post gets 2-8 comments
            var numComments = _random.Next(2, 9);
            for (int i = 0; i < numComments && commentCount < 200; i++)
            {
                var commenter = signUpResults[_random.Next(signUpResults.Count)];
                var body = commentBodies[_random.Next(commentBodies.Length)];

                try
                {
                    await _contentService.CreateCommentAsync(new CreateCommentRequest
                    {
                        TenantId = TenantId,
                        PostId = post.Id!,
                        Author = new ContentEntityRef
                        {
                            Type = "Profile",
                            Id = commenter.Profile.Id!,
                            DisplayName = commenter.Profile.DisplayName,
                            ImageUrl = commenter.Profile.AvatarUrl
                        },
                        Body = body
                    });
                    
                    commentCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create comment on post {PostId}", post.Id);
                }
            }
        }
    }

    private async Task CreateReactionsAsync(List<SignUpResult> signUpResults, List<PostDto> posts)
    {
        foreach (var post in posts)
        {
            // Each post gets reactions from 30-80% of users
            var reactingUsers = signUpResults
                .OrderBy(_ => _random.Next())
                .Take(_random.Next(3, 8))
                .ToList();

            foreach (var reactor in reactingUsers)
            {
                var reactionType = (ReactionType)_random.Next(0, 6);

                try
                {
                    await _contentService.ReactAsync(new ReactRequest
                    {
                        TenantId = TenantId,
                        TargetId = post.Id!,
                        TargetKind = ReactionTargetKind.Post,
                        Actor = new ContentEntityRef
                        {
                            Type = "Profile",
                            Id = reactor.Profile.Id!,
                            DisplayName = reactor.Profile.DisplayName
                        },
                        Type = reactionType
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to add reaction to post {PostId}", post.Id);
                }
            }
        }
    }

    private async Task CreateConversationsAsync(List<SignUpResult> signUpResults)
    {
        var profileByFirstName = signUpResults.ToDictionary(
            r => r.Profile.DisplayName?.Split(' ')[0] ?? "",
            r => r);

        // Create some private DM conversations
        var dmPairs = new[]
        {
            ("Alice", "Bob"),
            ("Alice", "Grace"),
            ("Charlie", "Eve"),
            ("Diana", "Frank"),
            ("Grace", "Jack")
        };

        foreach (var (name1, name2) in dmPairs)
        {
            if (!profileByFirstName.TryGetValue(name1, out var user1) ||
                !profileByFirstName.TryGetValue(name2, out var user2))
                continue;

            try
            {
                // Create DM using GetOrCreateDirectConversationAsync
                var user1Ref = ChatEntityRef.Profile(user1.Profile.Id!, user1.Profile.DisplayName);
                var user2Ref = ChatEntityRef.Profile(user2.Profile.Id!, user2.Profile.DisplayName);

                var conversation = await _chatService.GetOrCreateDirectConversationAsync(
                    TenantId,
                    user1Ref,
                    user2Ref);

                // Add some messages
                var messages = GetDemoMessages(name1, name2);
                foreach (var (senderName, content) in messages)
                {
                    var sender = senderName == name1 ? user1 : user2;
                    await _chatService.SendMessageAsync(new SendMessageRequest
                    {
                        TenantId = TenantId,
                        ConversationId = conversation.Id!,
                        Sender = ChatEntityRef.Profile(sender.Profile.Id!, sender.Profile.DisplayName),
                        Body = content
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create conversation between {User1} and {User2}", name1, name2);
            }
        }

        // Create a group chat
        try
        {
            var techTeamNames = new[] { "Alice", "Charlie", "Eve", "Grace" };
            var techTeam = signUpResults
                .Where(r => techTeamNames.Any(n => r.Profile.DisplayName?.Contains(n) == true))
                .ToList();

            if (techTeam.Count >= 2)
            {
                var creator = techTeam.First();
                var participants = techTeam
                    .Select(p => ChatEntityRef.Profile(p.Profile.Id!, p.Profile.DisplayName))
                    .ToList();

                var groupChat = await _chatService.CreateGroupConversationAsync(new CreateConversationRequest
                {
                    TenantId = TenantId,
                    Creator = ChatEntityRef.Profile(creator.Profile.Id!, creator.Profile.DisplayName),
                    Type = ConversationType.Group,
                    Participants = participants,
                    Title = "Tech Team Chat"
                });

                await _chatService.SendMessageAsync(new SendMessageRequest
                {
                    TenantId = TenantId,
                    ConversationId = groupChat.Id!,
                    Sender = ChatEntityRef.Profile(creator.Profile.Id!, creator.Profile.DisplayName),
                    Body = "Hey team! Ready for the demo tomorrow?"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create group conversation");
        }
    }

    private async Task IndexUsersForSearchAsync(List<SignUpResult> signUpResults)
    {
        foreach (var result in signUpResults)
        {
            var profile = result.Profile;

            try
            {
                var doc = new SearchDocument
                {
                    Id = profile.Id!,
                    TenantId = TenantId,
                    DocumentType = "Profile",
                    TextFields = new Dictionary<string, string>
                    {
                        ["displayName"] = profile.DisplayName ?? "",
                        ["handle"] = profile.Handle ?? ""
                    },
                    KeywordFields = new Dictionary<string, List<string>>
                    {
                        ["profileId"] = new() { profile.Id! }
                    },
                    DateFields = new Dictionary<string, DateTimeOffset>
                    {
                        ["createdAt"] = profile.CreatedAt
                    },
                    Boost = 1.0,
                    SourceEntity = profile
                };

                await _searchIndex.IndexAsync(doc);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to index profile {ProfileId} for search", profile.Id);
            }
        }
    }

    #region Demo Content Data

    private static string[] GetPostBodies()
    {
        return new[]
        {
            "Just shipped a new feature! The team crushed it this sprint.",
            "Hot take: TypeScript > JavaScript for large codebases. Fight me.",
            "Anyone else excited about .NET 9 performance improvements? The runtime team is doing amazing work.",
            "Pro tip: Always write tests before your PR review. Future you will thank present you.",
            "Monday motivation: Remember, every expert was once a beginner. Keep learning!",
            "Just finished reading 'Clean Architecture' - highly recommend for any developer!",
            "The joy of debugging: spending 4 hours to find a missing semicolon.",
            "Grateful for amazing teammates who review PRs with constructive feedback.",
            "Working on optimizing our database queries. Already seeing 40% improvement!",
            "Coffee count today: 4 cups - It's going to be one of those days.",
            "Just gave my first conference talk! Nervous but so worth it.",
            "Question: What's your favorite VS Code extension? I'll start: GitLens.",
            "Finally migrated our legacy app to microservices. What a journey!",
            "Reminder: Take breaks. Your code will still be there when you get back.",
            "The new Blazor 8 features are game-changing. Auto render mode is chef's kiss.",
            "Just hit 1000 contributions on GitHub this year! Open source for the win.",
            "Code review feedback that actually helps > 'Looks good to me'",
            "Learning Rust this weekend. Borrow checker, here I come!",
            "The best documentation is the one that actually exists. Write docs, people!",
            "Celebrating 3 years at my company today! Time flies when you love what you do.",
            "Hot off the press: Our API now handles 10K requests/second. Scaling is fun!",
            "Pair programming session saved my day. Two heads really are better than one.",
            "Just discovered GitHub Copilot can explain regex. Mind = blown.",
            "Remember when we had to FTP files to deploy? DevOps has come so far.",
            "Friday deploy? We live dangerously here. YOLO (just kidding, we have rollback).",
            "The secret to good architecture: Make it easy to change later.",
            "Attended an amazing tech meetup tonight. The community here is incredible!",
            "Unit tests passing. Integration tests passing. My sanity: unknown.",
            "Finally understood monads. It only took me... 3 years.",
            "New blog post: 'Why I Switched from REST to GraphQL (and Back Again)'",
            "The amount of knowledge in this Slack channel is insane. Love this community!",
            "Just mentored a junior dev through their first PR. Proud moment!",
            "Kubernetes is not as scary as I thought. Famous last words?",
            "Performance optimization tip: Profile before you optimize. Always.",
            "Our incident response improved 70% after we started doing blameless postmortems.",
            "Accessibility is not an afterthought. Build inclusively from day one.",
            "The dopamine hit when all tests pass on the first try > everything else.",
            "Working from a coffee shop today. Productivity +100%, coffee bill +1000%.",
            "Just automated a 2-hour manual process into a 30-second script. Automation FTW!",
            "Reading about AI/ML for the first time as a backend dev. Fascinating stuff!",
            "The best code is code you don't have to write. Embrace good abstractions.",
            "Sprint retrospective done. We're getting better every iteration!",
            "Just deployed to production with zero downtime. Blue-green deployments rock!",
            "Remember: A 10x engineer makes everyone around them 2x. Lift others up!",
            "The debugger is your friend. Console.log is your anxious friend. Both help.",
            "Feature flags saved us from a production disaster today. Always have a kill switch!",
            "Just hit inbox zero for the first time this year. It won't last, but let me enjoy this.",
            "OSS maintainers deserve more appreciation. Thank you for your free labor!",
            "The codebase is finally well-documented. Is this what happiness feels like?",
            "Learning in public is the fastest way to grow. Don't be afraid to share!"
        };
    }

    private static string[] GetCommentBodies()
    {
        return new[]
        {
            "Great post! Totally agree with this.",
            "This is so true! Thanks for sharing.",
            "Congrats!",
            "I needed to hear this today. Thanks!",
            "Interesting perspective. Never thought about it that way.",
            "Love this! Keep it up!",
            "Facts!",
            "This is the way.",
            "Couldn't agree more!",
            "Amazing work! Proud of you!",
            "Thanks for sharing your journey!",
            "This is gold. Saving for later.",
            "So relatable",
            "Lessons learned the hard way, right?",
            "Inspiring stuff!",
            "What tools did you use?",
            "Can you share more details?",
            "Following for the updates!",
            "This made my day!",
            "Been there, done that!",
            "Solid advice for everyone starting out.",
            "The consistency is paying off clearly!",
            "More of this content please!",
            "Bookmarked!",
            "The tech community is the best!"
        };
    }

    private static List<(string Sender, string Content)> GetDemoMessages(string user1, string user2)
    {
        return new List<(string, string)>
        {
            (user1, $"Hey {user2}! How's it going?"),
            (user2, $"Great {user1}! Just working on that new feature."),
            (user1, "Nice! Need any help with the code review?"),
            (user2, "That would be awesome! I'll send you the PR link."),
            (user1, "Sounds good! Looking forward to it.")
        };
    }

    #endregion
}
