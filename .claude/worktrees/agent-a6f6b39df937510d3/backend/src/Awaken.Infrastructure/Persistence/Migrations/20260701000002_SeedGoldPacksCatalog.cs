using System;
using Awaken.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Inserts the 6 canonical Gold IAP packs (100, 200, 500, 1000, 2000, 5000).
    /// RevenueCatProductId must match the product identifiers registered in the
    /// RevenueCat dashboard (pack_100 … pack_5000).
    /// Prices are NOT stored here — they are pulled from RevenueCat / stores at runtime.
    /// GoldAmount is the server-side-only quantity credited after a successful IAP (ADR-022).
    /// Uses UPSERT so re-running the migration (e.g. dev seed) is idempotent.
    /// gold_pack_500 was previously seeded with type='consumable'; this migration
    /// corrects it to type='pack' so it appears in the Buy-Gold tab.
    /// </summary>
    [DbContext(typeof(AwakenDbContext))]
    [Migration("20260701000002_SeedGoldPacksCatalog")]
    public partial class SeedGoldPacksCatalog : Migration
    {
        private static readonly DateTime SeedUtcNow = new(2026, 7, 1, 0, 0, 1, DateTimeKind.Utc);

        private static readonly Guid GoldPack100Id  = new("c2000001-0000-0000-0000-000000000001");
        private static readonly Guid GoldPack200Id  = new("c2000002-0000-0000-0000-000000000002");
        private static readonly Guid GoldPack500Id  = new("5e7c1a2b-9d3f-4a6e-8b1c-2d4f6a8e0c1b"); // existing row
        private static readonly Guid GoldPack1000Id = new("c2000004-0000-0000-0000-000000000004");
        private static readonly Guid GoldPack2000Id = new("c2000005-0000-0000-0000-000000000005");
        private static readonly Guid GoldPack5000Id = new("c2000006-0000-0000-0000-000000000006");

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Upsert all 6 packs. Existing gold_pack_500 gets type and rarity corrected.
            migrationBuilder.Sql($@"
INSERT INTO ""shop_products"" (
    ""Id"", ""Key"", ""Name"", ""Description"", ""Type"", ""Rarity"",
    ""IsActive"", ""RevenueCatProductId"", ""PriceGold"", ""GoldAmount"",
    ""CreatedAtUtc"", ""UpdatedAtUtc"", ""DeletedAtUtc"",
    ""CreatedByUserId"", ""UpdatedByUserId"", ""IsDeleted""
)
VALUES
    ('{GoldPack100Id}',  'gold_pack_100',  'Pacote de 100 Gold',  NULL, 'pack', 'common',    TRUE, 'pack_100',  NULL,   100, TIMESTAMPTZ '{SeedUtcNow:O}', NULL, NULL, NULL, NULL, FALSE),
    ('{GoldPack200Id}',  'gold_pack_200',  'Pacote de 200 Gold',  NULL, 'pack', 'common',    TRUE, 'pack_200',  NULL,   200, TIMESTAMPTZ '{SeedUtcNow:O}', NULL, NULL, NULL, NULL, FALSE),
    ('{GoldPack500Id}',  'gold_pack_500',  'Pacote de 500 Gold',  NULL, 'pack', 'uncommon',  TRUE, 'pack_500',  NULL,   500, TIMESTAMPTZ '{SeedUtcNow:O}', NULL, NULL, NULL, NULL, FALSE),
    ('{GoldPack1000Id}', 'gold_pack_1000', 'Pacote de 1000 Gold', NULL, 'pack', 'rare',      TRUE, 'pack_1000', NULL,  1000, TIMESTAMPTZ '{SeedUtcNow:O}', NULL, NULL, NULL, NULL, FALSE),
    ('{GoldPack2000Id}', 'gold_pack_2000', 'Pacote de 2000 Gold', NULL, 'pack', 'epic',      TRUE, 'pack_2000', NULL,  2000, TIMESTAMPTZ '{SeedUtcNow:O}', NULL, NULL, NULL, NULL, FALSE),
    ('{GoldPack5000Id}', 'gold_pack_5000', 'Pacote de 5000 Gold', NULL, 'pack', 'legendary', TRUE, 'pack_5000', NULL,  5000, TIMESTAMPTZ '{SeedUtcNow:O}', NULL, NULL, NULL, NULL, FALSE)
ON CONFLICT (""Key"") DO UPDATE SET
    ""Description""        = EXCLUDED.""Description"",
    ""Type""               = EXCLUDED.""Type"",
    ""Rarity""             = EXCLUDED.""Rarity"",
    ""GoldAmount""         = EXCLUDED.""GoldAmount"",
    ""RevenueCatProductId""= EXCLUDED.""RevenueCatProductId"",
    ""IsActive""           = EXCLUDED.""IsActive"",
    ""UpdatedAtUtc""       = EXCLUDED.""CreatedAtUtc"";
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the 5 newly inserted packs; revert gold_pack_500 to its previous state.
            migrationBuilder.Sql($@"
DELETE FROM ""shop_products""
WHERE ""Key"" IN ('gold_pack_100', 'gold_pack_200', 'gold_pack_1000', 'gold_pack_2000', 'gold_pack_5000');

UPDATE ""shop_products""
SET ""Type"" = 'consumable', ""Rarity"" = 'rare', ""GoldAmount"" = 500,
    ""RevenueCatProductId"" = 'rc_gold_pack_500',
    ""Description"" = 'Pacote de Gold comprado com dinheiro real via loja do app.'
WHERE ""Key"" = 'gold_pack_500';
");
        }
    }
}
