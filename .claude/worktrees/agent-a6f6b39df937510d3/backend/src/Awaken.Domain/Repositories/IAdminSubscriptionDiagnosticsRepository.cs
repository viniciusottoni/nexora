namespace Awaken.Domain.Repositories;

/// <summary>
/// US-217: leitura administrativa somente-leitura combinando Subscription,
/// RevenueCatEvent (ledger de webhooks de assinatura) e IapTransactionLedger
/// (ledger de compras avulsas) para diagnóstico operacional de monetização.
///
/// Modelo unificado: cada linha de Subscription/IAP é projetada como um
/// "evento de validação" com Type (subscription|iap), Store, Status
/// (approved|denied|pending|failed), Plan/Product, Environment e Usuário.
///
/// RN-001: o backend é a fonte de verdade — este repositório só lê dados já
/// persistidos pelos fluxos de webhook/validação (US-194/US-195), nunca infere
/// estado a partir de dados não confiáveis do app.
/// </summary>
public interface IAdminSubscriptionDiagnosticsRepository
{
    /// <summary>
    /// RN-002/RN-003: cards agregados — aprovadas, negadas, pendentes (há mais de
    /// <paramref name="pendingThresholdMinutes"/> minutos) e falhas, no intervalo informado.
    /// </summary>
    Task<SubscriptionDiagnosticsCounts> GetCountsAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        int pendingThresholdMinutes,
        CancellationToken ct = default);

    /// <summary>
    /// Lista paginada de eventos de assinatura/IAP, com filtros opcionais.
    /// Ordenação por data do evento desc.
    /// </summary>
    Task<(IReadOnlyList<SubscriptionDiagnosticEventRow> Items, int Total)> GetPagedEventsAsync(
        string? type,
        string? store,
        string? status,
        string? plan,
        string? product,
        string? environment,
        Guid? userId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int pendingThresholdMinutes,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>Detalhe seguro de um único evento (RevenueCatEvent ou IapTransactionLedger) por Id.</summary>
    Task<SubscriptionDiagnosticEventRow?> GetEventByIdAsync(
        Guid id, string source, int pendingThresholdMinutes, CancellationToken ct = default);

    /// <summary>
    /// RN-005: outros eventos do mesmo usuário (para destacar tentativas repetidas /
    /// múltiplos eventos), ordenados por data desc, excluindo o evento informado.
    /// </summary>
    Task<IReadOnlyList<SubscriptionDiagnosticEventRow>> GetRelatedEventsByUserIdAsync(
        Guid userId, Guid excludeEventId, int take, CancellationToken ct = default);
}

/// <summary>Cards agregados de validações de assinatura/IAP (US-217, seção 7 "Indicadores mínimos").</summary>
public record SubscriptionDiagnosticsCounts(
    int ApprovedCount,
    int DeniedCount,
    int PendingCount,
    int FailedCount,
    int RepeatedTransactionsCount,
    int PendingGrantsCount);

/// <summary>
/// Linha unificada de evento de assinatura/IAP, já com os flags de divergência
/// (RN-005) calculados pelo repositório (que tem visão do conjunto de dados).
/// </summary>
public record SubscriptionDiagnosticEventRow(
    Guid Id,
    string Source,          // "revenuecat_event" | "iap_ledger"
    string Type,            // "subscription" | "iap"
    string Store,           // "revenuecat" | "google_play" | "app_store"
    string Status,          // "approved" | "denied" | "pending" | "failed"
    string? Plan,
    string? Product,
    string Environment,
    Guid? UserId,
    string? MaskedExternalRef,
    string? PayloadHashMasked,
    bool IsRepeatedTransaction,
    bool IsPendingTooLong,
    DateTime CreatedAtUtc);
