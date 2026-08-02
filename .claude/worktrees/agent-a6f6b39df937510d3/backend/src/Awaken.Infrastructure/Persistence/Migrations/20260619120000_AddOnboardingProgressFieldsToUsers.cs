using System;
using Awaken.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(AwakenDbContext))]
    [Migration("20260619120000_AddOnboardingProgressFieldsToUsers")]
    public partial class AddOnboardingProgressFieldsToUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "OnboardingStartedAtUtc",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OnboardingCompletedAtUtc",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentOnboardingStep",
                table: "users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OnboardingStartedAtUtc",
                table: "users");

            migrationBuilder.DropColumn(
                name: "OnboardingCompletedAtUtc",
                table: "users");

            migrationBuilder.DropColumn(
                name: "CurrentOnboardingStep",
                table: "users");
        }
    }
}
