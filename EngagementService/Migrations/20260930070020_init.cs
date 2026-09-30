using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngagementService.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    PostId = table.Column<string>(type: "text", nullable: false),
                    AuthorId = table.Column<string>(type: "text", nullable: false),
                    ParentCommentId = table.Column<long>(type: "bigint", nullable: true),
                    Content = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comments_Comments_ParentCommentId",
                        column: x => x.ParentCommentId,
                        principalTable: "Comments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    AggregateType = table.Column<string>(type: "text", nullable: false),
                    AggregateId = table.Column<string>(type: "text", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    TargetType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TargetId = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommentMedia",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    CommentId = table.Column<long>(type: "bigint", nullable: false),
                    ObjectKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MediaType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommentMedia_Comments_CommentId",
                        column: x => x.CommentId,
                        principalTable: "Comments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommentMentions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    CommentId = table.Column<long>(type: "bigint", nullable: false),
                    MentionedUserId = table.Column<string>(type: "text", nullable: false),
                    StartIndex = table.Column<int>(type: "integer", nullable: false),
                    Length = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentMentions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommentMentions_Comments_CommentId",
                        column: x => x.CommentId,
                        principalTable: "Comments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommentMedia_CommentId",
                table: "CommentMedia",
                column: "CommentId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentMentions_CommentId",
                table: "CommentMentions",
                column: "CommentId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentMentions_MentionedUserId",
                table: "CommentMentions",
                column: "MentionedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_Author",
                table: "Comments",
                columns: new[] { "AuthorId", "CreatedAt" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_Post_TopLevel",
                table: "Comments",
                columns: new[] { "PostId", "ParentCommentId", "CreatedAt" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_Replies",
                table: "Comments",
                columns: new[] { "ParentCommentId", "CreatedAt" },
                filter: "\"IsDeleted\" = false AND \"ParentCommentId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_Active_User_Target",
                table: "Reactions",
                columns: new[] { "UserId", "TargetType", "TargetId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_Target_Active",
                table: "Reactions",
                columns: new[] { "TargetType", "TargetId", "CreatedAt" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_User_Active",
                table: "Reactions",
                columns: new[] { "UserId", "CreatedAt" },
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommentMedia");

            migrationBuilder.DropTable(
                name: "CommentMentions");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "Reactions");

            migrationBuilder.DropTable(
                name: "Comments");
        }
    }
}
