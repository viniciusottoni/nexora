using System.Text.Json;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Domain.Metrics;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tables.Sessions;

/// <summary>
/// Núcleo único de "chamar o garçom" (US-025) — chamado pelos dois pontos de entrada da história
/// (<c>CallWaiterCommandHandler</c>, cliente via QR) e é o único lugar que decide se cria um novo
/// <see cref="Alert"/> ou reaproveita um já pendente (cenário Gherkin "Chamada repetida": "não deve
/// ser criado um novo alerta"). Extraído como método estático (mesmo padrão de
/// <see cref="TableSessionOpener"/>) porque só há um chamador de escrita nesta wave — o efeito de
/// "garçom confirma atendimento" (US-025 §7, <c>AcknowledgeWaiterCallCommand</c>) só RESOLVE o
/// alerta, não cria, por isso vive no próprio handler.
/// </summary>
internal static class WaiterCallCoordinator
{
    /// <summary>
    /// [DECISÃO] Fallback quando a sessão ainda não tem <see cref="TableSession.WaiterId"/> definido
    /// (mesa aberta pelo cliente via QR, aguardando o garçom confirmar quem é o responsável —
    /// US-021/US-022): em vez de recusar a chamada ou silenciosamente não notificar ninguém, o
    /// alerta é dirigido a <c>TargetRoles=["WAITER"]</c> (todos os garçons do ambiente) — mesma
    /// forma que o próprio escalonamento (US-025 §4) usa quando ninguém atende a tempo. É um
    /// alerta "para todo mundo" só nesse caso específico (sessão sem responsável ainda), que é
    /// exatamente o caso em que "alerta para todo mundo" é o comportamento CORRETO (não há um
    /// responsável único para restringir a quem avisar).
    /// </summary>
    public static async Task<WaiterCallOutcome> CallAsync(
        IApplicationDbContext db,
        IEventOriginProvider eventOrigin,
        IAlertsBroadcaster alertsBroadcaster,
        ITableMapBroadcaster tableMapBroadcaster,
        TableSession session,
        DiningTable table,
        CancellationToken cancellationToken)
    {
        var pending = await db.Alerts.FirstOrDefaultAsync(
            a => a.TenantId == session.TenantId
                 && a.EntityType == "table_session"
                 && a.EntityId == session.Id
                 && a.Type == AlertTypes.WaiterCalled
                 && a.ResolvedAt == null,
            cancellationToken);

        if (pending is not null)
        {
            // Cenário "Chamada repetida": nenhum Alert/DomainEvent/broadcast novo — o cliente só
            // recebe a informação de que já foi avisado (AlreadyPending, mapeado pelo handler).
            return new WaiterCallOutcome(AlreadyPending: true);
        }

        var now = DateTimeOffset.UtcNow;
        var targetRoles = session.WaiterId is null ? new[] { "WAITER" } : Array.Empty<string>();

        var alert = Alert.Create(
            session.TenantId,
            type: AlertTypes.WaiterCalled,
            message: $"Mesa {table.Label} chamou o garçom.",
            storeId: session.StoreId,
            targetRoles: targetRoles,
            payload: JsonSerializer.Serialize(new { tableId = table.Id, sessionId = session.Id }),
            entityType: "table_session",
            entityId: session.Id,
            targetUserId: session.WaiterId,
            groupKey: $"table_session:{session.Id}:{AlertTypes.WaiterCalled}");

        db.Alerts.Add(alert);

        // EVT-021 table.waiter_called (US-025 §6): tableId, sessionId.
        db.DomainEvents.Add(DomainEvent.Create(
            session.TenantId,
            type: "table.waiter_called",
            aggregateType: "table_session",
            aggregateId: session.Id,
            payload: JsonSerializer.Serialize(new { tableId = table.Id, sessionId = session.Id }),
            origin: eventOrigin.Origin,
            occurredAt: now,
            storeId: session.StoreId));

        // Broadcast síncrono, aguardado dentro do handler (mesmo padrão de AddOrderItemCommandHandler
        // / IOrderConsumptionBroadcaster) — dois destinos: o garçom (ou role:waiter, som/vibração,
        // IAlertsBroadcaster) e o mapa de mesas inteiro do tenant (indicador visual, ITableMapBroadcaster).
        await alertsBroadcaster.WaiterCalled(session.TenantId, table.Id, table.Label, session.WaiterId, cancellationToken);
        await tableMapBroadcaster.NotifySignalAsync(
            session.TenantId, "table.waiter_called", new { tableId = table.Id, label = table.Label }, cancellationToken);

        return new WaiterCallOutcome(AlreadyPending: false);
    }
}

/// <summary>Resultado de <see cref="WaiterCallCoordinator.CallAsync"/> — mapeado para <c>CallWaiterResponse</c> pelo handler.</summary>
internal sealed record WaiterCallOutcome(bool AlreadyPending);
