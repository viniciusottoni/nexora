using System;
using Awaken.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// US-230: cria tabela item_usage_records para rastrear uso de itens
    /// consumíveis por usuário por período (diário/semanal), permitindo
    /// enforcement de limites de uso configurados nos IItemEffectHandler.
    /// </summary>
    /// <inheritdoc />
    [DbContext(typeof(AwakenDbContext))]
    [Migration("20260701000002_AddItemUsageRecords")]
    public partial class AddItemUsageRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "item_usage_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PeriodStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsageCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_usage_records", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_item_usage_records_UserId_ItemKey",
                table: "item_usage_records",
                columns: new[] { "UserId", "ItemKey" });

            migrationBuilder.CreateIndex(
                name: "IX_item_usage_records_UserId_ItemKey_PeriodStartUtc",
                table: "item_usage_records",
                columns: new[] { "UserId", "ItemKey", "PeriodStartUtc" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "item_usage_records");
        }
    }
}
