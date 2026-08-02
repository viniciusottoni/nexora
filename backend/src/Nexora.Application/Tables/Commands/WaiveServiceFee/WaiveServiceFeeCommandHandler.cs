using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Tables.Billing;
using Nexora.Application.Tables.Sessions;
using Nexora.Contracts.Operation;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tables.Commands.WaiveServiceFee;

/// <summary>
/// US-027 §4, cenário "Retirada da taxa por uma das partes" — ver docstring de
/// <see cref="WaiveServiceFeeCommand"/>. Não é uma transição de estado do agregado
/// <see cref="TableSession"/> (a divisão não é persistida, US-027 §6), por isso não emite
/// <see cref="DomainEvent"/> (ADR-006 rege transição de ESTADO; aqui não há nenhuma) — só o
/// <see cref="AuditLog"/> exigido pela RN-010 ("a retirada é registrada e auditada").
/// </summary>
internal sealed class WaiveServiceFeeCommandHandler : IRequestHandler<WaiveServiceFeeCommand, Result<BillResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public WaiveServiceFeeCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<BillResponse>> Handle(WaiveServiceFeeCommand request, CancellationToken cancellationToken)
    {
        var session = await _db.TableSessions
            .SingleOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);

        if (session is null)
        {
            return Result<BillResponse>.Failure("Sessão não encontrada.", ApiErrorCodes.TableSessionNotFound);
        }

        if (session.Status is TableSessionStatus.Paid or TableSessionStatus.Closed)
        {
            return Result<BillResponse>.Failure(
                "Esta comanda já foi encerrada e não permite mais dividir a conta.", ApiErrorCodes.TableSessionNotOpen);
        }

        var waived = (request.AlreadyWaivedPersons ?? Array.Empty<int>()).ToHashSet();
        waived.Add(request.Person);

        _db.AuditLogs.Add(AuditLog.Create(
            session.TenantId,
            action: "TABLE_SESSION_SERVICE_FEE_WAIVED",
            entity: "table_session",
            occurredAt: DateTimeOffset.UtcNow,
            storeId: session.StoreId,
            actorId: _tenantContext.UserId,
            entityId: session.Id,
            after: JsonSerializer.Serialize(new { person = request.Person, people = request.People }),
            reason: request.Reason));

        var items = await BillQueryCoordinator.LoadItemsAsync(_db, session.Id, cancellationToken);
        var feePercent = await BillQueryCoordinator.ResolveFeePercentAsync(_db, session.TenantId, cancellationToken);
        var subtotal = items.Where(i => i.Status != OrderItemStatus.Cancelled).Sum(i => i.TotalPrice);

        var result = BillSplitCalculator.CalculateByPerson(subtotal, feePercent, request.People, waived);
        var itemResponses = items.Select(i => new BillItemResponse(
            i.Id, BillQueryCoordinator.ItemName(i), i.TotalPrice, BillQueryCoordinator.IsStillCooking(i), AssignedPerson: null)).ToList();
        var pendingItems = BillQueryCoordinator.BuildPendingItems(items);

        var response = new BillResponse(
            itemResponses,
            result.Subtotal,
            result.ServiceFeeNominal,
            result.Total,
            "BY_PERSON",
            result.Parts.Select(p => new BillSplitPartResponse(p.Person, p.Amount, p.ServiceFeeAmount, p.ServiceFeeWaived)).ToList(),
            pendingItems,
            pendingItems.Count > 0,
            AmountPaid: null,
            RemainingAmount: null,
            UnassignedItemIds: Array.Empty<Guid>());

        return Result<BillResponse>.Success(response);
    }
}
