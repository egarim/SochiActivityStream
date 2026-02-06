using ActivityStream.Abstractions;
using ActivityStream.Core;
using ActivityStream.Store.InMemory;
using Chat.Abstractions;
using Chat.Core;
using Chat.Store.InMemory;
using Content.Abstractions;
using Content.Core;
using Content.Store.InMemory;
using Identity.Abstractions;
using Identity.Core;
using Identity.Store.InMemory;
using Inbox.Abstractions;
using Inbox.Core;
using Inbox.Store.InMemory;
using Media.Abstractions;
using Media.Core;
using Media.Store.InMemory;
using Realtime.Abstractions;
using Realtime.Core;
using Realtime.Transport.InMemory;
using RelationshipService.Abstractions;
using RelationshipService.Core;
using RelationshipService.Store.InMemory;
using Search.Abstractions;
using Search.Core;
using Search.Index.InMemory;

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════════════════════════
// IDENTITY SERVICE
// ═══════════════════════════════════════════════════════════════════════════════
builder.Services.AddSingleton<IUserStore, InMemoryUserStore>();
builder.Services.AddSingleton<IProfileStore, InMemoryProfileStore>();
builder.Services.AddSingleton<IMembershipStore, InMemoryMembershipStore>();
builder.Services.AddSingleton<ISessionStore, InMemorySessionStore>();
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddSingleton<Identity.Abstractions.IIdGenerator, Identity.Core.UlidIdGenerator>();
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<IProfileService, ProfileService>();

// ═══════════════════════════════════════════════════════════════════════════════
// CONTENT SERVICE (Posts, Comments, Reactions)
// ═══════════════════════════════════════════════════════════════════════════════
builder.Services.AddSingleton<IPostStore, InMemoryPostStore>();
builder.Services.AddSingleton<ICommentStore, InMemoryCommentStore>();
builder.Services.AddSingleton<IReactionStore, InMemoryReactionStore>();
builder.Services.AddSingleton<Content.Abstractions.IIdGenerator, Content.Core.UlidIdGenerator>();
builder.Services.AddSingleton<IContentService, ContentService>();

// ═══════════════════════════════════════════════════════════════════════════════
// CHAT SERVICE
// ═══════════════════════════════════════════════════════════════════════════════
builder.Services.AddSingleton<IConversationStore, InMemoryConversationStore>();
builder.Services.AddSingleton<IMessageStore, InMemoryMessageStore>();
builder.Services.AddSingleton<IChatNotifier, NullChatNotifier>();
builder.Services.AddSingleton<Chat.Abstractions.IIdGenerator, Chat.Core.UlidIdGenerator>();
builder.Services.AddSingleton<IChatService, ChatService>();

// ═══════════════════════════════════════════════════════════════════════════════
// MEDIA SERVICE
// ═══════════════════════════════════════════════════════════════════════════════
builder.Services.AddSingleton<IMediaStore, InMemoryMediaStore>();
builder.Services.AddSingleton<IMediaStorageProvider, MockStorageProvider>();
builder.Services.AddSingleton<ActivityStream.Abstractions.IIdGenerator, ActivityStream.Core.UlidIdGenerator>();
builder.Services.AddSingleton<IMediaService, MediaService>();

// ═══════════════════════════════════════════════════════════════════════════════
// ACTIVITY STREAM SERVICE
// ═══════════════════════════════════════════════════════════════════════════════
builder.Services.AddSingleton<IActivityStore, InMemoryActivityStore>();
builder.Services.AddSingleton<IActivityValidator, DefaultActivityValidator>();
builder.Services.AddSingleton<IActivityStreamService, ActivityStreamService>();

// ═══════════════════════════════════════════════════════════════════════════════
// RELATIONSHIP SERVICE
// ═══════════════════════════════════════════════════════════════════════════════
builder.Services.AddSingleton<IRelationshipStore, InMemoryRelationshipStore>();
builder.Services.AddSingleton<IRelationshipService, RelationshipServiceImpl>();

// ═══════════════════════════════════════════════════════════════════════════════
// INBOX / NOTIFICATIONS SERVICE
// ═══════════════════════════════════════════════════════════════════════════════
builder.Services.AddSingleton<IInboxStore, InMemoryInboxStore>();
builder.Services.AddSingleton<IInboxNotificationService, InboxNotificationService>();

