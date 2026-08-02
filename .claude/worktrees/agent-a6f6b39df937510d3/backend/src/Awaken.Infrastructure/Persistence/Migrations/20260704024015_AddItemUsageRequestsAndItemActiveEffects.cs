using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddItemUsageRequestsAndItemActiveEffects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EquippedAuraKey",
                table: "hunter_progressions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EquippedBackgroundKey",
                table: "hunter_progressions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EquippedFrameKey",
                table: "hunter_progressions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecentLostStreakDays",
                table: "hunter_progressions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "item_active_effects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EffectType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "active"),
                    XpBoostMultiplier = table.Column<decimal>(type: "numeric(4,2)", nullable: true),
                    StreakDaysToRestore = table.Column<int>(type: "integer", nullable: true),
                    EffectDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActivatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConsumedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_active_effects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "item_usage_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UseRequestId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    EffectType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RemainingQuantity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_usage_requests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_item_active_effects_UserId_EffectType_EffectDateUtc",
                table: "item_active_effects",
                columns: new[] { "UserId", "EffectType", "EffectDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_item_active_effects_UserId_EffectType_Status",
                table: "item_active_effects",
                columns: new[] { "UserId", "EffectType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_item_usage_requests_UseRequestId",
                table: "item_usage_requests",
                column: "UseRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_item_usage_requests_UserId_ItemKey_CreatedAtUtc",
                table: "item_usage_requests",
                columns: new[] { "UserId", "ItemKey", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "item_active_effects");

            migrationBuilder.DropTable(
                name: "item_usage_requests");

            migrationBuilder.DropColumn(
                name: "EquippedAuraKey",
                table: "hunter_progressions");

            migrationBuilder.DropColumn(
                name: "EquippedBackgroundKey",
                table: "hunter_progressions");

            migrationBuilder.DropColumn(
                name: "EquippedFrameKey",
                table: "hunter_progressions");

            migrationBuilder.DropColumn(
                name: "RecentLostStreakDays",
                table: "hunter_progressions");
        }
    }
}
