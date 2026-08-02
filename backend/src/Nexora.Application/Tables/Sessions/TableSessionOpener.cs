using System.Text.Json;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Catalog.Availability;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tables.Sessions;

/// <summary>
/// Núcleo único de abertura de <see cref="TableSession"/> — o comando de abertura que a US-021
/// pede explicitamente para ser reaproveitado ("Abertura automática de sessão de mesa se ainda não
/// houver uma (com a US-022)"). Chamado por
/// <see cref="Commands.OpenTableSession.OpenTableSessionCommandHandler"/> (US-022, garçom, sempre
/// chamado depois de confirmar que a mesa existe e que não há sessão ativa) e por
/// <see cref="Commands.AccessTableByQrToken.AccessTableByQrTokenCommandHandler"/> (US-021, cliente
/// via QR, chamado só quando a checagem de sessão ativa feita ali já deu negativo). Mesma checagem
/// de sessão única, mesmo cálculo de dia operacional (ADR-018) e mesmo par AuditLog+DomainEvent
/// (EVT-020 <c>table.session.opened</c>) para os dois fluxos — só <c>source</c>/quem abriu muda.
/// </summary>
/// <remarks>
/// [DECISÃO] Extraído como método estático em vez de um segundo <c>ICommand</c> despachado via
/// <c>ISender.Send</c> aninhado: os dois chamadores já precisam consultar a mesa antes de decidir
/// se abrem ou não uma sessão (o fluxo do QR só abre quando NÃO existe sessão ativa; o fluxo do
/// garçom sempre recusa com <see cref="ApiErrorCodes.TableAlreadyOpen"/> quando existe) — nenhum
/// dos dois quer que este método repita essa checagem sozinho como pré-condição de um "comando"
/// completo. Ainda assim, para defesa em profundidade contra uma corrida rara (duas leituras
/// simultâneas do mesmo QR, ou o garçom e o cliente abrindo ao mesmo tempo), este método REPETE a
/// checagem de sessão ativa antes de inserir — se ainda assim colidir na gravação (janela entre a
/// checagem e o <c>SaveChangesAsync</c> do <c>TransactionBehavior</c>), a constraint
/// <c>uq_session_open</c> do banco é quem faz a garantia final; esse caso extremo propagaria como
/// erro 500 em vez de 409 nesta wave (limitação conhecida, documentada no relatório da tarefa).
/// </remarks>
internal static class TableSessionOpener
{
    public static async Task<Result<TableSession>> OpenAsync(
        IApplicationDbContext db,
        IEventOriginProvider eventOrigin,
        DiningTable table,
        short? requestedGuestCount,
        Guid? waiterId,
        Guid? openedBy,
        string source,
        DateTimeOffset? occurredAt,
        CancellationToken cancellationToken)
    {
        var existing = await db.TableSessions.SingleOrDefaultAsync(
            s => s.TableId == table.Id && s.Status != TableSessionStatus.Closed, cancellationToken);
        if (existing is not null)
        {
            return Result<TableSession>.Failure(
                "Esta mesa já tem uma comanda em aberto.",
                ApiErrorCodes.TableAlreadyOpen,
                new Dictionary<string, string[]> { ["sessionId"] = new[] { existing.Id.ToString() } });
        }

        var now = DateTimeOffset.UtcNow;
        var effectiveOccurredAt = occurredAt ?? now;

        var tenantConfig = await db.TenantConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.TenantId == table.TenantId, cancellationToken);
        var startHourUtc = BusinessDayPolicy.ResolveStartHourUtc(tenantConfig?.Operation);
        var businessDay = DateOnly.FromDateTime(
            BusinessDayPolicy.CurrentBusinessDayStart(effectiveOccurredAt, startHourUtc).UtcDateTime);

        // US-021 RN "contagem de pessoas pendente de confirmação": só a origem QR nasce não
        // confirmada — o garçom sempre informa (e portanto confirma) a contagem na abertura.
        var guestCountConfirmed = source != "QR";
        var guestCount = requestedGuestCount ?? 1;

        var session = TableSession.Create(
            table.TenantId,
            table.StoreId,
            table.Id,
            businessDay,
            guestCount: guestCount,
            waiterId: waiterId,
            openedBy: openedBy,
            openedSource: source,
            occurredAt: effectiveOccurredAt,
            guestCountConfirmed: guestCountConfirmed);

        db.TableSessions.Add(session);
        table.Occupy();

        db.AuditLogs.Add(AuditLog.Create(
            table.TenantId,
            action: "TABLE_SESSION_OPENED",
            entity: "table_session",
            occurredAt: effectiveOccurredAt,
            storeId: table.StoreId,
            actorId: openedBy,
            entityId: session.Id,
            after: JsonSerializer.Serialize(new { tableId = table.Id, guestCount, source, waiterId })));

        // EVT-020 table.session.opened (US-022 §6) — mesmo tipo de evento para os dois fluxos,
        // só o campo source muda.
        db.DomainEvents.Add(DomainEvent.Create(
            table.TenantId,
            type: "table.session.opened",
            aggregateType: "table_session",
            aggregateId: session.Id,
            payload: JsonSerializer.Serialize(new { tableId = table.Id, guestCount, waiterId, source }),
            origin: eventOrigin.Origin,
            occurredAt: effectiveOccurredAt,
            storeId: table.StoreId,
            actorId: openedBy));

        return Result<TableSession>.Success(session);
    }
}
