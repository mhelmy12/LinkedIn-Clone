using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PostService.Migrations
{
    /// <inheritdoc />
    public partial class RemovePostRepostEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PostReposts");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Posts",
                type: "rowversion",
                rowVersion: true,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Posts");

            migrationBuilder.CreateTable(
                name: "PostReposts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OriginalPostId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostReposts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostReposts_Posts_OriginalPostId",
                        column: x => x.OriginalPostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PostReposts_CreatedAt",
                table: "PostReposts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PostReposts_OriginalPostId",
                table: "PostReposts",
                column: "OriginalPostId");

            migrationBuilder.CreateIndex(
                name: "IX_PostReposts_OriginalPostId_UserId",
                table: "PostReposts",
                columns: new[] { "OriginalPostId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostReposts_UserId",
                table: "PostReposts",
                column: "UserId");
        }
    }
}