// ═══════════════════════════════════════════════════════════════════════════════
// REALTIME SERVICE
// ═══════════════════════════════════════════════════════════════════════════════
builder.Services.AddSingleton<IConnectionManager, InMemoryConnectionManager>();
builder.Services.AddSingleton<IEventDispatcher, InMemoryEventDispatcher>();
builder.Services.AddSingleton<IPresenceTracker, PresenceTrackerCore>();
builder.Services.AddSingleton<IGroupMembershipResolver, NullGroupMembershipResolver>();
builder.Services.AddSingleton<IRealtimePublisher, RealtimePublisher>();

// ═══════════════════════════════════════════════════════════════════════════════
// SEARCH SERVICE
// ═══════════════════════════════════════════════════════════════════════════════
builder.Services.AddSingleton<ITextAnalyzer, SimpleTextAnalyzer>();
builder.Services.AddSingleton<ISearchIndex, InMemorySearchIndex>();
builder.Services.AddSingleton<ISearchIndexer>(sp => (ISearchIndexer)sp.GetRequiredService<ISearchIndex>());
builder.Services.AddSingleton<ISearchService>(sp => (ISearchService)sp.GetRequiredService<ISearchIndex>());

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "BlazorBook API", Version = "v1", Description = "A Facebook-like social network demo 📘" });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "BlazorBook API v1"));

// ═══════════════════════════════════════════════════════════════════════════════
// AUTH ENDPOINTS
// ═══════════════════════════════════════════════════════════════════════════════
var authGroup = app.MapGroup("/api/auth").WithTags("Authentication");

authGroup.MapPost("/signup", async (SignUpRequest request, IAuthService auth) =>
{
    var result = await auth.SignUpAsync("blazorbook", request);
    return Results.Created($"/api/profiles/{result.Profile.Id}", new
    {
        User = result.User,
        Profile = result.Profile,
        Message = "Welcome to BlazorBook! 📘"
    });
}).WithName("SignUp").WithOpenApi();

authGroup.MapPost("/signin", async (SignInRequest request, IAuthService auth) =>
{
    var session = await auth.SignInAsync("blazorbook", request);
    return Results.Ok(new { Session = session, Message = "Signed in successfully!" });
}).WithName("SignIn").WithOpenApi();

authGroup.MapPost("/signout/{sessionId}", async (string sessionId, IAuthService auth) =>
{
    await auth.SignOutAsync(sessionId);
    return Results.Ok(new { Message = "Signed out successfully!" });
}).WithName("SignOut").WithOpenApi();

// ═══════════════════════════════════════════════════════════════════════════════
// PROFILE ENDPOINTS
// ═══════════════════════════════════════════════════════════════════════════════
var profileGroup = app.MapGroup("/api/profiles").WithTags("Profiles");

profileGroup.MapGet("/{profileId}", async (string profileId, IProfileStore profileStore) =>
{
    var profile = await profileStore.GetByIdAsync(profileId);
    return profile is not null ? Results.Ok(ToProfileDto(profile)) : Results.NotFound();
}).WithName("GetProfile").WithOpenApi();

profileGroup.MapGet("/handle/{handle}", async (string handle, IProfileStore profileStore) =>
{
    var profile = await profileStore.FindByHandleAsync(handle.ToLowerInvariant());
    return profile is not null ? Results.Ok(ToProfileDto(profile)) : Results.NotFound();
}).WithName("GetProfileByHandle").WithOpenApi();

// ═══════════════════════════════════════════════════════════════════════════════
// POST ENDPOINTS
// ═══════════════════════════════════════════════════════════════════════════════
var postGroup = app.MapGroup("/api/posts").WithTags("Posts");

postGroup.MapPost("/", async (CreatePostApiRequest request, IContentService content) =>
{
    var post = await content.CreatePostAsync(new CreatePostRequest
    {
        TenantId = "blazorbook",
        Author = new Content.Abstractions.EntityRefDto 
        { 
            Type = "Profile", 
            Id = request.AuthorId, 
            DisplayName = request.AuthorName 
        },
        Body = request.Body,
        Visibility = request.Visibility
    });
    return Results.Created($"/api/posts/{post.Id}", post);
}).WithName("CreatePost").WithOpenApi();

postGroup.MapGet("/{postId}", async (string postId, string? viewerId, IContentService content) =>
{
    Content.Abstractions.EntityRefDto? viewer = viewerId is not null 
        ? new Content.Abstractions.EntityRefDto { Type = "Profile", Id = viewerId } 
        : null;
    var post = await content.GetPostAsync("blazorbook", postId, viewer);
    return post is not null ? Results.Ok(post) : Results.NotFound();
}).WithName("GetPost").WithOpenApi();

