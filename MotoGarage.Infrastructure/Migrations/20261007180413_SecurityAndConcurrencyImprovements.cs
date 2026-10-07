using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotoGarage.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SecurityAndConcurrencyImprovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fail clearly before schema changes; never discard existing business data.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM Reviews GROUP BY UserId, MechanicId HAVING COUNT(*) > 1)
                    THROW 50001, 'Duplicate reviews must be resolved before applying this migration.', 1;
                IF EXISTS (SELECT 1 FROM Conversations WHERE
                    (MechanicId IS NULL AND StoreId IS NULL) OR (MechanicId IS NOT NULL AND StoreId IS NOT NULL))
                    THROW 50002, 'Conversation targets must contain exactly one participant.', 1;
                IF EXISTS (SELECT 1 FROM Conversations WHERE MechanicId IS NOT NULL
                    GROUP BY UserId, MechanicId HAVING COUNT(*) > 1)
                    THROW 50003, 'Duplicate mechanic conversations must be resolved.', 1;
                IF EXISTS (SELECT 1 FROM Conversations WHERE StoreId IS NOT NULL
                    GROUP BY UserId, StoreId HAVING COUNT(*) > 1)
                    THROW 50004, 'Duplicate store conversations must be resolved.', 1;
                IF EXISTS (SELECT 1 FROM Appointments WHERE Status IN (1,2)
                    GROUP BY MechanicId, AppointmentDate HAVING COUNT(*) > 1)
                    THROW 50005, 'Duplicate active appointment slots must be resolved.', 1;
                IF EXISTS (SELECT 1 FROM Products WHERE Stock < 0)
                    THROW 50006, 'Negative product stock must be resolved.', 1;
                """);

            migrationBuilder.DropIndex(
                name: "IX_Reviews_UserId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_UserId",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_MechanicId",
                table: "Appointments");


            migrationBuilder.AddColumn<string>(
                name: "ImagePublicId",
                table: "Vehicles",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Vehicles",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);


            migrationBuilder.AddColumn<string>(
                name: "ImagePublicId",
                table: "Stores",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "RefreshTokens",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);


            migrationBuilder.AddColumn<string>(
                name: "ImagePublicId",
                table: "Products",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Products",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Orders",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);


            migrationBuilder.AddColumn<string>(
                name: "ImagePublicId",
                table: "Mechanics",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "EmailVerificationCodes",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<bool>(
                name: "IsAdminBlocked",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Appointments",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.Sql("""
                UPDATE AspNetUsers SET IsAdminBlocked = 1 WHERE LockoutEnd >= '9999-01-01';
                UPDATE RefreshTokens SET Token = CONVERT(varchar(64),
                    HASHBYTES('SHA2_256', CONVERT(varchar(500), Token)), 2);
                """);

            migrationBuilder.CreateTable(
                name: "ImageAssets",
                columns: table => new
                {
                    PublicId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    IsDeleting = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageAssets", x => x.PublicId);
                    table.ForeignKey(
                        name: "FK_ImageAssets_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_UserId_MechanicId",
                table: "Reviews",
                columns: new[] { "UserId", "MechanicId" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_Stock",
                table: "Products",
                sql: "[Stock] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_UserId_MechanicId",
                table: "Conversations",
                columns: new[] { "UserId", "MechanicId" },
                unique: true,
                filter: "[MechanicId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_UserId_StoreId",
                table: "Conversations",
                columns: new[] { "UserId", "StoreId" },
                unique: true,
                filter: "[StoreId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Conversations_ExactlyOneTarget",
                table: "Conversations",
                sql: "([MechanicId] IS NOT NULL AND [StoreId] IS NULL) OR ([MechanicId] IS NULL AND [StoreId] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "UX_Appointments_ActiveSlot",
                table: "Appointments",
                columns: new[] { "MechanicId", "AppointmentDate" },
                unique: true,
                filter: "[Status] IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_ImageAssets_UserId",
                table: "ImageAssets",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Hashes cannot be reversed. Downgrading requires a fresh login.
            migrationBuilder.Sql("UPDATE RefreshTokens SET RevokedAt = COALESCE(RevokedAt, SYSUTCDATETIME());");

            migrationBuilder.DropTable(
                name: "ImageAssets");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_UserId_MechanicId",
                table: "Reviews");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_Stock",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_UserId_MechanicId",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_UserId_StoreId",
                table: "Conversations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Conversations_ExactlyOneTarget",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "UX_Appointments_ActiveSlot",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ImagePublicId",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "ImagePublicId",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "ImagePublicId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ImagePublicId",
                table: "Mechanics");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "EmailVerificationCodes");

            migrationBuilder.DropColumn(
                name: "IsAdminBlocked",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Appointments");





            migrationBuilder.CreateIndex(
                name: "IX_Reviews_UserId",
                table: "Reviews",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_UserId",
                table: "Conversations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_MechanicId",
                table: "Appointments",
                column: "MechanicId");
        }
    }
}
