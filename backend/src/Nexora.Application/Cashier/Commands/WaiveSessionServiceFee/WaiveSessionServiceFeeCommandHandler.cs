using System.Text.Json;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Tables.Sessions;
using Nexora.Contracts.Cashier;
using Nexora.Domain.Metrics;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Cashier.Commands.WaiveSessionServiceFee;

/// <summary>
/// US-053 §4 — ver docstring de <see cref="WaiveSessionServiceFeeCommand"/>. Escopo <c>FULL</c> é a
/// única transição que persiste no agregado <see cref="TableSession"/> (<c>ServiceFeeWaived</c>);
/// <c>PARTIAL</c> só audita/emite evento (RN-010: "a retirada é registrada e auditada") — o cálculo
/// em si continua na prévia efêmera de US-027, que já resolve corretamente "só a parte de quem
/// retirou muda" sem duplicar essa lógica aqui.
/// </summary>
internal sealed class WaiveSessionServiceFeeCommandHandler : IRequestHandler<WaiveSessionServiceFeeCommand, Result<WaiveSessionServiceFeeResponse>>
{
    /// <summary>US-053 §4, cenário "Padrão anômalo de retirada" — limiar e amostra mínima (RN-010, hipótese a calibrar no piloto, US-053 §15).</summary>
    private const decimal AnomalyThreshold = 0.8m;
    private const int AnomalyMinimumSample = 3;

    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly IEventOriginProvider _eventOrigin;

    public WaiveSessionServiceFeeCommandHandler(
        IApplicationDbContext db, ICurrentTenantContext tenantContext, IEventOriginProvider eventOrigin)
    {
        _db = db;
        _tenantContext = tenantContext;
        _eventOrigin = eventOrigin;
    }

    public async Task<Result<WaiveSessionServiceFeeResponse>> Handle(WaiveSessionServiceFeeCommand request, CancellationToken cancellationToken)
    {
        var session = await _db.TableSessions.SingleOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);
        if (session is null)
        {
            return Result<WaiveSessionServiceFeeResponse>.Failure("Sessão não encontrada.", ApiErrorCodes.TableSessionNotFound);
        }

        if (session.Status is TableSessionStatus.Paid or TableSessionStatus.Closed)
        {
            return Result<WaiveSessionServiceFeeResponse>.Failure(
                "Esta comanda já foi paga ou encerrada.", ApiErrorCodes.TableSessionNotOpen);
        }

        var scope = request.Scope.Trim().ToUpperInvariant();
        var actorId = _tenantContext.UserId!.Value;
        var occurredAt = DateTimeOffset.UtcNow;

        if (scope == "PARTIAL" && (session.SplitMode != "BY_PERSON" || session.SplitPeople is null))
        {
            return Result<WaiveSessionServiceFeeResponse>.Failure(
                "A retirada parcial exige uma conta já dividida por pessoa.", ApiErrorCodes.ServiceFeePartialRequiresSplitPeople);
        }

        if (scope == "FULL")
        {
            session.WaiveServiceFee(request.Reason, actorId, "FULL");
        }

        var billResult = await BillQueryCoordinator.BuildAsync(_db, session, "SINGLE", null, null, null, cancellationToken);
        if (billResult.IsFailure)
        {
            return Result<WaiveSessionServiceFeeResponse>.Failure(billResult.Error!, billResult.Code);
        }

        var bill = billResult.Value!;

        _db.DomainEvents.Add(DomainEvent.Create(
            session.TenantId,
            type: "service_fee.waived",
            aggregateType: "table_session",
            aggregateId: session.Id,
            payload: JsonSerializer.Serialize(new { amount = bill.ServiceFee, reason = request.Reason, waivedBy = actorId, scope, person = request.Person }),
            origin: _eventOrigin.Origin,
            occurredAt: occurredAt,
            storeId: session.StoreId,
            actorId: actorId));

        _db.AuditLogs.Add(AuditLog.Create(
            session.TenantId,
            action: "SERVICE_FEE_WAIVED",
            entity: "table_session",
            occurredAt: occurredAt,
            storeId: session.StoreId,
            actorId: actorId,
            entityId: session.Id,
            after: JsonSerializer.Serialize(new { scope, person = request.Person }),
            reason: request.Reason));

        if (scope == "FULL" && session.WaiterId is { } waiterId)
        {
            await RaiseAnomalyAlertIfNeededAsync(session, waiterId, cancellationToken);
        }

        var response = new WaiveSessionServiceFeeResponse(new ServiceFeeWaivedSessionResponse(bill.ServiceFee, bill.Total));
        return Result<WaiveSessionServiceFeeResponse>.Success(response);
    }

    /// <summary>
    /// US-053 §4, cenário "Padrão anômalo de retirada": proporção de contas do MESMO garçom/turno
    /// hoje com a taxa retirada — ver ressalva de interpretação na docstring da classe (denominador
    /// é "mesas atendidas pelo mesmo garçom no dia", não um conceito formal de turno/cash_session,
    /// que não está ligado a <see cref="TableSession"/> no modelo atual).
    /// </summary>
    private async Task RaiseAnomalyAlertIfNeededAsync(TableSession session, Guid waiterId, CancellationToken cancellationToken)
    {
        var totalOtherSessions = await _db.TableSessions.AsNoTracking().CountAsync(
            s => s.WaiterId == waiterId && s.BusinessDay == session.BusinessDay && s.Id != session.Id, cancellationToken);
        var waivedOtherSessions = await _db.TableSessions.AsNoTracking().CountAsync(
            s => s.WaiterId == waiterId && s.BusinessDay == session.BusinessDay && s.ServiceFeeWaived && s.Id != session.Id, cancellationToken);

        var total = totalOtherSessions + 1;
        var waived = waivedOtherSessions + 1; // a sessão corrente acabou de ser marcada como retirada (ainda não salva).

        if (total < AnomalyMinimumSample || (decimal)waived / total < AnomalyThreshold)
        {
            return;
        }

        _db.Alerts.Add(Alert.Create(
            session.TenantId,
            type: AlertTypes.ServiceFeeWaiveAboveThreshold,
            message: $"Taxa de serviço retirada em {waived} de {total} contas do turno — acima do padrão.",
            storeId: session.StoreId,
            targetRoles: new[] { "OWNER", "MANAGER" },
            payload: JsonSerializer.Serialize(new { waiterId, waived, total }),
            entityType: "table_session",
            entityId: session.Id));
    }
}
