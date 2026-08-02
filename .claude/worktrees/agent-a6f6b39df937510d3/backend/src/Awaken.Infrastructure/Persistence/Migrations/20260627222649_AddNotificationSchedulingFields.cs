using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationSchedulingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DailyNotificationCount",
                table: "notification_preferences",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "LastNotificationCountResetDate",
                table: "notification_preferences",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastNotificationSentAt",
                table: "notification_preferences",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "PreferredReminderTime",
                table: "notification_preferences",
                type: "time without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailyNotificationCount",
                table: "notification_preferences");

            migrationBuilder.DropColumn(
                name: "LastNotificationCountResetDate",
                table: "notification_preferences");

            migrationBuilder.DropColumn(
                name: "LastNotificationSentAt",
                table: "notification_preferences");

            migrationBuilder.DropColumn(
                name: "PreferredReminderTime",
                table: "notification_preferences");
        }
    }
}
