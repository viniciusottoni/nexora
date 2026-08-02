using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Quest history queries (US-183)
            migrationBuilder.CreateIndex(
                name: "IX_quests_UserId_CreatedAtUtc",
                table: "quests",
                columns: new[] { "UserId", "CreatedAtUtc" });

            // Active quest lookup
            migrationBuilder.CreateIndex(
                name: "IX_quests_UserId_Status",
                table: "quests",
                columns: new[] { "UserId", "Status" });

            // Quest log lookups by quest
            migrationBuilder.CreateIndex(
                name: "IX_quest_exercises_QuestId",
                table: "quest_exercises",
                column: "QuestId");

            // Inventory lookup by user
            migrationBuilder.CreateIndex(
                name: "IX_inventory_items_UserId",
                table: "inventory_items",
                column: "UserId");

            // Notification log by user and time
            migrationBuilder.CreateIndex(
                name: "IX_notification_logs_UserId_CreatedAtUtc",
                table: "notification_logs",
                columns: new[] { "UserId", "CreatedAtUtc" });

            // Support ticket quick lookups
            migrationBuilder.CreateIndex(
                name: "IX_support_tickets_CreatedAtUtc",
                table: "support_tickets",
                column: "CreatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_quests_UserId_CreatedAtUtc",
                table: "quests");

            migrationBuilder.DropIndex(
                name: "IX_quests_UserId_Status",
                table: "quests");

            migrationBuilder.DropIndex(
                name: "IX_quest_exercises_QuestId",
                table: "quest_exercises");

            migrationBuilder.DropIndex(
                name: "IX_inventory_items_UserId",
                table: "inventory_items");

            migrationBuilder.DropIndex(
                name: "IX_notification_logs_UserId_CreatedAtUtc",
                table: "notification_logs");

            migrationBuilder.DropIndex(
                name: "IX_support_tickets_CreatedAtUtc",
                table: "support_tickets");
        }
    }
}
