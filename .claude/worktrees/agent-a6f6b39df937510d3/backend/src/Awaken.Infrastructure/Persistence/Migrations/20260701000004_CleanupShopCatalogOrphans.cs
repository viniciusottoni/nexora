using System;
using System.Linq;
using Awaken.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable enable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// EPIC-021: remove linhas orfas de "shop_products" que nao pertencem ao
    /// catalogo oficial (duplicatas de teste/dev sem Description, Flavor ou
    /// UsageLimit, criadas fora das migrations de seed). Nao ha FK de outras
    /// tabelas para "shop_products".Id (compras referenciam a Key), entao a
    /// remocao e segura.
    /// </summary>
    /// <inheritdoc />
    [DbContext(typeof(AwakenDbContext))]
    [Migration("20260701000004_CleanupShopCatalogOrphans")]
    public partial class CleanupShopCatalogOrphans : Migration
    {
        private static readonly string[] CanonicalKeys =
        {
            // Consumiveis
            "reforja_scroll", "scroll_substitution", "dungeon_compass", "dungeon_key",
            "protection_seal", "recovery_tonic", "return_amulet", "focus_potion",
            "focus_potion_large", "luck_potion", "pedra_dungeon",
            // Cosmeticos
            "frame_rank_e", "frame_rank_d", "frame_rank_c", "frame_rank_b",
            "frame_rank_a", "frame_rank_s", "frame_rank_ss", "frame_rank_sss",
            "aura_default", "background_portal", "background_dungeon",
            "background_hunter_shadows",
            // Perfil
            "scroll_rename", "scroll_class_change",
            // Packs de itens
            "pack_striker", "pack_runner", "pack_guardian", "pack_shadow",
            "pack_reawakened",
            // Pacotes de Gold (IAP)
            "gold_pack_100", "gold_pack_200", "gold_pack_500", "gold_pack_1000",
            "gold_pack_2000", "gold_pack_5000",
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var keys = string.Join(", ", CanonicalKeys.Select(SqlString));
            migrationBuilder.Sql($@"
DELETE FROM ""shop_products""
WHERE ""Key"" NOT IN ({keys});
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Sujeira removida intencionalmente; nao ha estado anterior a restaurar.
        }

        private static string SqlString(string value) => $"'{value.Replace("'", "''")}'";
    }
}