postGroup.MapGet("/", async (string? authorId, int? limit, IContentService content) =>
{
    var query = new PostQuery
    {
        TenantId = "blazorbook",
        Author = authorId is not null ? new Content.Abstractions.EntityRefDto { Type = "Profile", Id = authorId } : null,
        Limit = limit ?? 20
    };
    var result = await content.QueryPostsAsync(query);
    return Results.Ok(result);
}).WithName("QueryPosts").WithOpenApi();

postGroup.MapDelete("/{postId}", async (string postId, string actorId, IContentService content) =>
{
    await content.DeletePostAsync(new DeletePostRequest
    {
        TenantId = "blazorbook",
        PostId = postId,
        Actor = new Content.Abstractions.EntityRefDto { Type = "Profile", Id = actorId }
    });
    return Results.NoContent();
}).WithName("DeletePost").WithOpenApi();

// ═══════════════════════════════════════════════════════════════════════════════
// COMMENT ENDPOINTS
// ═══════════════════════════════════════════════════════════════════════════════
var commentGroup = app.MapGroup("/api/comments").WithTags("Comments");

commentGroup.MapPost("/", async (CreateCommentApiRequest request, IContentService content) =>
{
    var comment = await content.CreateCommentAsync(new CreateCommentRequest
    {
        TenantId = "blazorbook",
        PostId = request.PostId,
        ParentCommentId = request.ParentCommentId,
        Author = new Content.Abstractions.EntityRefDto 
        { 
            Type = "Profile", 
            Id = request.AuthorId, 
            DisplayName = request.AuthorName 
        },
        Body = request.Body
    });
    return Results.Created($"/api/comments/{comment.Id}", comment);
}).WithName("CreateComment").WithOpenApi();

commentGroup.MapGet("/post/{postId}", async (string postId, string? parentCommentId, int? limit, IContentService content) =>
{
    var result = await content.QueryCommentsAsync(new CommentQuery
    {
        TenantId = "blazorbook",
        PostId = postId,
        ParentCommentId = parentCommentId,
        Limit = limit ?? 20
    });
    return Results.Ok(result);
}).WithName("GetPostComments").WithOpenApi();

// ═══════════════════════════════════════════════════════════════════════════════
// REACTION ENDPOINTS (Like, Love, Haha, Wow, Sad, Angry)
// ═══════════════════════════════════════════════════════════════════════════════
var reactionGroup = app.MapGroup("/api/reactions").WithTags("Reactions");

reactionGroup.MapPost("/", async (ReactApiRequest request, IContentService content) =>
{
    var reaction = await content.ReactAsync(new ReactRequest
    {
        TenantId = "blazorbook",
        Actor = new Content.Abstractions.EntityRefDto 
        { 
            Type = "Profile", 
            Id = request.ActorId, 
            DisplayName = request.ActorName 
        },
        TargetId = request.TargetId,
        TargetKind = request.TargetKind,
        Type = request.Type
    });
    return Results.Ok(reaction);
}).WithName("React").WithOpenApi();

reactionGroup.MapDelete("/{targetId}", async (
    string targetId, 
    ReactionTargetKind targetKind, 
    string actorId, 
    IContentService content) =>
{
    await content.RemoveReactionAsync(new RemoveReactionRequest
    {
        TenantId = "blazorbook",
        TargetId = targetId,
        TargetKind = targetKind,
        Actor = new Content.Abstractions.EntityRefDto { Type = "Profile", Id = actorId }
    });
    return Results.NoContent();
}).WithName("RemoveReaction").WithOpenApi();

// ═══════════════════════════════════════════════════════════════════════════════
// RELATIONSHIP ENDPOINTS (Follow, Block, Mute, Friends)
// ═══════════════════════════════════════════════════════════════════════════════
var relationshipGroup = app.MapGroup("/api/relationships").WithTags("Relationships");

relationshipGroup.MapPost("/follow", async (FollowApiRequest request, IRelationshipService relationships) =>
{
    var edge = await relationships.UpsertAsync(new RelationshipEdgeDto
    {
        TenantId = "blazorbook",
        From = new ActivityStream.Abstractions.EntityRefDto { Kind = "user", Type = "Profile", Id = request.FollowerId },
        To = new ActivityStream.Abstractions.EntityRefDto { Kind = "user", Type = "Profile", Id = request.FolloweeId },
        Kind = RelationshipKind.Follow
    });
    return Results.Ok(new { Message = "Now following!", Edge = edge });
}).WithName("Follow").WithOpenApi();

