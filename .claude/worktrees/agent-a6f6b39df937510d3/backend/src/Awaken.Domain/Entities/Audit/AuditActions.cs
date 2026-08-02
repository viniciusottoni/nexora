namespace Awaken.Domain.Entities.Audit;

/// <summary>
/// Constantes estáveis de ações de auditoria (US-190).
/// Evitam strings soltas nos handlers e garantem consistência.
/// </summary>
public static class AuditActions
{
    // ── Economia / Loja ──────────────────────────────────────────────────────
    public const string ShopOrderInitiated  = "ShopOrder.Initiated";
    public const string ShopOrderGranted    = "ShopOrder.Granted";
    public const string ShopOrderFailed     = "ShopOrder.Failed";

    /// US-230: item consumível/cosmético usado pelo usuário.
    public const string InventoryItemUsed   = "InventoryItem.Used";

    // ── Carteira Gold ────────────────────────────────────────────────────────
    public const string GoldWalletCredited = "GoldWallet.Credited";
    public const string GoldWalletDebited  = "GoldWallet.Debited";

    // ── Admin — Visibilidade administrativa ──────────────────────────────────
    /// US-193: consulta administrativa de pedidos (RN-004 / CA-003).
    public const string AdminShopOrdersQueried = "AdminShopOrders.Queried";

    // ── Segurança — Alertas (detectores automáticos) ─────────────────────────
    /// US-228: criação automática de SecurityAlert por um detector/job de reconciliação.
    public const string SecurityAlertCreated = "SecurityAlert.Created";

    // ── EPIC-017 — Admin Auth ─────────────────────────────────────────────────
    public const string AdminAuthLogin       = "AdminAuth.Login";
    public const string AdminAuthLoginFailed = "AdminAuth.LoginFailed";
    public const string AdminAuthLocked      = "AdminAuth.Locked";
    public const string AdminAuthMfaSetup    = "AdminAuth.MfaSetup";
    public const string AdminAuthMfaFailed   = "AdminAuth.MfaFailed";
    public const string AdminAuthMfaReset    = "AdminAuth.MfaReset";

    // ── EPIC-017 — Admin Tickets ──────────────────────────────────────────────
    public const string AdminTicketStatusChanged = "AdminTicket.StatusChanged";
    public const string AdminTicketTriaged       = "AdminTicket.Triaged";
    public const string AdminTicketNoteAdded     = "AdminTicket.NoteAdded";

    // ── EPIC-017 — Admin Bugs ─────────────────────────────────────────────────
    public const string AdminBugCreated = "AdminBug.Created";
    public const string AdminBugUpdated = "AdminBug.Updated";
    public const string AdminBugClosed  = "AdminBug.Closed";

    // ── EPIC-017 — Admin Security ─────────────────────────────────────────────
    public const string AdminSecurityAlertAnalyzed   = "AdminSecurityAlert.Analyzed";
    /// US-219 RN-004/RN-005: classificação de triagem (ex.: falso positivo) — não apaga o alerta.
    public const string AdminSecurityAlertClassified = "AdminSecurityAlert.Classified";
    /// US-219 RN-004: nota de triagem adicionada/atualizada em um alerta.
    public const string AdminSecurityAlertNoteAdded  = "AdminSecurityAlert.NoteAdded";
    /// US-219 RN-004: alerta vinculado a um bug/incidente operacional.
    public const string AdminSecurityAlertLinkedToBug = "AdminSecurityAlert.LinkedToBug";

    // ── EPIC-017 — Admin Exportações ──────────────────────────────────────────
    public const string AdminUsersExported   = "AdminUsers.Exported";
    public const string AdminReportsExported = "AdminReports.Exported";

    // ── Outros (já existentes — referência, não migrados aqui) ──────────────
    // "account_deleted", "legal_terms_accepted", "trial_started"
}
