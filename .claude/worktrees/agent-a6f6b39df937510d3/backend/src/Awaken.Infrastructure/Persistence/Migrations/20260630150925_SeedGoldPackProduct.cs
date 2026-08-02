using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// US-226: produto de exemplo de "pacote de Gold" comprado com dinheiro real
    /// via IAP. GoldAmount define exclusivamente, no servidor, a quantidade de
    /// Gold creditada — o app nunca informa esse valor (RN-001/RN-002).
    /// RevenueCatProductId é um placeholder até a integração real ser configurada
    /// no painel do RevenueCat.
    /// </summary>
    /// <inheritdoc />
    public partial class SeedGoldPackProduct : Migration
    {
        private static readonly Guid GoldPack500Id = new("5e7c1a2b-9d3f-4a6e-8b1c-2d4f6a8e0c1b");
        private static readonly DateTime SeedUtcNow = new(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc);

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "shop_products",
                columns: new[]
                {
                    "Id", "Key", "Name", "Description", "Type", "Rarity",
                    "IsActive", "RevenueCatProductId", "PriceGold", "GoldAmount",
                    "CreatedAtUtc", "UpdatedAtUtc", "DeletedAtUtc",
                    "CreatedByUserId", "UpdatedByUserId", "IsDeleted",
                },
                values: new object[,]
                {
                    {
                        GoldPack500Id, "gold_pack_500", "Pacote de 500 Gold",
                        "Pacote de Gold comprado com dinheiro real via loja do app.",
                        "consumable", "rare", true, "rc_gold_pack_500", null, 500,
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
                keyValues: new object[] { GoldPack500Id });
        }
    }
}