relationshipGroup.MapDelete("/follow/{followerId}/{followeeId}", async (
    string followerId, 
    string followeeId, 
    IRelationshipService relationships) =>
{
    var edge = await relationships.GetEdgeAsync(
        "blazorbook",
        new ActivityStream.Abstractions.EntityRefDto { Kind = "user", Type = "Profile", Id = followerId },
        new ActivityStream.Abstractions.EntityRefDto { Kind = "user", Type = "Profile", Id = followeeId },
        RelationshipKind.Follow);
    
    if (edge is not null)
    {
        await relationships.RemoveAsync("blazorbook", edge.Id!);
    }
    return Results.NoContent();
}).WithName("Unfollow").WithOpenApi();

relationshipGroup.MapGet("/mutual-friends/{userId1}/{userId2}", async (
    string userId1, 
    string userId2, 
    int? limit,
    IRelationshipService relationships) =>
{
    var mutuals = await relationships.GetMutualRelationshipsAsync(
        "blazorbook",
        new ActivityStream.Abstractions.EntityRefDto { Kind = "user", Type = "Profile", Id = userId1 },
        new ActivityStream.Abstractions.EntityRefDto { Kind = "user", Type = "Profile", Id = userId2 },
        RelationshipKind.Follow,
        limit ?? 50);
    return Results.Ok(new { MutualFriends = mutuals, Count = mutuals.Count });
}).WithName("GetMutualFriends").WithOpenApi();

relationshipGroup.MapGet("/are-friends/{userId1}/{userId2}", async (
    string userId1, 
    string userId2, 
    IRelationshipService relationships) =>
{
    var areMutual = await relationships.AreMutualAsync(
        "blazorbook",
        new ActivityStream.Abstractions.EntityRefDto { Kind = "user", Type = "Profile", Id = userId1 },
        new ActivityStream.Abstractions.EntityRefDto { Kind = "user", Type = "Profile", Id = userId2 },
        RelationshipKind.Follow);
    return Results.Ok(new { AreFriends = areMutual });
}).WithName("AreFriends").WithOpenApi();

// ═══════════════════════════════════════════════════════════════════════════════
// CHAT ENDPOINTS
// ═══════════════════════════════════════════════════════════════════════════════
var chatGroup = app.MapGroup("/api/chat").WithTags("Chat");

chatGroup.MapPost("/conversations/direct", async (DirectConversationApiRequest request, IChatService chat) =>
{
    var conv = await chat.GetOrCreateDirectConversationAsync(
        "blazorbook",
        Chat.Abstractions.EntityRefDto.Profile(request.User1Id, request.User1Name),
        Chat.Abstractions.EntityRefDto.Profile(request.User2Id, request.User2Name));
    return Results.Ok(conv);
}).WithName("GetOrCreateDirectConversation").WithOpenApi();

chatGroup.MapPost("/conversations/group", async (CreateGroupConversationApiRequest request, IChatService chat) =>
{
    var conv = await chat.CreateGroupConversationAsync(new CreateConversationRequest
    {
        TenantId = "blazorbook",
        Title = request.Title,
        Creator = Chat.Abstractions.EntityRefDto.Profile(request.CreatorId, request.CreatorName),
        Participants = request.Participants.Select(p => 
            Chat.Abstractions.EntityRefDto.Profile(p.Id, p.Name)).ToList()
    });
    return Results.Created($"/api/chat/conversations/{conv.Id}", conv);
}).WithName("CreateGroupConversation").WithOpenApi();

chatGroup.MapGet("/conversations/{conversationId}", async (
    string conversationId, 
    string viewerId, 
    IChatService chat) =>
{
    var conv = await chat.GetConversationAsync(
        "blazorbook", 
        conversationId, 
        Chat.Abstractions.EntityRefDto.Profile(viewerId, ""));
    return conv is not null ? Results.Ok(conv) : Results.NotFound();
}).WithName("GetConversation").WithOpenApi();

chatGroup.MapGet("/conversations", async (string participantId, int? limit, IChatService chat) =>
{
    var result = await chat.GetConversationsAsync(new ConversationQuery
    {
        TenantId = "blazorbook",
        Participant = Chat.Abstractions.EntityRefDto.Profile(participantId, ""),
        Limit = limit ?? 20
    });
    return Results.Ok(result);
}).WithName("GetConversations").WithOpenApi();

