using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Shop;
using Awaken.Domain.Entities.Subscriptions;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Awaken.Infrastructure.Persistence.Repositories;

/// <summary>
/// US-217: combina RevenueCatEvent (ledger de webhooks de assinatura, US-194) e
/// IapTransactionLedger (ledger de compras avulsas, US-195) numa visão unificada
/// de diagnóstico operacional de monetização.
///
/// Decisão de modelagem (não-óbvia): nem RevenueCatEvent nem IapTransactionLedger têm
/// um campo "Status" de validação/ambiente diretamente comparável entre si:
///   • RevenueCatEvent é um ledger de idempotência de webhook — o "status" da validação
///     é inferido do Type do evento (ActivationEvents → approved, ExpirationEvents →
///     denied, demais tipos → pending), espelhando exatamente a lógica já usada em
///     ProcessRevenueCatWebhookCommandHandler.
///   • IapTransactionLedger já tem Status explícito (pending|granted|failed), mapeado
///     para (pending|approved|failed) — não há "denied" explícito no fluxo IAP atual.
///   • Nenhuma das duas entidades persiste "Environment" por registro (diferente de
///     SecurityAlert). Como o MVP roda um ambiente por processo, o Environment é
///     resolvido uma vez via IHostEnvironment e aplicado a todas as linhas — é honesto
///     dado o modelo de dados atual e evita inventar uma coluna nova fora do escopo da US.
///   • RN-005 (divergência → alerta operacional): "transação repetida" é sinalizada
///     quando há mais de um RevenueCatEvent com o mesmo OriginalTransactionId, e
///     "concessão pendente" quando um IapTransactionLedger está em pending há mais de
///     <c>pendingThresholdMinutes</c>. Optei por sinalizar via flags no próprio DTO de
///     resposta (IsRepeatedTransaction / IsPendingTooLong) em vez de gravar
///     SecurityAlert automaticamente — escolha pragmática para MVP somente-leitura;
///     a geração de alerta automatizado fica para uma iteração futura caso necessário.
/// </summary>
public class AdminSubscriptionDiagnosticsRepository(
    AwakenDbContext context,
    IHostEnvironment hostEnvironment,
    IDateTimeService dateTimeService) : IAdminSubscriptionDiagnosticsRepository
{
    // Espelha ProcessRevenueCatWebhookCommandHandler — eventos que ativam assinatura.
    private static readonly HashSet<string> ActivationEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        "INITIAL_PURCHASE", "RENEWAL", "UNCANCELLATION", "PRODUCT_CHANGE"
    };

    // Espelha ProcessRevenueCatWebhookCommandHandler — eventos que encerram/suspendem assinatura.
    private static readonly HashSet<string> ExpirationEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        "CANCELLATION", "EXPIRATION", "BILLING_ISSUE", "SUBSCRIBER_ALIAS"
    };

    private readonly string _environment = NormalizeEnvironment(hostEnvironment.EnvironmentName);

    public async Task<SubscriptionDiagnosticsCounts> GetCountsAsync(
        DateTime? fromUtc, DateTime? toUtc, int pendingThresholdMinutes, CancellationToken ct = default)
    {
        var rcEvents = await GetFilteredRevenueCatEventsAsync(fromUtc, toUtc, ct);
        var iapLedgers = await GetFilteredIapLedgersAsync(fromUtc, toUtc, ct);

        var pendingCutoff = DateTimeNow() - TimeSpan.FromMinutes(pendingThresholdMinutes);

        var approved = rcEvents.Count(e => ActivationEvents.Contains(e.Type))
            + iapLedgers.Count(l => l.Status == "granted");

        var denied = rcEvents.Count(e => ExpirationEvents.Contains(e.Type));

        var pending = rcEvents.Count(e => !ActivationEvents.Contains(e.Type) && !ExpirationEvents.Contains(e.Type))
            + iapLedgers.Count(l => l.Status == "pending");

        var failed = iapLedgers.Count(l => l.Status == "failed");

        var repeatedTransactions = rcEvents
            .Where(e => e.OriginalTransactionId is not null)
            .GroupBy(e => e.OriginalTransactionId)
            .Count(g => g.Count() > 1);

        var pendingGrants = iapLedgers.Count(l => l.Status == "pending" && l.CreatedAtUtc <= pendingCutoff);

        return new SubscriptionDiagnosticsCounts(
            approved, denied, pending, failed, repeatedTransactions, pendingGrants);
    }

    public async Task<(IReadOnlyList<SubscriptionDiagnosticEventRow> Items, int Total)> GetPagedEventsAsync(
        string? type, string? store, string? status, string? plan, string? product,
        string? environment, Guid? userId, DateTime? fromUtc, DateTime? toUtc,
        int pendingThresholdMinutes, int page, int pageSize, CancellationToken ct = default)
    {
        // Ambiente é global ao processo (ver comentário de classe) — filtro só restringe
        // resultado se o valor pedido não bater com o ambiente atual.
        if (!string.IsNullOrWhiteSpace(environment) &&
            !environment.Equals(_environment, StringComparison.OrdinalIgnoreCase))
        {
            return (Array.Empty<SubscriptionDiagnosticEventRow>(), 0);
        }

        var rows = new List<SubscriptionDiagnosticEventRow>();

        if (string.IsNullOrWhiteSpace(type) || type.Equals("subscription", StringComparison.OrdinalIgnoreCase))
        {
            var rcEvents = await GetFilteredRevenueCatEventsAsync(fromUtc, toUtc, ct, userId, product);
            rows.AddRange(ProjectRevenueCatEvents(rcEvents, pendingThresholdMinutes));
        }

        if (string.IsNullOrWhiteSpace(type) || type.Equals("iap", StringComparison.OrdinalIgnoreCase))
        {
            var iapLedgers = await GetFilteredIapLedgersAsync(fromUtc, toUtc, ct, userId, product);
            rows.AddRange(ProjectIapLedgers(iapLedgers, pendingThresholdMinutes));
        }

        if (!string.IsNullOrWhiteSpace(store))
            rows = rows.Where(r => r.Store.Equals(store, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!string.IsNullOrWhiteSpace(status))
            rows = rows.Where(r => r.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!string.IsNullOrWhiteSpace(plan))
            rows = rows.Where(r => r.Plan is not null && r.Plan.Equals(plan, StringComparison.OrdinalIgnoreCase)).ToList();

        var ordered = rows.OrderByDescending(r => r.CreatedAtUtc).ToList();
        var total = ordered.Count;

        var page1 = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return (page1, total);
    }

    public async Task<SubscriptionDiagnosticEventRow?> GetEventByIdAsync(
        Guid id, string source, int pendingThresholdMinutes, CancellationToken ct = default)
    {
        if (source.Equals("revenuecat_event", StringComparison.OrdinalIgnoreCase))
        {
            var rcEvent = await context.RevenueCatEvents.FirstOrDefaultAsync(e => e.Id == id, ct);
            return rcEvent is null ? null : ProjectRevenueCatEvent(rcEvent, pendingThresholdMinutes, isRepeated: await IsRepeatedAsync(rcEvent, ct));
        }

        if (source.Equals("iap_ledger", StringComparison.OrdinalIgnoreCase))
        {
            var ledger = await context.IapTransactionLedgers.FirstOrDefaultAsync(l => l.Id == id, ct);
            return ledger is null ? null : ProjectIapLedger(ledger, pendingThresholdMinutes);
        }

        return null;
    }

    public async Task<IReadOnlyList<SubscriptionDiagnosticEventRow>> GetRelatedEventsByUserIdAsync(
        Guid userId, Guid excludeEventId, int take, CancellationToken ct = default)
    {
        var rcEvents = await context.RevenueCatEvents
            .Where(e => e.AppUserId == userId.ToString())
            .OrderByDescending(e => e.ProcessedAtUtc)
            .ToListAsync(ct);

        var iapLedgers = await context.IapTransactionLedgers
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAtUtc)
            .ToListAsync(ct);

        var rows = new List<SubscriptionDiagnosticEventRow>();
        rows.AddRange(ProjectRevenueCatEvents(rcEvents, pendingThresholdMinutes: 30));
        rows.AddRange(ProjectIapLedgers(iapLedgers, pendingThresholdMinutes: 30));

        return rows
            .Where(r => r.Id != excludeEventId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(take)
            .ToList();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<List<RevenueCatEvent>> GetFilteredRevenueCatEventsAsync(
        DateTime? fromUtc, DateTime? toUtc, CancellationToken ct, Guid? userId = null, string? product = null)
    {
        var query = context.RevenueCatEvents.AsQueryable();

        if (fromUtc.HasValue) query = query.Where(e => e.ProcessedAtUtc >= fromUtc.Value);
        if (toUtc.HasValue) query = query.Where(e => e.ProcessedAtUtc <= toUtc.Value);
        if (userId.HasValue) query = query.Where(e => e.AppUserId == userId.Value.ToString());
        if (!string.IsNullOrWhiteSpace(product)) query = query.Where(e => e.ProductId == product);

        return await query.ToListAsync(ct);
    }

    private async Task<List<IapTransactionLedger>> GetFilteredIapLedgersAsync(
        DateTime? fromUtc, DateTime? toUtc, CancellationToken ct, Guid? userId = null, string? product = null)
    {
        var query = context.IapTransactionLedgers.AsQueryable();

        if (fromUtc.HasValue) query = query.Where(l => l.CreatedAtUtc >= fromUtc.Value);
        if (toUtc.HasValue) query = query.Where(l => l.CreatedAtUtc <= toUtc.Value);
        if (userId.HasValue) query = query.Where(l => l.UserId == userId.Value);
        if (!string.IsNullOrWhiteSpace(product)) query = query.Where(l => l.ProductKey == product);

        return await query.ToListAsync(ct);
    }

    private async Task<bool> IsRepeatedAsync(RevenueCatEvent rcEvent, CancellationToken ct)
    {
        if (rcEvent.OriginalTransactionId is null) return false;

        var count = await context.RevenueCatEvents
            .CountAsync(e => e.OriginalTransactionId == rcEvent.OriginalTransactionId, ct);

        return count > 1;
    }

    private IEnumerable<SubscriptionDiagnosticEventRow> ProjectRevenueCatEvents(
        IReadOnlyCollection<RevenueCatEvent> events, int pendingThresholdMinutes)
    {
        var repeatedTransactionIds = events
            .Where(e => e.OriginalTransactionId is not null)
            .GroupBy(e => e.OriginalTransactionId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key!)
            .ToHashSet();

        return events.Select(e => ProjectRevenueCatEvent(
            e, pendingThresholdMinutes, isRepeated: e.OriginalTransactionId is not null && repeatedTransactionIds.Contains(e.OriginalTransactionId)));
    }

    private SubscriptionDiagnosticEventRow ProjectRevenueCatEvent(
        RevenueCatEvent e, int pendingThresholdMinutes, bool isRepeated)
    {
        var status = ActivationEvents.Contains(e.Type) ? "approved"
            : ExpirationEvents.Contains(e.Type) ? "denied"
            : "pending";

        var pendingCutoff = DateTimeNow() - TimeSpan.FromMinutes(pendingThresholdMinutes);
        var isPendingTooLong = status == "pending" && e.ProcessedAtUtc <= pendingCutoff;

        Guid? userId = Guid.TryParse(e.AppUserId, out var parsed) ? parsed : null;

        return new SubscriptionDiagnosticEventRow(
            e.Id,
            Source: "revenuecat_event",
            Type: "subscription",
            Store: "revenuecat",
            Status: status,
            Plan: DerivePlanFromProductId(e.ProductId),
            Product: e.ProductId,
            Environment: _environment,
            UserId: userId,
            MaskedExternalRef: MaskReference(e.OriginalTransactionId),
            PayloadHashMasked: e.PayloadHash,
            IsRepeatedTransaction: isRepeated,
            IsPendingTooLong: isPendingTooLong,
            CreatedAtUtc: e.ProcessedAtUtc);
    }

    private IEnumerable<SubscriptionDiagnosticEventRow> ProjectIapLedgers(
        IReadOnlyCollection<IapTransactionLedger> ledgers, int pendingThresholdMinutes) =>
        ledgers.Select(l => ProjectIapLedger(l, pendingThresholdMinutes));

    private SubscriptionDiagnosticEventRow ProjectIapLedger(IapTransactionLedger l, int pendingThresholdMinutes)
    {
        var status = l.Status switch
        {
            "granted" => "approved",
            "failed" => "failed",
            _ => "pending",
        };

        var pendingCutoff = DateTimeNow() - TimeSpan.FromMinutes(pendingThresholdMinutes);
        var isPendingTooLong = status == "pending" && l.CreatedAtUtc <= pendingCutoff;

        return new SubscriptionDiagnosticEventRow(
            l.Id,
            Source: "iap_ledger",
            Type: "iap",
            Store: l.Store,
            Status: status,
            Plan: null,
            Product: l.ProductKey,
            Environment: _environment,
            UserId: l.UserId,
            MaskedExternalRef: MaskReference(l.TransactionId),
            PayloadHashMasked: null,
            IsRepeatedTransaction: false,
            IsPendingTooLong: isPendingTooLong,
            CreatedAtUtc: l.CreatedAtUtc);
    }

    /// <summary>RN-004: dados sensíveis de provider mascarados — mantém só os 4 últimos caracteres.</summary>
    private static string? MaskReference(string? reference)
    {
        if (string.IsNullOrEmpty(reference)) return reference;
        return reference.Length <= 4
            ? new string('*', reference.Length)
            : new string('*', reference.Length - 4) + reference[^4..];
    }

    private static string DerivePlanFromProductId(string? productId)
    {
        if (string.IsNullOrEmpty(productId)) return "unknown";
        if (productId.Contains("annual", StringComparison.OrdinalIgnoreCase)) return "annual";
        if (productId.Contains("monthly", StringComparison.OrdinalIgnoreCase)) return "monthly";
        return "unknown";
    }

    private static string NormalizeEnvironment(string aspnetEnvironmentName) => aspnetEnvironmentName switch
    {
        "Production" => "prod",
        "Staging" => "staging",
        _ => "dev",
    };

    private DateTime DateTimeNow() => dateTimeService.UtcNow;
}
