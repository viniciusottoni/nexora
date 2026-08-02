using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// US-188: vincula os itens legados da ADR-022 (ReforgeScroll, DungeonStone)
    /// ao novo catálogo orientado a dados (shop_products), preservando os
    /// preços em Gold que antes viviam hardcoded no ShopCatalog estático.
    /// </summary>
    /// <inheritdoc />
    public partial class SeedLegacyShopProducts : Migration
    {
        private static readonly Guid ReforgeScrollId = new("8b1f7a8e-2e0a-4d3a-8c9b-1f6a2e0a4d3a");
        private static readonly Guid DungeonStoneId = new("3c2e9b5d-7f1a-4e6b-9a0c-2e9b5d7f1a4e");
        private static readonly DateTime SeedUtcNow = new(2026, 6, 29, 0, 0, 0, DateTimeKind.Utc);

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "shop_products",
                columns: new[]
                {
                    "Id", "Key", "Name", "Description", "Type", "Rarity",
                    "IsActive", "RevenueCatProductId", "PriceGold",
                    "CreatedAtUtc", "UpdatedAtUtc", "DeletedAtUtc",
                    "CreatedByUserId", "UpdatedByUserId", "IsDeleted",
                },
                values: new object[,]
                {
                    {
                        ReforgeScrollId, "reforja_scroll", "Pergaminho da Reforja",
                        "Regenera uma quest diaria alem do limite gratuito.",
                        "consumable", "rare", true, null, 150,
                        SeedUtcNow, null, null, null, null, false,
                    },
                    {
                        DungeonStoneId, "pedra_dungeon", "Pedra da Dungeon",
                        null, "consumable", "common", true, null, 120,
                        SeedUtcNow, null, null, null, null, false,
                    },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "shop_products",
                keyColumn: "Id",
                keyValues: new object[] { ReforgeScrollId, DungeonStoneId });
        }
    }
}
