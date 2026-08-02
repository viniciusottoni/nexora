using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Quests",
                table: "Quests");

            migrationBuilder.RenameTable(
                name: "Quests",
                newName: "quests");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "quests",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "quests",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Language",
                table: "quests",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "IdempotencyKey",
                table: "quests",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedAtUtc",
                table: "quests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_quests",
                table: "quests",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_quests_IdempotencyKey",
                table: "quests",
                column: "IdempotencyKey");

            migrationBuilder.CreateIndex(
                name: "IX_quests_UserId_Type_QuestDateUtc",
                table: "quests",
                columns: new[] { "UserId", "Type", "QuestDateUtc" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_quests",
                table: "quests");

            migrationBuilder.DropIndex(
                name: "IX_quests_IdempotencyKey",
                table: "quests");

            migrationBuilder.DropIndex(
                name: "IX_quests_UserId_Type_QuestDateUtc",
                table: "quests");

            migrationBuilder.DropColumn(
                name: "ConfirmedAtUtc",
                table: "quests");

            migrationBuilder.RenameTable(
                name: "quests",
                newName: "Quests");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Quests",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Quests",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "Language",
                table: "Quests",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(8)",
                oldMaxLength: 8);

            migrationBuilder.AlterColumn<string>(
                name: "IdempotencyKey",
                table: "Quests",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Quests",
                table: "Quests",
                column: "Id");
        }
    }
}
