using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FeedService.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PostSnapshots",
                columns: table => new
                {
                    PostId = table.Column<long>(type: "bigint", nullable: false),
                    AuthorId = table.Column<string>(type: "text", nullable: false),
                    AuthorDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AuthorProfileImageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AuthorHeadline = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Content = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    IsPureRepost = table.Column<bool>(type: "boolean", nullable: false),
                    RepostOfPostId = table.Column<long>(type: "bigint", nullable: true),
                    RepostOfContent = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    RepostOfAuthorId = table.Column<string>(type: "text", nullable: true),
                    RepostOfAuthorDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Hashtags = table.Column<List<string>>(type: "text[]", nullable: false),
                    MentionedUserIds = table.Column<List<string>>(type: "bigint[]", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostSnapshots", x => x.PostId);
                });

            migrationBuilder.CreateTable(
                name: "UserSummaries",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProfileImageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Headline = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSummaries", x => x.UserId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PostSnapshots_Author_CreatedAt",
                table: "PostSnapshots",
                columns: new[] { "AuthorId", "CreatedAt" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_PostSnapshots_AuthorId",
                table: "PostSnapshots",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_PostSnapshots_CreatedAt",
                table: "PostSnapshots",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserSummaries_UpdatedAt",
                table: "UserSummaries",
                column: "UpdatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PostSnapshots");

            migrationBuilder.DropTable(
                name: "UserSummaries");
        }
    }
}
