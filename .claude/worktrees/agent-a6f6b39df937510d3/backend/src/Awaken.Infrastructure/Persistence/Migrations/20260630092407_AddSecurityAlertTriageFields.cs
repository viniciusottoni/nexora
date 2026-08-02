using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityAlertTriageFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Classification",
                table: "security_alerts",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "security_alerts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RelatedBugId",
                table: "security_alerts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_security_alerts_Classification",
                table: "security_alerts",
                column: "Classification");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_security_alerts_Classification",
                table: "security_alerts");

            migrationBuilder.DropColumn(
                name: "Classification",
                table: "security_alerts");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "security_alerts");

            migrationBuilder.DropColumn(
                name: "RelatedBugId",
                table: "security_alerts");
        }
    }
}
