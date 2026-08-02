using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260629050000_FixPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_support_tickets_CreatedAtUtc",
                table: "support_tickets");

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
                name: "IX_notification_logs_UserId_CreatedAtUtc",
                table: "notification_logs");

            migrationBuilder.DropIndex(
                name: "IX_inventory_items_UserId",
                table: "inventory_items");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_support_tickets_CreatedAtUtc",
                table: "support_tickets",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_quests_UserId_CreatedAtUtc",
                table: "quests",
                columns: new[] { "UserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_quests_UserId_Status",
                table: "quests",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_quest_exercises_QuestId",
                table: "quest_exercises",
                column: "QuestId");

            migrationBuilder.CreateIndex(
                name: "IX_notification_logs_UserId_CreatedAtUtc",
                table: "notification_logs",
                columns: new[] { "UserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_items_UserId",
                table: "inventory_items",
                column: "UserId");
        }
    }
}
