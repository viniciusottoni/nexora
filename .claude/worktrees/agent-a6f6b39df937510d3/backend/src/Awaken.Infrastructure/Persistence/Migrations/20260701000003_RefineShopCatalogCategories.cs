using System;
using System.Linq;
using Awaken.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable enable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// EPIC-021: refina o catalogo da loja para as 4 categorias oficiais
    /// (consumable, cosmetic, profile, pack), adiciona as colunas Flavor e
    /// UsageLimit e garante que apenas os itens definidos no design existam.
    ///
    /// - Novas colunas: Flavor (frase tematica) e UsageLimit (codigo semantico).
    /// - Remove os slots de inventario (fora do escopo do design atual).
    /// - Move Pergaminho de Renomeacao/Classe de "cosmetic" para "profile".
    /// - Adiciona Pergaminho da Reforja e Pedra da Dungeon (consumiveis).
    /// - UPSERT de todos os itens com efeito, flavor, raridade, preco e limite
    ///   de uso exatamente como no design (idempotente via ON CONFLICT).
    ///
    /// Os pacotes de Gold (gold_pack_*, canal IAP) NAO sao tocados aqui.
    /// </summary>
    /// <inheritdoc />
    [DbContext(typeof(AwakenDbContext))]
    [Migration("20260701000003_RefineShopCatalogCategories")]
    public partial class RefineShopCatalogCategories : Migration
    {
        private static readonly DateTime SeedUtcNow = new(2026, 7, 1, 0, 0, 2, DateTimeKind.Utc);

        // Novos consumiveis (os demais itens ja possuem Id do seed inicial).
        private static readonly Guid ReforgeScrollId = new("a1000010-0000-0000-0000-000000000010");
        private static readonly Guid DungeonStoneId = new("a1000011-0000-0000-0000-000000000011");

        // Slots de inventario removidos por esta migration.
        private static readonly Guid[] InventorySlotIds =
        {
            new("d1000001-0000-0000-0000-000000000001"),
            new("d1000002-0000-0000-0000-000000000002"),
            new("d1000003-0000-0000-0000-000000000003"),
            new("d1000004-0000-0000-0000-000000000004"),
        };

        private sealed record ShopProductSeed(
            Guid Id,
            string Key,
            string Name,
            string Description,
            string Type,
            string Rarity,
            int PriceGold,
            string Flavor,
            string UsageLimit);

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Flavor",
                table: "shop_products",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsageLimit",
                table: "shop_products",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            // Remove itens fora do escopo do design (slots de inventario).
            migrationBuilder.Sql($@"
DELETE FROM ""shop_products""
WHERE ""Id"" IN ({string.Join(", ", InventorySlotIds.Select(SqlGuid))});
");

            // UPSERT de todo o catalogo de itens (4 categorias).
            var products = BuildCatalog();
            var sql = $@"
INSERT INTO ""shop_products"" (
    ""Id"", ""Key"", ""Name"", ""Description"", ""Type"", ""Rarity"",
    ""IsActive"", ""RevenueCatProductId"", ""PriceGold"", ""GoldAmount"",
    ""Flavor"", ""UsageLimit"",
    ""CreatedAtUtc"", ""UpdatedAtUtc"", ""DeletedAtUtc"",
    ""CreatedByUserId"", ""UpdatedByUserId"", ""IsDeleted""
)
VALUES
{string.Join(",\n", products.Select(BuildValuesClause))}
ON CONFLICT (""Key"") DO UPDATE SET
    ""Name"" = EXCLUDED.""Name"",
    ""Description"" = EXCLUDED.""Description"",
    ""Type"" = EXCLUDED.""Type"",
    ""Rarity"" = EXCLUDED.""Rarity"",
    ""IsActive"" = EXCLUDED.""IsActive"",
    ""PriceGold"" = EXCLUDED.""PriceGold"",
    ""Flavor"" = EXCLUDED.""Flavor"",
    ""UsageLimit"" = EXCLUDED.""UsageLimit"",
    ""UpdatedAtUtc"" = EXCLUDED.""CreatedAtUtc"";
";
            migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverte os tipos de perfil para cosmetic (estado anterior).
            migrationBuilder.Sql(@"
UPDATE ""shop_products"" SET ""Type"" = 'cosmetic'
WHERE ""Key"" IN ('scroll_rename', 'scroll_class_change');
");

            // Remove os consumiveis adicionados por esta migration.
            migrationBuilder.Sql($@"
DELETE FROM ""shop_products""
WHERE ""Id"" IN ({SqlGuid(ReforgeScrollId)}, {SqlGuid(DungeonStoneId)});
");

            migrationBuilder.DropColumn(name: "UsageLimit", table: "shop_products");
            migrationBuilder.DropColumn(name: "Flavor", table: "shop_products");
        }

        private static ShopProductSeed[] BuildCatalog() => new[]
        {
            // ── Consumiveis ─────────────────────────────────────────────────
            new ShopProductSeed(ReforgeScrollId, "reforja_scroll", "Pergaminho da Reforja",
                "Regenera a quest diaria alem do limite gratuito.",
                "consumable", "uncommon", 150, "Reescreva o destino da quest de hoje.", "daily_1"),
            new ShopProductSeed(new("a1000001-0000-0000-0000-000000000001"), "scroll_substitution", "Pergaminho da Substituicao",
                "Substitui 1 exercicio por outro compativel com perfil/equipamento.",
                "consumable", "common", 90, "Troque um exercicio sem quebrar a missao.", "daily_2"),
            new ShopProductSeed(new("a1000002-0000-0000-0000-000000000002"), "dungeon_compass", "Bussola da Dungeon",
                "Troca a Dungeon por outra.",
                "consumable", "uncommon", 120, "Reoriente sua missao para outro foco.", "daily_1"),
            new ShopProductSeed(new("a1000003-0000-0000-0000-000000000003"), "dungeon_key", "Chave da Dungeon",
                "Libera uma dungeon/treino especial do dia, sem afetar a quest principal.",
                "consumable", "epic", 250, "Abre uma dungeon/treino opcional.", "daily_1"),
            new ShopProductSeed(new("a1000004-0000-0000-0000-000000000004"), "protection_seal", "Selo de Protecao",
                "Protege o streak se o usuario falhar 1 dia.",
                "consumable", "uncommon", 100, "O System te cobre por um dia.", "max_active_2"),
            new ShopProductSeed(new("a1000005-0000-0000-0000-000000000005"), "recovery_tonic", "Tonico de Recuperacao",
                "Marca 1 dia como recuperacao ativa sem quebrar streak.",
                "consumable", "common", 70, "Descanso tambem faz parte da evolucao.", "weekly_2"),
            new ShopProductSeed(new("a1000006-0000-0000-0000-000000000006"), "return_amulet", "Amuleto de Retorno",
                "Permite recuperar um streak perdido ontem, apenas se treinar hoje.",
                "consumable", "epic", 220, "Volte antes que o portal feche.", "weekly_1"),
            new ShopProductSeed(new("a1000007-0000-0000-0000-000000000007"), "focus_potion", "Pocao de Foco",
                "+25% XP no proximo treino concluido.",
                "consumable", "uncommon", 120, "Seu esforco ecoa mais forte.", "daily_1"),
            new ShopProductSeed(new("a1000008-0000-0000-0000-000000000008"), "focus_potion_large", "Pocao de Foco Grande",
                "+50% XP no proximo treino concluido.",
                "consumable", "epic", 260, "Concentracao maxima na quest.", "weekly_1"),
            new ShopProductSeed(new("a1000009-0000-0000-0000-000000000009"), "luck_potion", "Pocao da Sorte",
                "Bonus de gold encontrado na quest.",
                "consumable", "common", 90, "Recompensa por trabalho real.", "daily_1"),
            new ShopProductSeed(DungeonStoneId, "pedra_dungeon", "Pedra da Dungeon",
                "Material para desbloquear dungeons especiais.",
                "consumable", "uncommon", 120, "Fragmento de uma dungeon colapsada.", "daily_1"),

            // ── Cosmeticos ──────────────────────────────────────────────────
            new ShopProductSeed(new("b1000001-0000-0000-0000-000000000001"), "frame_rank_e", "Moldura Especial - Rank E",
                "Borda brilhante no card na cor do ranking E.",
                "cosmetic", "common", 250, "Borda especial para o ranking E.", "one_time"),
            new ShopProductSeed(new("b1000002-0000-0000-0000-000000000002"), "frame_rank_d", "Moldura Especial - Rank D",
                "Borda brilhante no card na cor do ranking D.",
                "cosmetic", "common", 350, "Borda especial para o ranking D.", "one_time"),
            new ShopProductSeed(new("b1000003-0000-0000-0000-000000000003"), "frame_rank_c", "Moldura Especial - Rank C",
                "Borda brilhante no card na cor do ranking C.",
                "cosmetic", "uncommon", 500, "Borda especial para o ranking C.", "one_time"),
            new ShopProductSeed(new("b1000004-0000-0000-0000-000000000004"), "frame_rank_b", "Moldura Especial - Rank B",
                "Borda brilhante no card na cor do ranking B.",
                "cosmetic", "rare", 750, "Borda especial para o ranking B.", "one_time"),
            new ShopProductSeed(new("b1000005-0000-0000-0000-000000000005"), "frame_rank_a", "Moldura Especial - Rank A",
                "Borda brilhante no card na cor do ranking A.",
                "cosmetic", "rare", 1000, "Borda especial para o ranking A.", "one_time"),
            new ShopProductSeed(new("b1000006-0000-0000-0000-000000000006"), "frame_rank_s", "Moldura Especial - Rank S",
                "Borda brilhante no card na cor do ranking S.",
                "cosmetic", "epic", 1500, "Borda especial para o ranking S.", "one_time"),
            new ShopProductSeed(new("b1000007-0000-0000-0000-000000000007"), "frame_rank_ss", "Moldura Especial - Rank SS",
                "Borda brilhante no card na cor do ranking SS.",
                "cosmetic", "epic", 2000, "Borda especial para o ranking SS.", "one_time"),
            new ShopProductSeed(new("b1000008-0000-0000-0000-000000000008"), "frame_rank_sss", "Moldura Especial - Rank SSS",
                "Borda brilhante no card na cor do ranking SSS.",
                "cosmetic", "legendary", 3000, "Borda especial para o ranking SSS.", "one_time"),
            new ShopProductSeed(new("b1000009-0000-0000-0000-000000000009"), "aura_default", "Aura",
                "Particulas discretas no card.",
                "cosmetic", "epic", 600, "Um grande poder emana.", "one_time"),
            new ShopProductSeed(new("b1000010-0000-0000-0000-000000000010"), "background_portal", "Fundo: Portal",
                "Background do card.",
                "cosmetic", "uncommon", 450, "O caminho diario do hunter visivel no card.", "one_time"),
            new ShopProductSeed(new("b1000011-0000-0000-0000-000000000011"), "background_dungeon", "Fundo: Dungeon",
                "Background do card.",
                "cosmetic", "rare", 600, "O desafio especial do hunter visivel no card.", "one_time"),
            new ShopProductSeed(new("b1000012-0000-0000-0000-000000000012"), "background_hunter_shadows", "Fundo: Sombras do Hunter",
                "Background do card.",
                "cosmetic", "epic", 900, "O hunter visivel no card.", "one_time"),

            // ── Perfil ──────────────────────────────────────────────────────
            new ShopProductSeed(new("b1000013-0000-0000-0000-000000000013"), "scroll_rename", "Pergaminho de Renomeacao",
                "Permite mudar nickname/codinome.",
                "profile", "common", 200, "Escolha novamente seu codinome.", "monthly_1"),
            new ShopProductSeed(new("b1000014-0000-0000-0000-000000000014"), "scroll_class_change", "Pergaminho da Classe",
                "Permite mudar classe.",
                "profile", "uncommon", 350, "Escolha uma nova classe para seu nivel.", "monthly_1"),

            // ── Packs ───────────────────────────────────────────────────────
            new ShopProductSeed(new("c1000001-0000-0000-0000-000000000001"), "pack_striker", "Pack Striker",
                "Avatares do Striker, classe de striker, 1 pergaminho de renomeacao, 1 pergaminho da reforja, 1 pergaminho da substituicao, 1 selo de protecao, 1 tonico de recuperacao, 1 amuleto de retorno, 1 pocao do foco, 1 pocao da sorte, 1 pedra da dungeon, 1 bussola da dungeon.",
                "pack", "uncommon", 1500, "Pack para iniciantes.", "one_time"),
            new ShopProductSeed(new("c1000002-0000-0000-0000-000000000002"), "pack_runner", "Pack Runner",
                "Avatares do Runner, classe de runner, 1 pergaminho de renomeacao, 1 pergaminho da reforja, 1 pergaminho da substituicao, 1 selo de protecao, 1 tonico de recuperacao, 1 amuleto de retorno, 1 pocao do foco, 1 pocao da sorte, 1 pedra da dungeon, 1 bussola da dungeon.",
                "pack", "uncommon", 1500, "Pack para iniciantes.", "one_time"),
            new ShopProductSeed(new("c1000003-0000-0000-0000-000000000003"), "pack_guardian", "Pack Guardian",
                "Avatares do Guardian, classe de guardian, 1 pergaminho de renomeacao, 1 pergaminho da reforja, 1 pergaminho da substituicao, 1 selo de protecao, 1 tonico de recuperacao, 1 amuleto de retorno, 1 pocao do foco, 1 pocao da sorte, 1 pedra da dungeon, 1 bussola da dungeon.",
                "pack", "uncommon", 1500, "Pack para iniciantes.", "one_time"),
            new ShopProductSeed(new("c1000004-0000-0000-0000-000000000004"), "pack_shadow", "Pack Shadow",
                "Avatares do Shadow, classe de shadow, 1 pergaminho de renomeacao, 1 pergaminho da reforja, 1 pergaminho da substituicao, 1 selo de protecao, 1 tonico de recuperacao, 1 amuleto de retorno, 1 pocao do foco, 1 pocao da sorte, 1 pedra da dungeon, 1 bussola da dungeon.",
                "pack", "rare", 1800, "Pack para iniciantes.", "one_time"),
            new ShopProductSeed(new("c1000005-0000-0000-0000-000000000005"), "pack_reawakened", "Pack Reawakened",
                "Avatares do Reawakened, classe de reawakened, 1 pergaminho de renomeacao, 1 pergaminho da reforja, 1 pergaminho da substituicao, 1 selo de protecao, 1 tonico de recuperacao, 1 amuleto de retorno, 1 pocao do foco, 1 pocao da sorte, 1 pedra da dungeon, 1 bussola da dungeon.",
                "pack", "rare", 2000, "Pack para iniciantes.", "one_time"),
        };

        private static string BuildValuesClause(ShopProductSeed p) =>
            $"({SqlGuid(p.Id)}, {SqlString(p.Key)}, {SqlString(p.Name)}, {SqlString(p.Description)}, " +
            $"{SqlString(p.Type)}, {SqlString(p.Rarity)}, TRUE, NULL, {p.PriceGold}, NULL, " +
            $"{SqlString(p.Flavor)}, {SqlString(p.UsageLimit)}, " +
            $"{SqlTimestamp(SeedUtcNow)}, NULL, NULL, NULL, NULL, FALSE)";

        private static string SqlGuid(Guid value) => $"'{value:D}'";

        private static string SqlString(string value) => $"'{value.Replace("'", "''")}'";

        private static string SqlTimestamp(DateTime value) => $"TIMESTAMPTZ '{value:O}'";
    }
}
