using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatService.Migrations
{
    /// <inheritdoc />
    public partial class InitialChatSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatConnectionProjections",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    OtherUserId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatConnectionProjections", x => new { x.UserId, x.OtherUserId });
                });

            migrationBuilder.CreateTable(
                name: "ChatUserProjections",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatUserProjections", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Conversations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Participant1Id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Participant2Id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastMessageAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.Id);
                    table.CheckConstraint("CK_Conversations_Participants_Order", "\"Participant1Id\" < \"Participant2Id\"");
                });

            migrationBuilder.CreateTable(
                name: "InboxMessages",
                columns: table => new
                {
                    ConsumerGroup = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxMessages", x => new { x.ConsumerGroup, x.EventId });
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AggregateId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserDeviceKeys",
                columns: table => new
                {
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PublicKey = table.Column<byte[]>(type: "bytea", nullable: false),
                    Algorithm = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDeviceKeys", x => x.DeviceId);
                });

            migrationBuilder.CreateTable(
                name: "ConversationKeyEnvelopes",
                columns: table => new
                {
                    ConversationId = table.Column<long>(type: "bigint", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    WrappedKey = table.Column<byte[]>(type: "bytea", nullable: false),
                    Nonce = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationKeyEnvelopes", x => new { x.ConversationId, x.DeviceId });
                    table.CheckConstraint("CK_ConversationKeyEnvelopes_Nonce_Size", "octet_length(\"Nonce\") = 12");
                    table.ForeignKey(
                        name: "FK_ConversationKeyEnvelopes_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    ConversationId = table.Column<long>(type: "bigint", nullable: false),
                    SenderId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ClientMessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ciphertext = table.Column<byte[]>(type: "bytea", nullable: false),
                    Nonce = table.Column<byte[]>(type: "bytea", nullable: false),
                    Algorithm = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.CheckConstraint("CK_Messages_Ciphertext_Size", "octet_length(\"Ciphertext\") <= 16384");
                    table.CheckConstraint("CK_Messages_Nonce_Size", "octet_length(\"Nonce\") = 12");
                    table.ForeignKey(
                        name: "FK_Messages_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatConnectionProjections_User_Active",
                table: "ChatConnectionProjections",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ConversationKeyEnvelopes_Device",
                table: "ConversationKeyEnvelopes",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_LastMessageAt",
                table: "Conversations",
                column: "LastMessageAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "UX_Conversations_Participants",
                table: "Conversations",
                columns: new[] { "Participant1Id", "Participant2Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_Conversation_Id",
                table: "Messages",
                columns: new[] { "ConversationId", "Id" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_Unread",
                table: "Messages",
                column: "ConversationId",
                filter: "\"ReadAtUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Messages_Sender_ClientMessageId",
                table: "Messages",
                columns: new[] { "ConversationId", "SenderId", "ClientMessageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Timestamp",
                table: "OutboxMessages",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceKeys_User_Active",
                table: "UserDeviceKeys",
                column: "UserId",
                filter: "\"RevokedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatConnectionProjections");

            migrationBuilder.DropTable(
                name: "ChatUserProjections");

            migrationBuilder.DropTable(
                name: "ConversationKeyEnvelopes");

            migrationBuilder.DropTable(
                name: "InboxMessages");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "UserDeviceKeys");

            migrationBuilder.DropTable(
                name: "Conversations");
        }
    }
}
