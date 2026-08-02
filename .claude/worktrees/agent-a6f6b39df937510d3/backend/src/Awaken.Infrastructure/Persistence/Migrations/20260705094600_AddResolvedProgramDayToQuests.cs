using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    /// US-238: dia do programa resolvido pela rotação cíclica (RN-009), gravado
    /// no Quest para auditoria e base da próxima rotação.
    public partial class AddResolvedProgramDayToQuests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ResolvedDayIndex",
                table: "quests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolvedDayKey",
                table: "quests",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolvedProgramKey",
                table: "quests",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SplitMapVersion",
                table: "quests",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_quests_UserId_ProgramId_CompletedAtUtc",
                table: "quests",
                columns: new[] { "UserId", "ProgramId", "CompletedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_quests_UserId_ProgramId_CompletedAtUtc",
                table: "quests");

            migrationBuilder.DropColumn(
                name: "ResolvedDayIndex",
                table: "quests");

            migrationBuilder.DropColumn(
                name: "ResolvedDayKey",
                table: "quests");

            migrationBuilder.DropColumn(
                name: "ResolvedProgramKey",
                table: "quests");

            migrationBuilder.DropColumn(
                name: "SplitMapVersion",
                table: "quests");
        }
    }
}
