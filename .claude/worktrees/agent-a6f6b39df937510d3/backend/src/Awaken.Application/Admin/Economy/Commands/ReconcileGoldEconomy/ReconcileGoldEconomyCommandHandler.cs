using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Economy;
using Awaken.Domain.Entities.Security;
using Awaken.Domain.Entities.Shop;
using Awaken.Domain.Repositories;
using Awaken.Shared.Audit;
using MediatR;

namespace Awaken.Application.Admin.Economy.Commands.ReconcileGoldEconomy;

/// <summary>
/// US-228: reconcilia GoldWallet, GoldLedgerEntry, ShopOrder e InventoryItem, criando um
/// SecurityAlert para cada divergência encontrada (RN-005), reaproveitando integralmente a
/// infraestrutura de alertas operacionais já existente (US-165/US-219) — nenhuma tabela nova,
/// nenhuma lógica de exibição nova.
///
/// Escopo MVP / limitações conscientes:
/// - Carrega todas as wallets/ledger/pedidos/itens em memória via GetAllAsync (sem paginação).
///   Aceitável para o volume do MVP; se a base crescer, deve evoluir para leitura em lotes —
///   não implementado aqui para não alterar as interfaces de repositório de Gold/Shop usadas
///   em paralelo por outras US (US-226/US-227).
/// - "Origem rastreável" de um InventoryItem (RN-004) é aproximada (best-effort): considera-se
///   rastreável se existir pelo menos um ShopOrder "granted" do mesmo usuário com ProductKey
///   igual ao ItemKey do item. Não há hoje um vínculo direto ShopOrder→InventoryItem nem um log
///   de origem por item; essa é uma heurística documentada, não uma garantia formal.
/// - "Compra repetida" (DuplicatePurchase) é estruturalmente quase impossível hoje devido ao
///   índice único em ExternalTransactionId; o tipo de alerta existe na lista mínima (seção 7)
///   mas não há heurística de detecção implementada nesta primeira versão por não haver, no
///   modelo atual, uma forma de gerar dois ShopOrder "gold" granted idênticos sem que isso já
///   seja uma OrderGrantedWithoutDebit ou uma divergência de saldo capturada por outra regra.
/// </summary>
public class ReconcileGoldEconomyCommandHandler(
    IGoldWalletRepository goldWalletRepository,
    IGoldLedgerEntryRepository goldLedgerEntryRepository,
    IShopOrderRepository shopOrderRepository,
    IInventoryRepository inventoryRepository,
    ISecurityAlertRepository securityAlertRepository,
    IAuditLogService auditLogService,
    IUnitOfWork unitOfWork,
    IDateTimeService dateTimeService)
    : IRequestHandler<ReconcileGoldEconomyCommand, ReconciliationSummary>
{
    /// Janela usada tanto para detectar volume anormal de pedidos quanto para a checagem de
    /// duplicidade de alertas (não recriar um alerta já aberto criado dentro desta mesma janela).
    private static readonly TimeSpan DeduplicationWindow = TimeSpan.FromHours(6);

    private static readonly TimeSpan AbnormalVolumeWindow = TimeSpan.FromHours(24);
    private static readonly TimeSpan ExcessiveFailuresWindow = TimeSpan.FromHours(1);
    private static readonly TimeSpan DuplicatePurchaseWindow = TimeSpan.FromSeconds(10);

    /// Limiar de pedidos (qualquer canal) por usuário nas últimas 24h para alerta de volume anormal.
    public const int AbnormalVolumeThreshold = 20;

    /// Limiar de pedidos "failed" por usuário na última 1h para alerta de muitas falhas.
    public const int ExcessiveFailuresThreshold = 5;

    private const string ReconciliationEnvironment = "prod";

    public async Task<ReconciliationSummary> Handle(ReconcileGoldEconomyCommand request, CancellationToken ct)
    {
        var utcNow = dateTimeService.UtcNow;
        var dedupSinceUtc = utcNow - DeduplicationWindow;

        var wallets = (await goldWalletRepository.GetAllAsync(ct)).ToList();
        var ledgerEntries = (await goldLedgerEntryRepository.GetAllAsync(ct)).ToList();
        var orders = (await shopOrderRepository.GetAllAsync(ct)).ToList();
        var inventoryItems = (await inventoryRepository.GetAllAsync(ct)).ToList();

        var ledgerByWallet = ledgerEntries
            .GroupBy(e => e.WalletId)
            .ToDictionary(g => g.Key, g => g.OrderBy(e => e.CreatedAtUtc).ToList());

        var alertsCreated = 0;
        var alertsSkipped = 0;
        var alertsByType = new Dictionary<string, int>();

        async Task TryCreateAlertAsync(string alertType, string severity, Guid? affectedUserId, string? origin)
        {
            var alreadyOpen = await securityAlertRepository.HasOpenRecentAlertAsync(alertType, affectedUserId, dedupSinceUtc, ct);
            if (alreadyOpen)
            {
                alertsSkipped++;
                return;
            }

            var alert = SecurityAlert.Create(alertType, severity, ReconciliationEnvironment, utcNow, origin: origin, maskedIp: null, affectedUserId: affectedUserId);
            await securityAlertRepository.AddAsync(alert, ct);

            await auditLogService.RecordAsync(
                AuditActions.SecurityAlertCreated,
                null,
                AuditActorType.System,
                AuditResourceTypes.SecurityAlert,
                alert.Id,
                AuditMetadata.Safe(new { alertType }),
                ct);

            alertsCreated++;
            alertsByType[alertType] = alertsByType.GetValueOrDefault(alertType) + 1;
        }

        // ── RN-001 / saldo negativo / ledger ausente ────────────────────────────
        foreach (var wallet in wallets)
        {
            if (wallet.Balance < 0)
            {
                await TryCreateAlertAsync(GoldEconomyAlertTypes.NegativeBalance, "critical", wallet.UserId, origin: "gold_reconciliation");
                continue;
            }

            if (!ledgerByWallet.TryGetValue(wallet.Id, out var walletEntries) || walletEntries.Count == 0)
            {
                if (wallet.Balance != 0)
                    await TryCreateAlertAsync(GoldEconomyAlertTypes.LedgerMissing, "high", wallet.UserId, origin: "gold_reconciliation");
                continue;
            }

            var lastEntry = walletEntries[^1];
            if (lastEntry.BalanceAfter != wallet.Balance)
                await TryCreateAlertAsync(GoldEconomyAlertTypes.BalanceMismatch, "high", wallet.UserId, origin: "gold_reconciliation");
        }

        // ── RN-002: pedido gold granted sem débito correspondente ───────────────
        var goldGrantedOrders = orders.Where(o => o.Channel == "gold" && o.Status == "granted").ToList();
        foreach (var order in goldGrantedOrders)
        {
            var hasDebit = ledgerEntries.Any(e =>
                e.Direction == GoldLedgerDirection.Debit &&
                e.ReferenceType == "shop_order" &&
                e.ReferenceId == order.Id.ToString());

            if (!hasDebit)
                await TryCreateAlertAsync(GoldEconomyAlertTypes.OrderGrantedWithoutDebit, "high", order.UserId, origin: "gold_reconciliation");
        }

        // ── RN-003: crédito de Gold referenciando shop_order sem compra validada ─
        var ordersById = orders.ToDictionary(o => o.Id);
        var creditsFromShopOrder = ledgerEntries.Where(e =>
            e.Direction == GoldLedgerDirection.Credit && e.ReferenceType == "shop_order");

        foreach (var credit in creditsFromShopOrder)
        {
            var hasValidOrder =
                credit.ReferenceId is not null &&
                Guid.TryParse(credit.ReferenceId, out var orderId) &&
                ordersById.TryGetValue(orderId, out var order) &&
                order.Status == "granted";

            if (!hasValidOrder)
            {
                var wallet = wallets.FirstOrDefault(w => w.Id == credit.WalletId);
                await TryCreateAlertAsync(GoldEconomyAlertTypes.CreditWithoutValidation, "high", wallet?.UserId, origin: "gold_reconciliation");
            }
        }

        // ── RN-004: item concedido sem origem rastreável (best-effort) ──────────
        var grantedOrdersByUserAndProduct = orders
            .Where(o => o.Status == "granted")
            .GroupBy(o => (o.UserId, o.ProductKey))
            .Select(g => g.Key)
            .ToHashSet();

        foreach (var item in inventoryItems.Where(i => i.Quantity > 0))
        {
            var hasTraceableOrigin = grantedOrdersByUserAndProduct.Contains((item.UserId, item.ItemKey));
            if (!hasTraceableOrigin)
                await TryCreateAlertAsync(GoldEconomyAlertTypes.ItemWithoutOrigin, "medium", item.UserId, origin: "gold_reconciliation");
        }

        // ── Volume anormal de pedidos por usuário ────────────────────────────────
        var volumeWindowStart = utcNow - AbnormalVolumeWindow;
        var ordersByUserRecent = orders
            .Where(o => o.CreatedAtUtc >= volumeWindowStart)
            .GroupBy(o => o.UserId);

        foreach (var group in ordersByUserRecent)
        {
            if (group.Count() > AbnormalVolumeThreshold)
                await TryCreateAlertAsync(GoldEconomyAlertTypes.AbnormalVolume, "medium", group.Key, origin: "gold_reconciliation");
        }

        // ── Muitas falhas de compra em curto período ─────────────────────────────
        var failuresWindowStart = utcNow - ExcessiveFailuresWindow;
        var failedOrdersByUserRecent = orders
            .Where(o => o.Status == "failed" && o.CreatedAtUtc >= failuresWindowStart)
            .GroupBy(o => o.UserId);

        foreach (var group in failedOrdersByUserRecent)
        {
            if (group.Count() > ExcessiveFailuresThreshold)
                await TryCreateAlertAsync(GoldEconomyAlertTypes.ExcessiveFailures, "medium", group.Key, origin: "gold_reconciliation");
        }

        // ── Compra repetida (heurística defensiva) ───────────────────────────────
        var grantedOrders = orders.Where(o => o.Status == "granted").OrderBy(o => o.CreatedAtUtc).ToList();
        foreach (var group in grantedOrders.GroupBy(o => (o.UserId, o.ProductKey)))
        {
            var ordered = group.OrderBy(o => o.CreatedAtUtc).ToList();
            for (var i = 1; i < ordered.Count; i++)
            {
                var gap = ordered[i].CreatedAtUtc - ordered[i - 1].CreatedAtUtc;
                if (gap >= TimeSpan.Zero && gap <= DuplicatePurchaseWindow)
                {
                    await TryCreateAlertAsync(GoldEconomyAlertTypes.DuplicatePurchase, "medium", group.Key.UserId, origin: "gold_reconciliation");
                    break;
                }
            }
        }

        if (alertsCreated > 0)
            await unitOfWork.SaveChangesAsync(ct);

        return new ReconciliationSummary(wallets.Count, alertsCreated, alertsSkipped, alertsByType);
    }
}
