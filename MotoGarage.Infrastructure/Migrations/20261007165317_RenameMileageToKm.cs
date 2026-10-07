using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotoGarage.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameMileageToKm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Mileage",
                table: "Vehicles",
                newName: "CurrentKm");

            migrationBuilder.RenameColumn(
                name: "Mileage",
                table: "ServiceRecords",
                newName: "Kilometer");

            migrationBuilder.RenameColumn(
                name: "ReminderMileage",
                table: "Reminders",
                newName: "ReminderKm");

            migrationBuilder.RenameColumn(
                name: "Mileage",
                table: "FuelRecords",
                newName: "Kilometer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CurrentKm",
                table: "Vehicles",
                newName: "Mileage");

            migrationBuilder.RenameColumn(
                name: "Kilometer",
                table: "ServiceRecords",
                newName: "Mileage");

            migrationBuilder.RenameColumn(
                name: "ReminderKm",
                table: "Reminders",
                newName: "ReminderMileage");

            migrationBuilder.RenameColumn(
                name: "Kilometer",
                table: "FuelRecords",
                newName: "Mileage");
        }
    }
}
