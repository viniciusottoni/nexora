using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "quest_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    XpEarned = table.Column<long>(type: "bigint", nullable: false),
                    StrengthXpEarned = table.Column<int>(type: "integer", nullable: false),
                    AgilityXpEarned = table.Column<int>(type: "integer", nullable: false),
                    EnduranceXpEarned = table.Column<int>(type: "integer", nullable: false),
                    VitalityXpEarned = table.Column<int>(type: "integer", nullable: false),
                    FocusXpEarned = table.Column<int>(type: "integer", nullable: false),
                    WisdomXpEarned = table.Column<int>(type: "integer", nullable: false),
                    StrengthPointsGranted = table.Column<int>(type: "integer", nullable: false),
                    AgilityPointsGranted = table.Column<int>(type: "integer", nullable: false),
                    EndurancePointsGranted = table.Column<int>(type: "integer", nullable: false),
                    VitalityPointsGranted = table.Column<int>(type: "integer", nullable: false),
                    FocusPointsGranted = table.Column<int>(type: "integer", nullable: false),
                    ItemsEarnedJson = table.Column<string>(type: "text", nullable: true),
                    XpPenaltyApplied = table.Column<long>(type: "bigint", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quest_logs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_quest_logs_QuestId",
                table: "quest_logs",
                column: "QuestId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quest_logs");
        }
    }
}
