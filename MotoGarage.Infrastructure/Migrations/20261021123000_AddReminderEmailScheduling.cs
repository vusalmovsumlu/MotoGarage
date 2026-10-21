using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotoGarage.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReminderEmailScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EmailQueuedAtUtc",
                table: "Reminders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScheduleRevision",
                table: "Reminders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_PendingEmail",
                table: "Reminders",
                columns: new[] { "ReminderDate", "VehicleId", "ScheduleRevision" },
                filter: "[IsCompleted] = 0 AND [EmailQueuedAtUtc] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reminders_PendingEmail",
                table: "Reminders");

            migrationBuilder.DropColumn(
                name: "EmailQueuedAtUtc",
                table: "Reminders");

            migrationBuilder.DropColumn(
                name: "ScheduleRevision",
                table: "Reminders");
        }
    }
}
