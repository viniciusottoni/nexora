using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestExercises : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "quest_exercises",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ExerciseCatalogProviderId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Sets = table.Column<int>(type: "integer", nullable: false),
                    RepsMin = table.Column<int>(type: "integer", nullable: false),
                    RepsMax = table.Column<int>(type: "integer", nullable: true),
                    RestSeconds = table.Column<int>(type: "integer", nullable: true),
                    TargetRpe = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    VideoUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    XpReward = table.Column<long>(type: "bigint", nullable: false),
                    StrengthXp = table.Column<int>(type: "integer", nullable: false),
                    AgilityXp = table.Column<int>(type: "integer", nullable: false),
                    EnduranceXp = table.Column<int>(type: "integer", nullable: false),
                    VitalityXp = table.Column<int>(type: "integer", nullable: false),
                    FocusXp = table.Column<int>(type: "integer", nullable: false),
                    WisdomXp = table.Column<int>(type: "integer", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    XpEarned = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quest_exercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_quest_exercises_quests_QuestId",
                        column: x => x.QuestId,
                        principalTable: "quests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_quest_exercises_QuestId_Order",
                table: "quest_exercises",
                columns: new[] { "QuestId", "Order" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quest_exercises");
        }
    }
}
