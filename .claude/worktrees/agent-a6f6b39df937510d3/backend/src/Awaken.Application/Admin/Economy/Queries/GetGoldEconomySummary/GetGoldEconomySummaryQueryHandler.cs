using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Admin.Economy;
using Awaken.Domain.Entities.Economy;
using Awaken.Domain.Entities.Security;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Admin.Economy.Queries.GetGoldEconomySummary;

/// <summary>
/// US-229: agrega indicadores da economia Gold para o painel admin.
/// Carrega dados em memória (padrão MVP — ver ReconcileGoldEconomyCommandHandler).
/// </summary>
public class GetGoldEconomySummaryQueryHandler(
    IGoldWalletRepository goldWalletRepository,
    IGoldLedgerEntryRepository goldLedgerEntryRepository,
    IShopOrderRepository shopOrderRepository,
    ISecurityAlertRepository securityAlertRepository,
    IDateTimeService dateTimeService)
    : IRequestHandler<GetGoldEconomySummaryQuery, GoldEconomySummaryResponse>
{
    private const int MaxRows = 50_000;
    private static readonly HashSet<string> GoldAlertTypes =
        new(GoldEconomyAlertTypes.All, StringComparer.Ordinal);

    public async Task<GoldEconomySummaryResponse> Handle(
        GetGoldEconomySummaryQuery request, CancellationToken ct)
    {
        var utcNow = dateTimeService.UtcNow;
        var fromUtc = request.FromUtc ?? utcNow.AddDays(-30);
        var toUtc   = request.ToUtc   ?? utcNow;

        // ── Ledger no período ─────────────────────────────────────────────────
        var (ledgerEntries, _) = await goldLedgerEntryRepository.GetAdminPagedAsync(
            null, null, fromUtc, toUtc, 1, MaxRows, ct);

        long totalPurchased = ledgerEntries
            .Where(e => e.Direction == GoldLedgerDirection.Credit)
            .Sum(e => e.Amount);

        long totalSpent = ledgerEntries
            .Where(e => e.Direction == GoldLedgerDirection.Debit)
            .Sum(e => e.Amount);

        // ── Saldo total em circulação (todas as carteiras) ────────────────────
        var wallets = await goldWalletRepository.GetAllAsync(ct);
        long totalInCirculation = wallets.Sum(w => w.Balance);

        // ── Pedidos gold no período ───────────────────────────────────────────
        var (orders, _) = await shopOrderRepository.GetPagedByFilterAsync(
            null, null, "gold", fromUtc, toUtc, 1, MaxRows, ct);

        int ordersGranted = orders.Count(o => o.Status == "granted");
        int ordersPending = orders.Count(o => o.Status == "pending");
        int ordersFailed  = orders.Count(o => o.Status == "failed");

        // ── Top produtos comprados com Gold ───────────────────────────────────
        var topProducts = orders
            .Where(o => o.Status == "granted")
            .GroupBy(o => o.ProductKey)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => new GoldTopProductItem(g.Key, g.Count()))
            .ToList();

        // ── Alertas gold abertos ──────────────────────────────────────────────
        var (openAlerts, _) = await securityAlertRepository.GetPagedAsync(
            null, null, "open", null, 1, MaxRows, ct);

        int openGoldAlerts = openAlerts.Count(a => GoldAlertTypes.Contains(a.AlertType));

        // ── Usuários com volume anormal ───────────────────────────────────────
        var (abnormalAlerts, _) = await securityAlertRepository.GetPagedAsync(
            GoldEconomyAlertTypes.AbnormalVolume, null, "open", null, 1, 200, ct);

        var abnormalUsers = abnormalAlerts
            .Where(a => a.AffectedUserId != null)
            .GroupBy(a => a.AffectedUserId!.Value)
            .Select(g => new GoldAbnormalUserItem(g.Key, g.Count()))
            .OrderByDescending(u => u.AlertCount)
            .Take(20)
            .ToList();

        // ── Última reconciliação (alerta gold mais recente entre os carregados) ─
        DateTime? lastReconciliation = openAlerts
            .Where(a => GoldAlertTypes.Contains(a.AlertType))
            .OrderByDescending(a => a.CreatedAtUtc)
            .Select(a => (DateTime?)a.CreatedAtUtc)
            .FirstOrDefault();

        return new GoldEconomySummaryResponse(
            totalPurchased,
            totalSpent,
            totalInCirculation,
            ordersGranted,
            ordersPending,
            ordersFailed,
            openGoldAlerts,
            topProducts,
            abnormalUsers,
            lastReconciliation,
            fromUtc,
            toUtc);
    }
}
