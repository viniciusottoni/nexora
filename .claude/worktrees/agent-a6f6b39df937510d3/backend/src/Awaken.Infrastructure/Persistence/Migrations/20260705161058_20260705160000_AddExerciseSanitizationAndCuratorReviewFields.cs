using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260705160000_AddExerciseSanitizationAndCuratorReviewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "exercise_catalogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAtUtc",
                table: "exercise_catalogs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedBy",
                table: "exercise_catalogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "SanitizationIssues",
                table: "exercise_catalogs",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "exercise_catalogs");

            migrationBuilder.DropColumn(
                name: "ReviewedAtUtc",
                table: "exercise_catalogs");

            migrationBuilder.DropColumn(
                name: "ReviewedBy",
                table: "exercise_catalogs");

            migrationBuilder.DropColumn(
                name: "SanitizationIssues",
                table: "exercise_catalogs");
        }
    }
}
