using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotoGarage.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentEmailReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EmailReminderLeaseId",
                table: "Appointments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailReminderLeaseUntilUtc",
                table: "Appointments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailReminderNextAttemptAtUtc",
                table: "Appointments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailReminderSentAtUtc",
                table: "Appointments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_EmailReminderDue",
                table: "Appointments",
                column: "AppointmentDate",
                filter: "[EmailReminderSentAtUtc] IS NULL AND [Status] IN (1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Appointments_EmailReminderDue",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "EmailReminderLeaseId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "EmailReminderLeaseUntilUtc",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "EmailReminderNextAttemptAtUtc",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "EmailReminderSentAtUtc",
                table: "Appointments");
        }
    }
}
