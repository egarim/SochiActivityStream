using Microsoft.EntityFrameworkCore;
using Identity.Abstractions;
using Content.Abstractions;
using Chat.Abstractions;
using RelationshipService.Abstractions;
using ActivityStream.Abstractions;
using Inbox.Abstractions;
using Media.Abstractions;
using Search.Abstractions;
using BlazorBook.Web.Data.Configurations;

namespace BlazorBook.Web.Data;

/// <summary>
/// Unified DbContext for all BlazorBook domains
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Identity Domain
    public DbSet<UserDto> Users => Set<UserDto>();
    public DbSet<ProfileDto> Profiles => Set<ProfileDto>();
    public DbSet<MembershipDto> Memberships => Set<MembershipDto>();
    public DbSet<SessionDto> Sessions => Set<SessionDto>();
    public DbSet<UserPasswordEntity> UserPasswords => Set<UserPasswordEntity>();

    // Content Domain
    public DbSet<PostDto> Posts => Set<PostDto>();
    public DbSet<CommentDto> Comments => Set<CommentDto>();
    public DbSet<ReactionDto> Reactions => Set<ReactionDto>();

    // Chat Domain
    public DbSet<ConversationDto> Conversations => Set<ConversationDto>();
    public DbSet<MessageDto> Messages => Set<MessageDto>();

    // Relationship Domain
    public DbSet<RelationshipEdgeDto> RelationshipEdges => Set<RelationshipEdgeDto>();

    // ActivityStream Domain
    public DbSet<ActivityDto> Activities => Set<ActivityDto>();

    // Inbox Domain
    public DbSet<InboxItemDto> InboxItems => Set<InboxItemDto>();
    public DbSet<FollowRequestDto> FollowRequests => Set<FollowRequestDto>();

    // Media Domain
    public DbSet<MediaDto> Media => Set<MediaDto>();

    // Search Domain
    public DbSet<SearchDocument> SearchDocuments => Set<SearchDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from the Configurations folder
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
