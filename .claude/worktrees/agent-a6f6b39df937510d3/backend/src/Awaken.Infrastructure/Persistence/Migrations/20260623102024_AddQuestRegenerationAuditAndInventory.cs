using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestRegenerationAuditAndInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppliedFiltersJson",
                table: "quests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GenerationMethod",
                table: "quests",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GenerationReason",
                table: "quests",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileSnapshotJson",
                table: "quests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RegenerationCount",
                table: "quests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "inventory_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_items", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_items_UserId_ItemKey",
                table: "inventory_items",
                columns: new[] { "UserId", "ItemKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_items");

            migrationBuilder.DropColumn(
                name: "AppliedFiltersJson",
                table: "quests");

            migrationBuilder.DropColumn(
                name: "GenerationMethod",
                table: "quests");

            migrationBuilder.DropColumn(
                name: "GenerationReason",
                table: "quests");

            migrationBuilder.DropColumn(
                name: "ProfileSnapshotJson",
                table: "quests");

            migrationBuilder.DropColumn(
                name: "RegenerationCount",
                table: "quests");
        }
    }
}