chatGroup.MapPost("/messages", async (SendMessageApiRequest request, IChatService chat) =>
{
    var msg = await chat.SendMessageAsync(new SendMessageRequest
    {
        TenantId = "blazorbook",
        ConversationId = request.ConversationId,
        Sender = Chat.Abstractions.EntityRefDto.Profile(request.SenderId, request.SenderName),
        Body = request.Body
    });
    return Results.Created($"/api/chat/messages/{msg.Id}", msg);
}).WithName("SendMessage").WithOpenApi();

chatGroup.MapGet("/messages/{conversationId}", async (
    string conversationId, 
    string viewerId, 
    int? limit, 
    IChatService chat) =>
{
    var result = await chat.GetMessagesAsync(new MessageQuery
    {
        TenantId = "blazorbook",
        ConversationId = conversationId,
        Viewer = Chat.Abstractions.EntityRefDto.Profile(viewerId, ""),
        Limit = limit ?? 50
    });
    return Results.Ok(result);
}).WithName("GetMessages").WithOpenApi();

chatGroup.MapPost("/read", async (MarkReadApiRequest request, IChatService chat) =>
{
    await chat.MarkReadAsync(new MarkReadRequest
    {
        TenantId = "blazorbook",
        ConversationId = request.ConversationId,
        Profile = Chat.Abstractions.EntityRefDto.Profile(request.ParticipantId, ""),
        MessageId = request.MessageId
    });
    return Results.Ok(new { Message = "Marked as read" });
}).WithName("MarkRead").WithOpenApi();

// ═══════════════════════════════════════════════════════════════════════════════
// FEED / ACTIVITY STREAM ENDPOINTS
// ═══════════════════════════════════════════════════════════════════════════════
var feedGroup = app.MapGroup("/api/feed").WithTags("Feed");

feedGroup.MapGet("/{profileId}", async (string profileId, int? limit, IActivityStreamService activities) =>
{
    var result = await activities.QueryAsync(new ActivityQuery
    {
        TenantId = "blazorbook",
        Actor = new ActivityStream.Abstractions.EntityRefDto { Kind = "user", Type = "Profile", Id = profileId },
        Limit = limit ?? 20
    });
    return Results.Ok(result);
}).WithName("GetFeed").WithOpenApi();

feedGroup.MapPost("/", async (CreateActivityApiRequest request, IActivityStreamService activities) =>
{
    var activity = await activities.PublishAsync(new ActivityDto
    {
        TenantId = "blazorbook",
        TypeKey = request.TypeKey,
        Actor = new ActivityStream.Abstractions.EntityRefDto 
        { 
            Kind = "user",
            Type = "Profile", 
            Id = request.ActorId, 
            Display = request.ActorName 
        },
        OccurredAt = DateTimeOffset.UtcNow,
        Summary = request.Summary,
        Payload = request.Payload
    });
    return Results.Created($"/api/feed/activity/{activity.Id}", activity);
}).WithName("PublishActivity").WithOpenApi();

// ═══════════════════════════════════════════════════════════════════════════════
// NOTIFICATIONS / INBOX ENDPOINTS
// ═══════════════════════════════════════════════════════════════════════════════
var inboxGroup = app.MapGroup("/api/inbox").WithTags("Inbox");

inboxGroup.MapGet("/{recipientId}", async (string recipientId, int? limit, IInboxNotificationService inbox) =>
{
    var result = await inbox.QueryInboxAsync(new InboxQuery
    {
        TenantId = "blazorbook",
        Recipients = [new ActivityStream.Abstractions.EntityRefDto { Kind = "user", Type = "Profile", Id = recipientId }],
        Limit = limit ?? 20
    });
    return Results.Ok(result);
}).WithName("GetInbox").WithOpenApi();

inboxGroup.MapPost("/mark-read/{itemId}", async (string itemId, IInboxNotificationService inbox) =>
{
    await inbox.MarkReadAsync("blazorbook", itemId);
    return Results.Ok(new { Message = "Marked as read" });
}).WithName("MarkNotificationRead").WithOpenApi();

// ═══════════════════════════════════════════════════════════════════════════════
// SEARCH ENDPOINTS
// ═══════════════════════════════════════════════════════════════════════════════
var searchGroup = app.MapGroup("/api/search").WithTags("Search");

searchGroup.MapGet("/", async (string q, string? type, int? limit, ISearchService search) =>
{
    var request = new SearchRequest
    {
        TenantId = "blazorbook",
        Query = q,
        Limit = limit ?? 20
    };
    if (type is not null)
    {
        request.DocumentTypes.Add(type);
    }
    var result = await search.SearchAsync(request);
    return Results.Ok(result);
}).WithName("Search").WithOpenApi();

