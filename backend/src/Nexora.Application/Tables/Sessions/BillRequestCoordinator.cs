using System.Text.Json;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Contracts.Operation;
using Nexora.Domain.Metrics;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;

namespace Nexora.Application.Tables.Sessions;

/// <summary>
/// Núcleo único de "pedir a conta" (US-026) — chamado pelos dois pontos de entrada com efeito
/// IDÊNTICO exigidos pela história (cenários "Solicitação pelo cliente" via QR e "Solicitação pelo
/// garçom" via mapa de mesas, US-026 §4: "o efeito deve ser idêntico ao da solicitação pelo
/// cliente"). Mesmo espírito de <see cref="TableSessionOpener"/>/<see cref="WaiterCallCoordinator"/>:
/// extraído porque dois comandos (<c>RequestBillCommandHandler</c>, staff; <c>RequestBillByQrCommandHandler</c>,
/// cliente) precisam do MESMO comportamento depois de resolver a sessão de formas diferentes (uma
/// por <c>sessionId</c> direto e autorização de permissão, outra por <c>qrToken</c>+claims do token
/// anônimo).
/// </summary>
internal static class BillRequestCoordinator
{
    public static async Task<Result<BillRequestOutcome>> RequestAsync(
        IApplicationDbContext db,
        IEventOriginProvider eventOrigin,
        IAlertsBroadcaster alertsBroadcaster,
        ITableMapBroadcaster tableMapBroadcaster,
        TableSession session,
        DiningTable table,
        string splitMode,
        short? people,
        CancellationToken cancellationToken)
    {
        if (session.Status is TableSessionStatus.Paid or TableSessionStatus.Closed)
        {
            return Result<BillRequestOutcome>.Failure(
                "Esta comanda já foi encerrada e não pode mais pedir a conta.", ApiErrorCodes.TableSessionNotOpen);
        }

        var now = DateTimeOffset.UtcNow;

        if (session.Status == TableSessionStatus.BillRequested)
        {
            // Cenário análogo a "Chamada repetida" da US-025: a conta já foi solicitada — só
            // atualiza a preferência (ex.: corrigir de 3 para 4 pessoas), sem um segundo alerta ao
            // caixa (ver docstring de TableSession.UpdateSplitPreference).
            session.UpdateSplitPreference(splitMode, people);

            return Result<BillRequestOutcome>.Success(
                new BillRequestOutcome(TableSessionMapper.Map(session, table), AlreadyRequested: true));
        }

        session.RequestBill(splitMode, people);

        db.AuditLogs.Add(AuditLog.Create(
            session.TenantId,
            action: "TABLE_SESSION_BILL_REQUESTED",
            entity: "table_session",
            occurredAt: now,
            storeId: session.StoreId,
            entityId: session.Id,
            after: JsonSerializer.Serialize(new { splitMode, people })));

        // EVT-022 table.bill_requested (US-026 §6): tableId, splitMode, people.
        db.DomainEvents.Add(DomainEvent.Create(
            session.TenantId,
            type: "table.bill_requested",
            aggregateType: "table_session",
            aggregateId: session.Id,
            payload: JsonSerializer.Serialize(new { tableId = table.Id, splitMode, people }),
            origin: eventOrigin.Origin,
            occurredAt: now,
            storeId: session.StoreId));

        // Um único Alert com TargetRoles=["CASHIER"] E TargetUserId=waiterId satisfaz "o caixa E o
        // garçom responsável devem ser alertados" (US-026 §4) — os dois destinos não são mutuamente
        // exclusivos no modelo de Alert (ver Alert.cs), então uma linha basta; o broadcaster manda
        // para os dois grupos SignalR independentemente.
        db.Alerts.Add(Alert.Create(
            session.TenantId,
            type: AlertTypes.BillRequested,
            message: $"Mesa {table.Label} pediu a conta.",
            storeId: session.StoreId,
            targetRoles: new[] { "CASHIER" },
            targetUserId: session.WaiterId,
            payload: JsonSerializer.Serialize(new { tableId = table.Id, sessionId = session.Id, splitMode, people }),
            entityType: "table_session",
            entityId: session.Id));

        await alertsBroadcaster.BillRequested(session.TenantId, table.Id, table.Label, splitMode, people, session.WaiterId, cancellationToken);
        await tableMapBroadcaster.NotifySignalAsync(
            session.TenantId, "table.bill_requested", new { tableId = table.Id, label = table.Label }, cancellationToken);

        return Result<BillRequestOutcome>.Success(
            new BillRequestOutcome(TableSessionMapper.Map(session, table), AlreadyRequested: false));
    }
}

/// <summary>Resultado de <see cref="BillRequestCoordinator.RequestAsync"/> — mapeado para <c>RequestBillResponse</c> pelo handler.</summary>
internal sealed record BillRequestOutcome(TableSessionResponse Session, bool AlreadyRequested);
