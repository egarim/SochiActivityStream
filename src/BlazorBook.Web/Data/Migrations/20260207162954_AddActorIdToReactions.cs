using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorBook.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddActorIdToReactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reactions_Unique",
                table: "Reactions");

            migrationBuilder.AddColumn<string>(
                name: "ActorId",
                table: "Reactions",
                type: "TEXT",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_TenantId_TargetId_TargetKind",
                table: "Reactions",
                columns: new[] { "TenantId", "TargetId", "TargetKind" });

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_Unique",
                table: "Reactions",
                columns: new[] { "TenantId", "TargetId", "TargetKind", "ActorId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reactions_TenantId_TargetId_TargetKind",
                table: "Reactions");

            migrationBuilder.DropIndex(
                name: "IX_Reactions_Unique",
                table: "Reactions");

            migrationBuilder.DropColumn(
                name: "ActorId",
                table: "Reactions");

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_Unique",
                table: "Reactions",
                columns: new[] { "TenantId", "TargetId", "TargetKind" },
                unique: true);
        }
    }
}