searchGroup.MapGet("/autocomplete", async (string prefix, string? type, int? limit, ISearchService search) =>
{
    var request = new AutocompleteRequest
    {
        TenantId = "blazorbook",
        Prefix = prefix,
        Limit = limit ?? 10
    };
    if (type is not null)
    {
        request.DocumentTypes.Add(type);
    }
    var result = await search.AutocompleteAsync(request);
    return Results.Ok(result);
}).WithName("Autocomplete").WithOpenApi();

searchGroup.MapPost("/index", async (IndexDocumentApiRequest request, ISearchIndexer indexer) =>
{
    var doc = new SearchDocument
    {
        TenantId = "blazorbook",
        DocumentType = request.DocumentType,
        Id = request.Id
    };
    if (request.Title is not null)
        doc.TextFields["title"] = request.Title;
    if (request.Body is not null)
        doc.TextFields["body"] = request.Body;
    
    await indexer.IndexAsync(doc);
    return Results.Ok(new { Message = "Document indexed" });
}).WithName("IndexDocument").WithOpenApi();

// ═══════════════════════════════════════════════════════════════════════════════
// HOME ENDPOINT
// ═══════════════════════════════════════════════════════════════════════════════
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.MapGet("/api/health", () => Results.Ok(new 
{ 
    Status = "Healthy", 
    App = "BlazorBook",
    Version = "1.0.0",
    Message = "Welcome to BlazorBook - Where friends connect! 📘"
})).WithName("HealthCheck").WithTags("Health").WithOpenApi();

app.Run();

// ═══════════════════════════════════════════════════════════════════════════════
// HELPER METHODS
// ═══════════════════════════════════════════════════════════════════════════════
static ProfileDto ToProfileDto(ProfileRecord record) => record.Profile;

// ═══════════════════════════════════════════════════════════════════════════════
// API REQUEST MODELS
// ═══════════════════════════════════════════════════════════════════════════════

public record CreatePostApiRequest(string AuthorId, string AuthorName, string Body, ContentVisibility Visibility = ContentVisibility.Public);
public record CreateCommentApiRequest(string PostId, string? ParentCommentId, string AuthorId, string AuthorName, string Body);
public record ReactApiRequest(string ActorId, string ActorName, string TargetId, ReactionTargetKind TargetKind, ReactionType Type);
public record FollowApiRequest(string FollowerId, string FolloweeId);
public record DirectConversationApiRequest(string User1Id, string User1Name, string User2Id, string User2Name);
public record CreateGroupConversationApiRequest(string Title, string CreatorId, string CreatorName, List<ParticipantInfo> Participants);
public record ParticipantInfo(string Id, string Name);
public record SendMessageApiRequest(string ConversationId, string SenderId, string SenderName, string Body);
public record MarkReadApiRequest(string ConversationId, string ParticipantId, string MessageId);
public record CreateActivityApiRequest(string TypeKey, string ActorId, string ActorName, string Summary, object? Payload);
public record IndexDocumentApiRequest(string DocumentType, string Id, string? Title, string? Body);

// ═══════════════════════════════════════════════════════════════════════════════
// MOCK STORAGE PROVIDER
// ═══════════════════════════════════════════════════════════════════════════════
public sealed class MockStorageProvider : IMediaStorageProvider
{
    public Task<string> GenerateUploadUrlAsync(string blobPath, string contentType, long maxSizeBytes, TimeSpan expiry, CancellationToken ct = default)
        => Task.FromResult($"https://blazorbook.blob.core.windows.net/media/{blobPath}?sas=mocktoken");

    public Task<string> GenerateDownloadUrlAsync(string blobPath, TimeSpan expiry, CancellationToken ct = default)
        => Task.FromResult($"https://blazorbook.blob.core.windows.net/media/{blobPath}");

    public Task<bool> ExistsAsync(string blobPath, CancellationToken ct = default)
        => Task.FromResult(true);

    public Task<BlobProperties?> GetPropertiesAsync(string blobPath, CancellationToken ct = default)
        => Task.FromResult<BlobProperties?>(new BlobProperties
        {
            SizeBytes = 1024,
            ContentType = "image/jpeg",
            LastModified = DateTimeOffset.UtcNow
        });

    public Task DeleteAsync(string blobPath, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task CopyAsync(string sourcePath, string destPath, CancellationToken ct = default)
        => Task.CompletedTask;
}
