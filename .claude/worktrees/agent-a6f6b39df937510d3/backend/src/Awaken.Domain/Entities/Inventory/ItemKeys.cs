namespace Awaken.Domain.Entities.Inventory;

/// Chaves estaveis de itens de inventario/loja (ADR-021: dominio usa codigos
/// estaveis; a apresentacao/traducao fica no app).
public static class ItemKeys
{
    // ── Legado (US-188) ──────────────────────────────────────────────────────
    public const string ReforgeScroll = "reforja_scroll";
    public const string DungeonStone  = "pedra_dungeon";

    // ── Consumiveis (US-229) ─────────────────────────────────────────────────
    public const string SubstitutionScroll = "scroll_substitution";
    public const string DungeonCompass     = "dungeon_compass";
    public const string DungeonKey         = "dungeon_key";
    public const string ProtectionSeal     = "protection_seal";
    public const string RecoveryTonic      = "recovery_tonic";
    public const string ReturnAmulet       = "return_amulet";
    public const string FocusPotion        = "focus_potion";
    public const string FocusPotionLarge   = "focus_potion_large";
    public const string LuckPotion         = "luck_potion";

    // ── Cosmeticos — Molduras (US-229) ───────────────────────────────────────
    public const string FrameRankE   = "frame_rank_e";
    public const string FrameRankD   = "frame_rank_d";
    public const string FrameRankC   = "frame_rank_c";
    public const string FrameRankB   = "frame_rank_b";
    public const string FrameRankA   = "frame_rank_a";
    public const string FrameRankS   = "frame_rank_s";
    public const string FrameRankSs  = "frame_rank_ss";
    public const string FrameRankSss = "frame_rank_sss";

    // ── Cosmeticos — Auras / Fundos (US-229) ─────────────────────────────────
    public const string AuraDefault            = "aura_default";
    public const string BackgroundPortal       = "background_portal";
    public const string BackgroundDungeon      = "background_dungeon";
    public const string BackgroundHunterShadows = "background_hunter_shadows";

    // ── Cosmeticos — Pergaminhos (US-229) ────────────────────────────────────
    public const string ScrollRename      = "scroll_rename";
    public const string ScrollClassChange = "scroll_class_change";

    // ── Packs (US-229) ───────────────────────────────────────────────────────
    public const string PackStriker    = "pack_striker";
    public const string PackRunner     = "pack_runner";
    public const string PackGuardian   = "pack_guardian";
    public const string PackShadow     = "pack_shadow";
    public const string PackReawakened = "pack_reawakened";
}
