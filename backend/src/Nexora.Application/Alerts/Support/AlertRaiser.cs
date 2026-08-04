using System.Text.Json;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Domain.Metrics;
using Nexora.Domain.Platform;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Alerts.Support;

/// <summary>
/// Pedido de criação de um alerta pelo motor (US-080) — dedupe, direcionamento (US-082) e
/// agrupamento (US-083) são todos resolvidos por <see cref="IAlertRaiser"/>, nunca pelo chamador.
/// </summary>
public sealed record RaiseAlertRequest(
    Guid TenantId,
    Guid? StoreId,
    string Type,
    AlertSeverity Severity,
    string Message,
    string? EntityType = null,
    Guid? EntityId = null,
    /// <summary>Usuário responsável pela entidade (ex.: garçom da mesa) — usado pelos escopos RESPONSIBLE/TABLE_OWNER (US-082).</summary>
    Guid? ResponsibleUserId = null,
    /// <summary>Autor da ação que originou o alerta — nunca é o próprio alvo quando o escopo resolve para ele (US-082, "autor não é alertado da própria ação").</summary>
    Guid? ExcludeUserId = null,
    object? Payload = null);

/// <summary>Núcleo único do motor de alertas (US-080/US-082/US-083) — todo alerta do catálogo do MVP passa por aqui, nunca por <c>Alert.Create</c> direto (essa chamada direta continua só em <c>WaiterCallCoordinator</c>/<c>BillRequestCoordinator</c>, US-025/US-026, fora do escopo do motor).</summary>
public interface IAlertRaiser
{
    /// <summary>Cria (ou, se já houver um alerta aberto para a mesma entidade+tipo, apenas escala a severidade de) um alerta.</summary>
    Task<Alert> RaiseAsync(RaiseAlertRequest request, CancellationToken cancellationToken);

    /// <summary>US-080 §4 "Resolução automática" — encerra todo alerta aberto da entidade+tipo quando a condição deixa de valer.</summary>
    Task<int> ResolveAsync(Guid tenantId, string type, string entityType, Guid entityId, CancellationToken cancellationToken);
}

/// <summary>Público (não <c>internal</c> como os demais handlers do módulo): precisa ser instanciável via <c>AddScoped&lt;IAlertRaiser, AlertRaiser&gt;()</c> a partir de Api.Edge/Api.Cloud, que ficam em assemblies separados.</summary>
public sealed class AlertRaiser : IAlertRaiser
{
    private readonly IApplicationDbContext _db;
    private readonly IEventOriginProvider _eventOrigin;
    private readonly IAlertsBroadcaster _broadcaster;

    public AlertRaiser(IApplicationDbContext db, IEventOriginProvider eventOrigin, IAlertsBroadcaster broadcaster)
    {
        _db = db;
        _eventOrigin = eventOrigin;
        _broadcaster = broadcaster;
    }

    public async Task<Alert> RaiseAsync(RaiseAlertRequest request, CancellationToken cancellationToken)
    {
        // Deduplicação (US-080 §4, "uma condição ativa gera um alerta, não N"): uma entidade só
        // tem UM alerta aberto por tipo. Alerta sem entidade associada (nenhum EntityType/EntityId,
        // ex.: AVG_TIME_ABOVE_TARGET por loja usa EntityType="store"/EntityId=storeId, então cai
        // no mesmo caminho) nunca deduplica.
        Alert? existing = null;
        if (request.EntityType is not null && request.EntityId is not null)
        {
            existing = await _db.Alerts.FirstOrDefaultAsync(
                a => a.TenantId == request.TenantId
                     && a.EntityType == request.EntityType
                     && a.EntityId == request.EntityId
                     && a.Type == request.Type
                     && a.ResolvedAt == null,
                cancellationToken);
        }

        if (existing is not null)
        {
            existing.IncreaseSeverity(request.Severity);
            return existing;
        }

        var tenantConfig = await _db.TenantConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.TenantId == request.TenantId, cancellationToken);
        var routing = AlertRoutingConfig.Parse(tenantConfig?.Operation);
        var rule = routing.Resolve(request.Type);

        var (targetRoles, targetUserId) = ResolveTargets(rule, request.ResponsibleUserId, request.ExcludeUserId);
        var (groupKey, groupWindowStart) = ResolveGroup(request, rule.GroupWindowSeconds);

        var groupCountBefore = groupKey is null ? 0 : await CountOpenGroupMembersAsync(request.TenantId, groupKey, cancellationToken);

        var alert = Alert.Create(
            request.TenantId,
            request.Type,
            request.Message,
            request.Severity,
            request.StoreId,
            targetRoles,
            request.Payload is null ? null : JsonSerializer.Serialize(request.Payload),
            request.EntityType,
            request.EntityId,
            targetUserId,
            groupKey,
            groupWindowStart);

        _db.Alerts.Add(alert);

