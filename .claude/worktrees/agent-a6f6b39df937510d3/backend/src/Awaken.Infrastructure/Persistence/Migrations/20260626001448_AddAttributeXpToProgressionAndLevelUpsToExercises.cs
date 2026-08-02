using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAttributeXpToProgressionAndLevelUpsToExercises : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_HunterProgressions",
                table: "HunterProgressions");

            migrationBuilder.RenameTable(
                name: "HunterProgressions",
                newName: "hunter_progressions");

            migrationBuilder.AddColumn<int>(
                name: "AgilityLevelUps",
                table: "quest_exercises",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EnduranceLevelUps",
                table: "quest_exercises",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FocusLevelUps",
                table: "quest_exercises",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StrengthLevelUps",
                table: "quest_exercises",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VitalityLevelUps",
                table: "quest_exercises",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WisdomLevelUps",
                table: "quest_exercises",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Rank",
                table: "hunter_progressions",
                type: "character varying(4)",
                maxLength: 4,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "HunterClass",
                table: "hunter_progressions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "AgilityXp",
                table: "hunter_progressions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EnduranceXp",
                table: "hunter_progressions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FocusXp",
                table: "hunter_progressions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StrengthXp",
                table: "hunter_progressions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VitalityXp",
                table: "hunter_progressions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WisdomXp",
                table: "hunter_progressions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_hunter_progressions",
                table: "hunter_progressions",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_hunter_progressions_UserId",
                table: "hunter_progressions",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_hunter_progressions",
                table: "hunter_progressions");

            migrationBuilder.DropIndex(
                name: "IX_hunter_progressions_UserId",
                table: "hunter_progressions");

            migrationBuilder.DropColumn(
                name: "AgilityLevelUps",
                table: "quest_exercises");

            migrationBuilder.DropColumn(
                name: "EnduranceLevelUps",
                table: "quest_exercises");

            migrationBuilder.DropColumn(
                name: "FocusLevelUps",
                table: "quest_exercises");

            migrationBuilder.DropColumn(
                name: "StrengthLevelUps",
                table: "quest_exercises");

            migrationBuilder.DropColumn(
                name: "VitalityLevelUps",
                table: "quest_exercises");

            migrationBuilder.DropColumn(
                name: "WisdomLevelUps",
                table: "quest_exercises");

            migrationBuilder.DropColumn(
                name: "AgilityXp",
                table: "hunter_progressions");

            migrationBuilder.DropColumn(
                name: "EnduranceXp",
                table: "hunter_progressions");

            migrationBuilder.DropColumn(
                name: "FocusXp",
                table: "hunter_progressions");

            migrationBuilder.DropColumn(
                name: "StrengthXp",
                table: "hunter_progressions");

            migrationBuilder.DropColumn(
                name: "VitalityXp",
                table: "hunter_progressions");

            migrationBuilder.DropColumn(
                name: "WisdomXp",
                table: "hunter_progressions");

            migrationBuilder.RenameTable(
                name: "hunter_progressions",
                newName: "HunterProgressions");

            migrationBuilder.AlterColumn<string>(
                name: "Rank",
                table: "HunterProgressions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(4)",
                oldMaxLength: 4);

            migrationBuilder.AlterColumn<string>(
                name: "HunterClass",
                table: "HunterProgressions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AddPrimaryKey(
                name: "PK_HunterProgressions",
                table: "HunterProgressions",
                column: "Id");
        }
    }
}
