using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotoGarage.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OrderDeliveryAndChatMedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddress",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryCity",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryNote",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryPhone",
                table: "Orders",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecipientName",
                table: "Orders",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AttachmentId",
                table: "Messages",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChatAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversationId = table.Column<int>(type: "int", nullable: false),
                    UploadedByUserId = table.Column<int>(type: "int", nullable: false),
                    PublicId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Format = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsAttached = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleting = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatAttachments", x => x.Id);
                    table.CheckConstraint("CK_ChatAttachments_KindSize", "([Kind] = 'image' AND [SizeBytes] > 0 AND [SizeBytes] <= 5242880) OR ([Kind] = 'video' AND [SizeBytes] > 0 AND [SizeBytes] <= 26214400)");
                    table.ForeignKey(
                        name: "FK_ChatAttachments_AspNetUsers_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChatAttachments_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_AttachmentId",
                table: "Messages",
                column: "AttachmentId",
                unique: true,
                filter: "[AttachmentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ChatAttachments_ConversationId",
                table: "ChatAttachments",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatAttachments_IsAttached_CreatedAtUtc",
                table: "ChatAttachments",
                columns: new[] { "IsAttached", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatAttachments_PublicId",
                table: "ChatAttachments",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatAttachments_UploadedByUserId",
                table: "ChatAttachments",
                column: "UploadedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_ChatAttachments_AttachmentId",
                table: "Messages",
                column: "AttachmentId",
                principalTable: "ChatAttachments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_ChatAttachments_AttachmentId",
                table: "Messages");

            migrationBuilder.DropTable(
                name: "ChatAttachments");

            migrationBuilder.DropIndex(
                name: "IX_Messages_AttachmentId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "DeliveryAddress",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryCity",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryNote",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryPhone",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RecipientName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "AttachmentId",
                table: "Messages");
        }
    }
}