        _db.DomainEvents.Add(DomainEvent.Create(
            request.TenantId,
            type: "alert.raised",
            aggregateType: "alert",
            aggregateId: alert.Id,
            payload: JsonSerializer.Serialize(new
            {
                alertType = alert.Type,
                severity = alert.Severity.ToString(),
                entityType = alert.EntityType,
                entityId = alert.EntityId,
                message = alert.Message
            }),
            origin: _eventOrigin.Origin,
            occurredAt: alert.RaisedAt,
            storeId: request.StoreId));

        if (groupCountBefore > 0)
        {
            // US-083 "atualização do grupo": junta-se a um grupo já aberto — sem som novo.
            await _broadcaster.AlertGroupUpdated(alert, groupCountBefore + 1, cancellationToken);
        }
        else
        {
            await _broadcaster.AlertRaised(alert, cancellationToken);
        }

        return alert;
    }

    public async Task<int> ResolveAsync(Guid tenantId, string type, string entityType, Guid entityId, CancellationToken cancellationToken)
    {
        var open = await _db.Alerts.Where(
            a => a.TenantId == tenantId && a.Type == type && a.EntityType == entityType
                 && a.EntityId == entityId && a.ResolvedAt == null).ToListAsync(cancellationToken);

        foreach (var alert in open)
        {
            alert.Resolve();
            await _broadcaster.AlertResolved(alert, cancellationToken);
        }

        return open.Count;
    }

    /// <summary>
    /// Conta membros abertos do grupo já contando os que ainda não foram salvos nesta mesma
    /// transação (US-083 "rajada": uma varredura do motor pode chamar <see cref="RaiseAsync"/>
    /// várias vezes seguidas ANTES do único <c>SaveChangesAsync</c> do <c>TransactionBehavior</c> —
    /// uma consulta só ao banco não veria os alertas irmãos ainda pendentes no change tracker, e
    /// cada um da rajada seria tratado como "grupo novo", disparando som repetido em vez de só o
    /// primeiro). <see cref="IApplicationDbContext"/> não expõe <c>ChangeTracker</c> — mesmo cast
    /// para <c>DbContext</c> já usado por <c>TransactionBehavior</c>.
    /// </summary>
    private async Task<int> CountOpenGroupMembersAsync(Guid tenantId, string groupKey, CancellationToken cancellationToken)
    {
        var persisted = await _db.Alerts.CountAsync(
            a => a.TenantId == tenantId && a.GroupKey == groupKey && a.ResolvedAt == null, cancellationToken);

        var pending = _db is DbContext dbContext
            ? dbContext.ChangeTracker.Entries<Alert>().Count(
                e => e.State == EntityState.Added && e.Entity.TenantId == tenantId && e.Entity.GroupKey == groupKey)
            : 0;

        return persisted + pending;
    }

    /// <summary>
    /// US-082 §3.1/§7. RESPONSIBLE/TABLE_OWNER: dirige a quem responde pela entidade quando esse
    /// alguém existe e não é o próprio autor; senão cai para os papéis do tipo (mesmo fallback já
    /// usado por <c>WaiterCallCoordinator</c> quando a mesa ainda não tem garçom responsável).
    /// [LIMITAÇÃO CONHECIDA] STATION é tratado como TENANT (broadcast por papel, não por praça
    /// específica) — não existe hoje uma relação usuário↔estação persistida para resolver "só quem
    /// está no KDS do forno"; o roteamento por estação em tempo real já existe por outro canal
    /// (<c>IStationBroadcaster</c>/<c>KdsHub</c>, que agrupa por dispositivo/estação, não por
    /// usuário) — ver Nexora.Api.Edge.Hubs.KdsHub.
    /// </summary>
    private static (IReadOnlyList<string> Roles, Guid? UserId) ResolveTargets(
        AlertRoutingRule rule, Guid? responsibleUserId, Guid? excludeUserId)
    {
        if (rule.Scope is AlertRoutingScopes.Responsible or AlertRoutingScopes.TableOwner
            && responsibleUserId is { } uid && uid != excludeUserId)
        {
            return (Array.Empty<string>(), uid);
        }

        return (rule.Roles, null);
    }

    /// <summary>
    /// US-083 §3: janela de tempo fixa (bucket alinhado ao relógio, não "rolling" a partir do
    /// primeiro alerta) — todo alerta do MESMO tipo/loja criado dentro do mesmo bucket de
    /// <paramref name="windowSeconds"/> segundos cai no mesmo grupo; passado o bucket, o próximo
    /// alerta abre grupo (e notificação) novos, satisfazendo o cenário "Fim da janela" sem precisar
    /// de estado externo (o bucket é determinístico a partir do relógio).
    /// </summary>
    private static (string? GroupKey, DateTimeOffset? WindowStart) ResolveGroup(RaiseAlertRequest request, int? windowSeconds)
    {
        if (windowSeconds is not { } seconds || seconds <= 0)
        {
            return (null, null);
        }

        var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var bucketStartUnix = nowUnix / seconds * seconds;
        var windowStart = DateTimeOffset.FromUnixTimeSeconds(bucketStartUnix);
        var scopeKey = request.StoreId?.ToString("N") ?? "tenant";

        return ($"{request.Type}:{scopeKey}:{bucketStartUnix}", windowStart);
    }
}
