using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRevenueCatFieldsToSubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Entitlement",
                table: "subscriptions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastRevenueCatSyncAt",
                table: "subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevenueCatCustomerId",
                table: "subscriptions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Entitlement",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "LastRevenueCatSyncAt",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "RevenueCatCustomerId",
                table: "subscriptions");
        }
    }
}
