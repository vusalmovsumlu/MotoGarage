using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotoGarage.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAutomaticReminderNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "NotificationSentAtUtc",
                table: "Reminders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_PendingNotification",
                table: "Reminders",
                columns: new[] { "ReminderDate", "VehicleId" },
                filter: "[IsCompleted] = 0 AND [NotificationSentAtUtc] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reminders_PendingNotification",
                table: "Reminders");

            migrationBuilder.DropColumn(
                name: "NotificationSentAtUtc",
                table: "Reminders");
        }
    }
}
