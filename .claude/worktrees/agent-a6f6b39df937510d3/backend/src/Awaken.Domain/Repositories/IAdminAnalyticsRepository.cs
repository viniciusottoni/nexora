namespace Awaken.Domain.Repositories;

/// <summary>
/// EPIC-017 — US-161/US-168/US-169: leitura agregada somente-leitura para o
/// dashboard administrativo, eventos de produto e métricas de engajamento/retenção.
/// Não modifica dados; todas as operações são consultas de agregação sobre
/// entidades já existentes (Users, AuditLogs, SupportTickets).
/// </summary>
public interface IAdminAnalyticsRepository
{
    Task<int> CountUsersAsync(CancellationToken ct = default);

    /// Quantidade de usuários distintos (ActorType = User) com pelo menos um AuditLog
    /// criado a partir de <paramref name="sinceUtc"/> (inclusive).
    Task<int> CountDistinctActiveUsersSinceAsync(DateTime sinceUtc, CancellationToken ct = default);

    Task<int> CountOpenSupportTicketsAsync(CancellationToken ct = default);

    /// Série diária de usuários ativos distintos (ActorType = User) no intervalo [from, to].
    Task<IReadOnlyList<(DateOnly Date, int Count)>> GetDailyActiveUsersAsync(
        DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);

    /// Últimos AuditLogs (qualquer ActorType) ordenados por CreatedAtUtc desc.
    Task<IReadOnlyList<(string Action, string ResourceType, DateTime CreatedAtUtc)>> GetRecentActivityAsync(
        int take, CancellationToken ct = default);

    /// Contagem de eventos (Action) de AuditLogs com ActorType = User no intervalo [from, to],
    /// ordenado por contagem desc.
    Task<IReadOnlyList<(string Action, int Count)>> GetTopEventsAsync(
        DateTime fromUtc, DateTime toUtc, int take, CancellationToken ct = default);

    /// Todas as Actions distintas já registradas (ActorType = User), usadas como
    /// taxonomia base para US-168 RN-004 (eventos sem volume recente ainda devem aparecer).
    Task<IReadOnlyList<string>> GetAllDistinctUserActionsAsync(CancellationToken ct = default);

    /// Contagem de eventos (Action) de AuditLogs com ActorType = User no intervalo [from, to],
    /// opcionalmente filtrado por nome de evento.
    Task<IReadOnlyList<(string Action, int Count)>> GetEventVolumesAsync(
        DateTime fromUtc, DateTime toUtc, string? eventNameFilter, CancellationToken ct = default);

    /// Série diária por evento (Action) no intervalo [from, to], para os eventos informados.
    Task<IReadOnlyList<(DateOnly Date, string Action, int Count)>> GetEventTimeSeriesAsync(
        DateTime fromUtc, DateTime toUtc, IReadOnlyCollection<string>? eventNames, CancellationToken ct = default);

    /// Usuários cuja CreatedAtUtc é menor ou igual a <paramref name="thresholdUtc"/> (idade mínima da coorte).
    Task<IReadOnlyList<(Guid UserId, DateTime CreatedAtUtc)>> GetUsersRegisteredBeforeAsync(
        DateTime thresholdUtc, CancellationToken ct = default);

    /// Indica, para o conjunto de usuários informado, se cada um possui pelo menos um AuditLog
    /// (ActorType = User) criado estritamente após (CreatedAtUtc do usuário + offsetDays).
    Task<IReadOnlyList<Guid>> GetUserIdsWithActivityAfterOffsetAsync(
        IReadOnlyCollection<Guid> userIds, int offsetDays, CancellationToken ct = default);

    /// Top features (Action) por contagem de uso (AuditLogs, ActorType = User) no intervalo [from, to].
    Task<IReadOnlyList<(string Action, int Count)>> GetFeatureUsageAsync(
        DateTime fromUtc, DateTime toUtc, int take, CancellationToken ct = default);
}
