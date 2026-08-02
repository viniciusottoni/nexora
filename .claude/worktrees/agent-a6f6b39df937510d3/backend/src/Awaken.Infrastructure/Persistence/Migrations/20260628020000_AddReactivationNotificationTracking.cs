using System;
using Awaken.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AwakenDbContext))]
    [Migration("20260628020000_AddReactivationNotificationTracking")]
    public partial class AddReactivationNotificationTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastReactivationNotificationSentAt",
                table: "notification_preferences",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastReactivationNotificationSentAt",
                table: "notification_preferences");
        }
    }
}
