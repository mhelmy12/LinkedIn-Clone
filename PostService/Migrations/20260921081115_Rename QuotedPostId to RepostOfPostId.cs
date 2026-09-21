using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PostService.Migrations
{
    /// <inheritdoc />
    public partial class RenameQuotedPostIdtoRepostOfPostId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Posts_QuotedPostId",
                table: "Posts");

            migrationBuilder.RenameColumn(
                name: "QuotedPostId",
                table: "Posts",
                newName: "RepostOfPostId");

            migrationBuilder.RenameIndex(
                name: "IX_Posts_QuotedPostId",
                table: "Posts",
                newName: "IX_Posts_RepostOfPostId");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Posts_RepostOfPostId",
                table: "Posts",
                column: "RepostOfPostId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Posts_RepostOfPostId",
                table: "Posts");

            migrationBuilder.RenameColumn(
                name: "RepostOfPostId",
                table: "Posts",
                newName: "QuotedPostId");

            migrationBuilder.RenameIndex(
                name: "IX_Posts_RepostOfPostId",
                table: "Posts",
                newName: "IX_Posts_QuotedPostId");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Posts_QuotedPostId",
                table: "Posts",
                column: "QuotedPostId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
