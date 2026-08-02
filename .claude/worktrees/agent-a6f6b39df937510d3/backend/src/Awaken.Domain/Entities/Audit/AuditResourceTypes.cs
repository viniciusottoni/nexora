namespace Awaken.Domain.Entities.Audit;

/// <summary>
/// Constantes estáveis de ResourceType para auditoria (US-190).
/// </summary>
public static class AuditResourceTypes
{
    public const string ShopOrder    = "ShopOrder";
    public const string GoldWallet   = "GoldWallet";
    public const string InventoryItem = "InventoryItem";

    // ── EPIC-017 — Site Admin ─────────────────────────────────────────────────
    public const string AdminUser      = "AdminUser";
    public const string SupportTicket  = "SupportTicket";
    public const string OperationalBug = "OperationalBug";
    public const string SecurityAlert  = "SecurityAlert";
}
