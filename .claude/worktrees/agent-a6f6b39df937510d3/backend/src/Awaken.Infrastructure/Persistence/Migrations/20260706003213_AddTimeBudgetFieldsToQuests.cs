using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeBudgetFieldsToQuests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EstimatedDurationSeconds",
                table: "quests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimeAdjustmentApplied",
                table: "quests",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TimeBudgetSeconds",
                table: "quests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkoutTimeModelVersion",
                table: "quests",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimatedDurationSeconds",
                table: "quests");

            migrationBuilder.DropColumn(
                name: "TimeAdjustmentApplied",
                table: "quests");

            migrationBuilder.DropColumn(
                name: "TimeBudgetSeconds",
                table: "quests");

            migrationBuilder.DropColumn(
                name: "WorkoutTimeModelVersion",
                table: "quests");
        }
    }
}
