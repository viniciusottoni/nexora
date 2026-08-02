using System;
using Awaken.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable enable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// US-229: insere o catalogo inicial da loja usando SQL bruto idempotente.
    /// Isso evita a dependencia de metadata do EF para data seeding em migrations
    /// criadas manualmente.
    /// </summary>
    /// <inheritdoc />
    [DbContext(typeof(AwakenDbContext))]
    [Migration("20260701000001_SeedInitialShopCatalog")]
    public partial class SeedInitialShopCatalog : Migration
    {
        private static readonly DateTime SeedUtcNow = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);

        // Consumiveis
        private static readonly Guid ScrollSubstitutionId = new("a1000001-0000-0000-0000-000000000001");
        private static readonly Guid DungeonCompassId = new("a1000002-0000-0000-0000-000000000002");
        private static readonly Guid DungeonKeyId = new("a1000003-0000-0000-0000-000000000003");
        private static readonly Guid ProtectionSealId = new("a1000004-0000-0000-0000-000000000004");
        private static readonly Guid RecoveryTonicId = new("a1000005-0000-0000-0000-000000000005");
        private static readonly Guid ReturnAmuletId = new("a1000006-0000-0000-0000-000000000006");
        private static readonly Guid FocusPotionId = new("a1000007-0000-0000-0000-000000000007");
        private static readonly Guid FocusPotionLargeId = new("a1000008-0000-0000-0000-000000000008");
        private static readonly Guid LuckPotionId = new("a1000009-0000-0000-0000-000000000009");

        // Cosmeticos - Molduras
        private static readonly Guid FrameRankEId = new("b1000001-0000-0000-0000-000000000001");
        private static readonly Guid FrameRankDId = new("b1000002-0000-0000-0000-000000000002");
        private static readonly Guid FrameRankCId = new("b1000003-0000-0000-0000-000000000003");
        private static readonly Guid FrameRankBId = new("b1000004-0000-0000-0000-000000000004");
        private static readonly Guid FrameRankAId = new("b1000005-0000-0000-0000-000000000005");
        private static readonly Guid FrameRankSId = new("b1000006-0000-0000-0000-000000000006");
        private static readonly Guid FrameRankSsId = new("b1000007-0000-0000-0000-000000000007");
        private static readonly Guid FrameRankSssId = new("b1000008-0000-0000-0000-000000000008");

        // Cosmeticos - Auras / Fundos
        private static readonly Guid AuraDefaultId = new("b1000009-0000-0000-0000-000000000009");
        private static readonly Guid BackgroundPortalId = new("b1000010-0000-0000-0000-000000000010");
        private static readonly Guid BackgroundDungeonId = new("b1000011-0000-0000-0000-000000000011");
        private static readonly Guid BackgroundHunterShadowsId = new("b1000012-0000-0000-0000-000000000012");

        // Cosmeticos - Pergaminhos
        private static readonly Guid ScrollRenameId = new("b1000013-0000-0000-0000-000000000013");
        private static readonly Guid ScrollClassChangeId = new("b1000014-0000-0000-0000-000000000014");

        // Packs
        private static readonly Guid PackStrikerId = new("c1000001-0000-0000-0000-000000000001");
        private static readonly Guid PackRunnerId = new("c1000002-0000-0000-0000-000000000002");
        private static readonly Guid PackGuardianId = new("c1000003-0000-0000-0000-000000000003");
        private static readonly Guid PackShadowId = new("c1000004-0000-0000-0000-000000000004");
        private static readonly Guid PackReawakenedId = new("c1000005-0000-0000-0000-000000000005");

        // Slots de Inventario
        private static readonly Guid InventorySlots05To15Id = new("d1000001-0000-0000-0000-000000000001");
        private static readonly Guid InventorySlots05To20Id = new("d1000002-0000-0000-0000-000000000002");
        private static readonly Guid InventorySlots05To25Id = new("d1000003-0000-0000-0000-000000000003");
        private static readonly Guid InventorySlots05To30Id = new("d1000004-0000-0000-0000-000000000004");

        private sealed record ShopProductSeed(
            Guid Id,
            string Key,
            string Name,
            string? Description,
            string Type,
            string Rarity,
            bool IsActive,
            string? RevenueCatProductId,
            int? PriceGold,
            int? GoldAmount,
            DateTime CreatedAtUtc,
            DateTime? UpdatedAtUtc,
            DateTime? DeletedAtUtc,
            Guid? CreatedByUserId,
            Guid? UpdatedByUserId,
            bool IsDeleted);

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var products = new[]
            {
                new ShopProductSeed(ScrollSubstitutionId, "scroll_substitution", "Pergaminho da Substituicao",
                    "Substitui 1 exercicio por outro compativel com perfil/equipamento.",
                    "consumable", "uncommon", true, null, 90, null, SeedUtcNow, null, null, null, null, false),
                new ShopProductSeed(DungeonCompassId, "dungeon_compass", "Bussola da Dungeon",
                    "Troca a dungeon atual por outra.",
                    "consumable", "uncommon", true, null, 120, null, SeedUtcNow, null, null, null, null, false),
                new ShopProductSeed(DungeonKeyId, "dungeon_key", "Chave da Dungeon",
                    "Libera uma dungeon/treino especial do dia sem afetar quest principal.",
                    "consumable", "epic", true, null, 250, null, SeedUtcNow, null, null, null, null, false),
                new ShopProductSeed(ProtectionSealId, "protection_seal", "Selo de Protecao",
                    "Protege o streak se o usuario falhar 1 dia.",
                    "consumable", "uncommon", true, null, 100, null, SeedUtcNow, null, null, null, null, false),
                new ShopProductSeed(RecoveryTonicId, "recovery_tonic", "Tonico de Recuperacao",
                    "Marca 1 dia como recuperacao ativa sem quebrar streak.",
                    "consumable", "common", true, null, 70, null, SeedUtcNow, null, null, null, null, false),
                new ShopProductSeed(ReturnAmuletId, "return_amulet", "Amuleto de Retorno",
                    "Permite recuperar um streak perdido ontem apenas se treinar hoje.",
                    "consumable", "epic", true, null, 220, null, SeedUtcNow, null, null, null, null, false),
                new ShopProductSeed(FocusPotionId, "focus_potion", "Pocao de Foco",
                    "+25% XP no proximo treino concluido.",
                    "consumable", "uncommon", true, null, 120, null, SeedUtcNow, null, null, null, null, false),
                new ShopProductSeed(FocusPotionLargeId, "focus_potion_large", "Pocao de Foco Grande",
                    "+50% XP no proximo treino concluido.",
                    "consumable", "epic", true, null, 260, null, SeedUtcNow, null, null, null, null, false),
                new ShopProductSeed(LuckPotionId, "luck_potion", "Pocao da Sorte",
                    "Bonus de Gold encontrado na quest conforme regra de economia.",
                    "consumable", "common", true, null, 90, null, SeedUtcNow, null, null, null, null, false),

                new ShopProductSeed(FrameRankEId, "frame_rank_e", "Moldura Especial - Rank E",
                    "Moldura decorativa para rank E.",
                    "cosmetic", "common", true, null, 250, null, SeedUtcNow, null, null, null, null, false),
                new ShopProductSeed(FrameRankDId, "frame_rank_d", "Moldura Especial - Rank D",
                    "Moldura decorativa para rank D.",
                    "cosmetic", "common", true, null, 350, null, SeedUtcNow, null, null, null, null, false),
                new ShopProductSeed(FrameRankCId, "frame_rank_c", "Moldura Especial - Rank C",
                    "Moldura decorativa para rank C.",
                    "cosmetic", "uncommon", true, null, 500, null, SeedUtcNow, null, null, null, null, false),
                new ShopProductSeed(FrameRankBId, "frame_rank_b", "Moldura Especial - Rank B",
                    "Moldura decorativa para rank B.",
                    "cosmetic", "rare", true, null, 750, null, SeedUtcNow, null, null, null, null, false),
                new ShopProductSeed(FrameRankAId, "frame_rank_a", "Moldura Especial - Rank A",
                    "Moldura decorativa para rank A.",
                    "cosmetic", "rare", true, null, 1000, null, SeedUtcNow, null, null, null, null, false),
                new ShopProductSeed(FrameRankSId, "frame_rank_s", "Moldura Especial - Rank S",
                    "Moldura decorativa para rank S.",
                    "cosmetic", "epic", true, null, 1500, null, SeedUtcNow, null, null, null, null, false),
                new ShopProductSeed(FrameRankSsId, "frame_rank_ss", "Moldura Especial - Rank SS",
                    "Moldura decorativa para rank SS.",
                    "cosmetic", "epic", true, null, 2000, null, SeedUtcNow, null, null, null, null, false),
                new ShopProductSeed(FrameRankSssId, "frame_rank_sss", "Moldura Especial - Rank SSS",
                    "Moldura decorativa para rank SSS.",
                    "cosmetic", "legendary", true, null, 3000, null, SeedUtcNow, null, null, null, null, false),

                new ShopProductSeed(AuraDefaultId, "aura_default", "Aura",
                    "Aura decorativa para o perfil.",
                    "cosmetic", "epic", true, null, 600, null, SeedUtcNow, null, null, null, null, false),
                new ShopProductSeed(BackgroundPortalId, "background_portal", "Fundo: Portal",
                    "Fundo de portal para o perfil.",
                    "cosmetic", "uncommon", true, null, 450, null, SeedUtcNow, null, null, null, null, false),
                new ShopProductSeed(BackgroundDungeonId, "background_dungeon", "Fundo: Dungeon",
                    "Fundo de dungeon para o perfil.",
                    "cosmetic", "rare", true, null, 600, null, SeedUtcNow, null, null, null, null, false),
                new ShopProductSeed(BackgroundHunterShadowsId, "background_hunter_shadows", "Fundo: Sombras do Hunter",
                    "Fundo de sombras do hunter para o perfil.",
                    "cosmetic", "epic", true, null, 900, null, SeedUtcNow, null, null, null, null, false),

                new ShopProductSeed(ScrollRenameId, "scroll_rename", "Pergaminho de Renomeacao",
                    "Permite mudar nickname/codinome.",
                    "cosmetic", "common", true, null, 200, null, SeedUtcNow, null, null, null, null, false),
                new ShopProductSeed(ScrollClassChangeId, "scroll_class_change", "Pergaminho da Classe",
                    "Permite mudar classe.",
                    "cosmetic", "uncommon", true, null, 350, null, SeedUtcNow, null, null, null, null, false),

                new ShopProductSeed(PackStrikerId, "pack_striker", "Pack Striker",
                    "Pacote de itens para o estilo Striker.",
                    "pack", "uncommon", true, null, 1500, null, SeedUtcNow, null, null, null, null, false),
                new ShopProductSeed(PackRunnerId, "pack_runner", "Pack Runner",
                    "Pacote de itens para o estilo Runner.",
                    "pack", "uncommon", true, null, 1500, null, SeedUtcNow, null, null, null, null, false),
                new ShopProductSeed(PackGuardianId, "pack_guardian", "Pack Guardian",
                    "Pacote de itens para o estilo Guardian.",
                    "pack", "uncommon", true, null, 1500, null, SeedUtcNow, null, null, null, null, false),
                new ShopProductSeed(PackShadowId, "pack_shadow", "Pack Shadow",
                    "Pacote de itens para o estilo Shadow.",
                    "pack", "rare", true, null, 1800, null, SeedUtcNow, null, null, null, null, false),
                new ShopProductSeed(PackReawakenedId, "pack_reawakened", "Pack Reawakened",
                    "Pacote de itens para o estilo Reawakened.",
                    "pack", "rare", true, null, 2000, null, SeedUtcNow, null, null, null, null, false),

                new ShopProductSeed(InventorySlots05To15Id, "inventory_slots_05_10_15", "+5 Slots de Inventario",
                    "Expande o inventario em 5 slots (faixa 10-15).",
                    "slot", "common", true, null, 300, null, SeedUtcNow, null, null, null, null, false),
                new ShopProductSeed(InventorySlots05To20Id, "inventory_slots_05_15_20", "+5 Slots de Inventario",
                    "Expande o inventario em 5 slots (faixa 15-20).",
                    "slot", "common", true, null, 500, null, SeedUtcNow, null, null, null, null, false),
                new ShopProductSeed(InventorySlots05To25Id, "inventory_slots_05_20_25", "+5 Slots de Inventario",
                    "Expande o inventario em 5 slots (faixa 20-25).",
                    "slot", "uncommon", true, null, 800, null, SeedUtcNow, null, null, null, null, false),
                new ShopProductSeed(InventorySlots05To30Id, "inventory_slots_05_25_30", "+5 Slots de Inventario",
                    "Expande o inventario em 5 slots (faixa 25-30).",
                    "slot", "uncommon", true, null, 1200, null, SeedUtcNow, null, null, null, null, false),
            };

            var sql = $@"
