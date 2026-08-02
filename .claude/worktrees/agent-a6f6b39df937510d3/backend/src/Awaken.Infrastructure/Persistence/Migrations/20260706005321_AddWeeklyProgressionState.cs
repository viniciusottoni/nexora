using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWeeklyProgressionState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WeeklyProgressionPlanJson",
                table: "quests",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "weekly_progression_states",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    WeekAnchorDate = table.Column<DateOnly>(type: "date", nullable: false),
                    MesocycleWeekIndex = table.Column<int>(type: "integer", nullable: false),
                    ProfileSnapshotHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ConsecutiveEasyWeeks = table.Column<int>(type: "integer", nullable: false),
                    ConsecutiveHardWeeks = table.Column<int>(type: "integer", nullable: false),
                    DeloadDue = table.Column<bool>(type: "boolean", nullable: false),
                    LastDecision = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    LastAxis = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    VolumeSetsDelta = table.Column<int>(type: "integer", nullable: false),
                    RpeDelta = table.Column<int>(type: "integer", nullable: false),
                    RestSecondsDelta = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weekly_progression_states", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_weekly_progression_states_UserId",
                table: "weekly_progression_states",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "weekly_progression_states");

            migrationBuilder.DropColumn(
                name: "WeeklyProgressionPlanJson",
                table: "quests");
        }
    }
}
