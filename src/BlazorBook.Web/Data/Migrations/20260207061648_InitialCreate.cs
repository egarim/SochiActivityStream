using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorBook.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    TypeKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Targets = table.Column<string>(type: "TEXT", nullable: false),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Payload = table.Column<string>(type: "TEXT", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    Actor = table.Column<string>(type: "TEXT", nullable: false),
                    Owner = table.Column<string>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    PostId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ParentCommentId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Body = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReplyCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ReactionCounts = table.Column<string>(type: "TEXT", nullable: false),
                    ViewerReaction = table.Column<int>(type: "INTEGER", nullable: true),
                    Author = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Conversations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    AvatarUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    Participants = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FollowRequests",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    RequestedKind = table.Column<int>(type: "INTEGER", nullable: false),
                    Scope = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    DecidedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    DecisionReason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    DecidedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Requester = table.Column<string>(type: "TEXT", nullable: false),
                    Target = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FollowRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InboxItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    Body = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Targets = table.Column<string>(type: "TEXT", nullable: false),
                    Data = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    DedupKey = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ThreadKey = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ThreadCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Event = table.Column<string>(type: "TEXT", nullable: false),
                    Recipient = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Media",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    BlobPath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    ThumbnailBlobPath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    Width = table.Column<int>(type: "INTEGER", nullable: true),
                    Height = table.Column<int>(type: "INTEGER", nullable: true),
                    DurationSeconds = table.Column<double>(type: "REAL", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    UploadExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    AltText = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata = table.Column<string>(type: "TEXT", nullable: true),
                    Owner = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Media", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Memberships",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ProfileId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Role = table.Column<int>(type: "INTEGER", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Memberships", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ConversationId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Body = table.Column<string>(type: "TEXT", maxLength: 10000, nullable: false),
                    Media = table.Column<string>(type: "TEXT", nullable: true),
                    ReplyToMessageId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    EditedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedByProfileIds = table.Column<string>(type: "TEXT", nullable: true),
                    SystemMessageType = table.Column<int>(type: "INTEGER", nullable: true),
                    Sender = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Posts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Body = table.Column<string>(type: "TEXT", maxLength: 10000, nullable: false),
                    MediaIds = table.Column<string>(type: "TEXT", nullable: true),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CommentCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ReactionCounts = table.Column<string>(type: "TEXT", nullable: false),
                    ViewerReaction = table.Column<int>(type: "INTEGER", nullable: true),
                    Author = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Posts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Profiles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Handle = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    AvatarUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    IsPrivate = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reactions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    TargetId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    TargetKind = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Actor = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RelationshipEdges",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Scope = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    From = table.Column<string>(type: "TEXT", nullable: false),
                    To = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelationshipEdges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SearchDocuments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    DocumentType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    TextFields = table.Column<string>(type: "TEXT", nullable: false),
                    KeywordFields = table.Column<string>(type: "TEXT", nullable: false),
                    NumericFields = table.Column<string>(type: "TEXT", nullable: false),
                    DateFields = table.Column<string>(type: "TEXT", nullable: false),
                    IndexedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Boost = table.Column<double>(type: "REAL", nullable: false),
                    SourceEntity = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    SessionId = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    AccessToken = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ProfileIds = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.SessionId);
                });

            migrationBuilder.CreateTable(
                name: "UserPasswords",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Salt = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Iterations = table.Column<int>(type: "INTEGER", nullable: false),
                    HashBytes = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Algorithm = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPasswords", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_TenantId_OccurredAt",
                table: "Activities",
                columns: new[] { "TenantId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_TypeKey",
                table: "Activities",
                column: "TypeKey");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_Visibility",
                table: "Activities",
                column: "Visibility");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_IsDeleted",
                table: "Comments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ParentCommentId",
                table: "Comments",
                column: "ParentCommentId",
                filter: "ParentCommentId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_TenantId_PostId_CreatedAt",
                table: "Comments",
                columns: new[] { "TenantId", "PostId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_TenantId_UpdatedAt",
                table: "Conversations",
                columns: new[] { "TenantId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_Type",
                table: "Conversations",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_FollowRequests_IdempotencyKey",
                table: "FollowRequests",
                column: "IdempotencyKey",
                unique: true,
                filter: "IdempotencyKey IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FollowRequests_TenantId_Status_CreatedAt",
                table: "FollowRequests",
                columns: new[] { "TenantId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InboxItems_DedupKey",
                table: "InboxItems",
                column: "DedupKey",
                unique: true,
                filter: "DedupKey IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InboxItems_TenantId_Status_CreatedAt",
                table: "InboxItems",
                columns: new[] { "TenantId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InboxItems_ThreadKey",
                table: "InboxItems",
                column: "ThreadKey",
                filter: "ThreadKey IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Media_DeletedAt",
                table: "Media",
                column: "DeletedAt",
                filter: "DeletedAt IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Media_TenantId_Status_CreatedAt",
                table: "Media",
                columns: new[] { "TenantId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_ProfileId",
                table: "Memberships",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_TenantId_UserId",
                table: "Memberships",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_IsDeleted",
                table: "Messages",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ReplyToMessageId",
                table: "Messages",
                column: "ReplyToMessageId",
                filter: "ReplyToMessageId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_TenantId_ConversationId_CreatedAt",
                table: "Messages",
                columns: new[] { "TenantId", "ConversationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_IsDeleted",
                table: "Posts",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_TenantId_CreatedAt",
                table: "Posts",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_Handle",
                table: "Profiles",
                column: "Handle",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_Unique",
                table: "Reactions",
                columns: new[] { "TenantId", "TargetId", "TargetKind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RelationshipEdges_Kind",
                table: "RelationshipEdges",
                column: "Kind");

            migrationBuilder.CreateIndex(
                name: "IX_RelationshipEdges_TenantId_IsActive",
                table: "RelationshipEdges",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Relationships_Unique",
                table: "RelationshipEdges",
                columns: new[] { "TenantId", "Kind", "Scope" });

            migrationBuilder.CreateIndex(
                name: "IX_SearchDocuments_Boost",
                table: "SearchDocuments",
                column: "Boost");

            migrationBuilder.CreateIndex(
                name: "IX_SearchDocuments_TenantId_DocumentType_IndexedAt",
                table: "SearchDocuments",
                columns: new[] { "TenantId", "DocumentType", "IndexedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_AccessToken",
                table: "Sessions",
                column: "AccessToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_UserId_ExpiresAt",
                table: "Sessions",
                columns: new[] { "UserId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Activities");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "Conversations");

            migrationBuilder.DropTable(
                name: "FollowRequests");

            migrationBuilder.DropTable(
                name: "InboxItems");

            migrationBuilder.DropTable(
                name: "Media");

            migrationBuilder.DropTable(
                name: "Memberships");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "Posts");

            migrationBuilder.DropTable(
                name: "Profiles");

            migrationBuilder.DropTable(
                name: "Reactions");

            migrationBuilder.DropTable(
                name: "RelationshipEdges");

            migrationBuilder.DropTable(
                name: "SearchDocuments");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "UserPasswords");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
