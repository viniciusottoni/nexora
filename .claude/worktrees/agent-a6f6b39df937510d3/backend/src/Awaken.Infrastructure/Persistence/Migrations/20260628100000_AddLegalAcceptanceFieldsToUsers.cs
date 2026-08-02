using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLegalAcceptanceFieldsToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "TermsAcceptedAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PrivacyAcceptedAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TermsVersion",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrivacyVersion",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResponsibilityNoticeAcceptedAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsibilityNoticeVersion",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "TermsAcceptedAt", table: "users");
            migrationBuilder.DropColumn(name: "PrivacyAcceptedAt", table: "users");
            migrationBuilder.DropColumn(name: "TermsVersion", table: "users");
            migrationBuilder.DropColumn(name: "PrivacyVersion", table: "users");
            migrationBuilder.DropColumn(name: "ResponsibilityNoticeAcceptedAt", table: "users");
            migrationBuilder.DropColumn(name: "ResponsibilityNoticeVersion", table: "users");
        }
    }
}