INSERT INTO ""shop_products"" (
    ""Id"", ""Key"", ""Name"", ""Description"", ""Type"", ""Rarity"",
    ""IsActive"", ""RevenueCatProductId"", ""PriceGold"", ""GoldAmount"",
    ""CreatedAtUtc"", ""UpdatedAtUtc"", ""DeletedAtUtc"",
    ""CreatedByUserId"", ""UpdatedByUserId"", ""IsDeleted""
)
VALUES
{string.Join(",\n", products.Select(BuildValuesClause))}
ON CONFLICT (""Key"") DO NOTHING;
";

            migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var ids = new[]
            {
                ScrollSubstitutionId,
                DungeonCompassId,
                DungeonKeyId,
                ProtectionSealId,
                RecoveryTonicId,
                ReturnAmuletId,
                FocusPotionId,
                FocusPotionLargeId,
                LuckPotionId,
                FrameRankEId,
                FrameRankDId,
                FrameRankCId,
                FrameRankBId,
                FrameRankAId,
                FrameRankSId,
                FrameRankSsId,
                FrameRankSssId,
                AuraDefaultId,
                BackgroundPortalId,
                BackgroundDungeonId,
                BackgroundHunterShadowsId,
                ScrollRenameId,
                ScrollClassChangeId,
                PackStrikerId,
                PackRunnerId,
                PackGuardianId,
                PackShadowId,
                PackReawakenedId,
                InventorySlots05To15Id,
                InventorySlots05To20Id,
                InventorySlots05To25Id,
                InventorySlots05To30Id,
            };

            migrationBuilder.Sql($@"
DELETE FROM ""shop_products""
WHERE ""Id"" IN ({string.Join(", ", ids.Select(SqlGuid))});
");
        }

        private static string BuildValuesClause(ShopProductSeed product) =>
            $"({SqlGuid(product.Id)}, {SqlString(product.Key)}, {SqlString(product.Name)}, {SqlString(product.Description)}, " +
            $"{SqlString(product.Type)}, {SqlString(product.Rarity)}, {SqlBool(product.IsActive)}, " +
            $"{SqlString(product.RevenueCatProductId)}, {SqlNullableInt(product.PriceGold)}, {SqlNullableInt(product.GoldAmount)}, " +
            $"{SqlTimestamp(product.CreatedAtUtc)}, {SqlNullableTimestamp(product.UpdatedAtUtc)}, {SqlNullableTimestamp(product.DeletedAtUtc)}, " +
            $"{SqlNullableGuid(product.CreatedByUserId)}, {SqlNullableGuid(product.UpdatedByUserId)}, {SqlBool(product.IsDeleted)})";

        private static string SqlGuid(Guid value) => $"'{value:D}'";

        private static string SqlNullableGuid(Guid? value) => value.HasValue ? SqlGuid(value.Value) : "NULL";

        private static string SqlString(string? value) =>
            value is null ? "NULL" : $"'{value.Replace("'", "''")}'";

        private static string SqlNullableInt(int? value) => value.HasValue ? value.Value.ToString() : "NULL";

        private static string SqlBool(bool value) => value ? "TRUE" : "FALSE";

        private static string SqlTimestamp(DateTime value) => $"TIMESTAMPTZ '{value:O}'";

        private static string SqlNullableTimestamp(DateTime? value) =>
            value.HasValue ? SqlTimestamp(value.Value) : "NULL";
    }
}
