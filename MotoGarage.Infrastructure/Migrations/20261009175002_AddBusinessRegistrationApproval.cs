using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotoGarage.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessRegistrationApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BusinessApprovalStatus",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "BusinessApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedById = table.Column<int>(type: "int", nullable: true),
                    DecisionComment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessApplications_AspNetUsers_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusinessApplications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessApplicationPhotos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    PublicId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessApplicationPhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessApplicationPhotos_BusinessApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "BusinessApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessApplicationPhotos_ApplicationId_Position",
                table: "BusinessApplicationPhotos",
                columns: new[] { "ApplicationId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessApplications_ReviewedById",
                table: "BusinessApplications",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessApplications_Status_CreatedAt",
                table: "BusinessApplications",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessApplications_UserId",
                table: "BusinessApplications",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessApplicationPhotos");

            migrationBuilder.DropTable(
                name: "BusinessApplications");

            migrationBuilder.DropColumn(
                name: "BusinessApprovalStatus",
                table: "AspNetUsers");
        }
    }
}
