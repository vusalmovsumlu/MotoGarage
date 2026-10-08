using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotoGarage.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleSlugs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Vehicles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            // Backfill existing vehicles before enforcing uniqueness and NOT NULL.
            migrationBuilder.Sql("""
                UPDATE [Vehicles]
                SET [Slug] = LOWER(TRANSLATE(CONCAT([Brand], '-', [Model]),
                    N'ƏəĞğİıÖöÜüŞşÇç', N'EeGgIiOoUuSsCc'));
                WHILE EXISTS (SELECT 1 FROM [Vehicles] WHERE PATINDEX('%[^a-z0-9-]%', [Slug] COLLATE Latin1_General_100_BIN2) > 0)
                    UPDATE [Vehicles] SET [Slug] = STUFF([Slug], PATINDEX('%[^a-z0-9-]%', [Slug] COLLATE Latin1_General_100_BIN2), 1, '-')
                    WHERE PATINDEX('%[^a-z0-9-]%', [Slug] COLLATE Latin1_General_100_BIN2) > 0;
                WHILE EXISTS (SELECT 1 FROM [Vehicles] WHERE [Slug] LIKE '%--%')
                    UPDATE [Vehicles] SET [Slug] = REPLACE([Slug], '--', '-') WHERE [Slug] LIKE '%--%';
                UPDATE [Vehicles] SET [Slug] = CONCAT(
                    COALESCE(NULLIF(TRIM('-' FROM [Slug]), ''), 'qaraj'), '-',
                    LOWER(REPLACE(CONVERT(varchar(36), NEWID()), '-', '')));
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Slug", table: "Vehicles", type: "nvarchar(200)",
                maxLength: 200, nullable: false,
                oldClrType: typeof(string), oldType: "nvarchar(200)",
                oldMaxLength: 200, oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_Slug",
                table: "Vehicles",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vehicles_Slug",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Vehicles");
        }
    }
}
