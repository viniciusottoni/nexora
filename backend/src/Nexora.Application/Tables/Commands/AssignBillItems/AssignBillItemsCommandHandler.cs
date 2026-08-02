using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Tables.Billing;
using Nexora.Application.Tables.Sessions;
using Nexora.Contracts.Operation;
using Nexora.Domain.Operation;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tables.Commands.AssignBillItems;

/// <summary>US-027 §7, modo <c>BY_ITEM</c> — ver docstring de <see cref="AssignBillItemsCommand"/>.</summary>
internal sealed class AssignBillItemsCommandHandler : IRequestHandler<AssignBillItemsCommand, Result<BillResponse>>
{
    private readonly IApplicationDbContext _db;

    public AssignBillItemsCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<BillResponse>> Handle(AssignBillItemsCommand request, CancellationToken cancellationToken)
    {
        var session = await _db.TableSessions
            .AsNoTracking()
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

        var items = await BillQueryCoordinator.LoadItemsAsync(_db, session.Id, cancellationToken);
        var activeItems = items.Where(i => i.Status != OrderItemStatus.Cancelled).ToList();
        var activeIds = activeItems.Select(i => i.Id).ToHashSet();

        // Cada item ativo só pode aparecer em UMA atribuição — id repetido, cancelado ou de outra
        // sessão nunca deveria acontecer num cliente bem-comportado, mas é recusado explicitamente
        // (nunca silenciosamente ignorado) para não mascarar um bug do POS.
        var itemToPerson = new Dictionary<Guid, int>();
        foreach (var assignment in request.Assignments)
        {
            foreach (var itemId in assignment.ItemIds)
            {
                if (!activeIds.Contains(itemId))
                {
                    return Result<BillResponse>.Failure(
                        "Um dos itens informados não pertence a esta comanda ou já foi cancelado.",
                        ApiErrorCodes.BillItemAssignmentInvalid);
                }

                if (!itemToPerson.TryAdd(itemId, assignment.Person))
                {
                    return Result<BillResponse>.Failure(
                        "Um item não pode ser atribuído a mais de uma pessoa.",
                        ApiErrorCodes.BillItemAssignmentInvalid);
                }
            }
        }

        var unassigned = activeIds.Where(id => !itemToPerson.ContainsKey(id)).ToArray();
        if (unassigned.Length > 0)
        {
            return Result<BillResponse>.Failure(
                "Todos os itens precisam ser atribuídos a uma pessoa antes de fechar a divisão.",
                ApiErrorCodes.BillItemNotAssigned,
                new Dictionary<string, string[]> { ["itemIds"] = unassigned.Select(id => id.ToString()).ToArray() });
        }

        var feePercent = await BillQueryCoordinator.ResolveFeePercentAsync(_db, session.TenantId, cancellationToken);
        var waived = (request.ServiceFeeWaivedPersons ?? Array.Empty<int>()).ToHashSet();

        var billItems = activeItems
            .Select(i => new BillSplitItem(i.Id, BillQueryCoordinator.ItemName(i), i.TotalPrice, BillQueryCoordinator.IsStillCooking(i), itemToPerson[i.Id]))
            .ToList();

        var result = BillSplitCalculator.CalculateByItem(billItems, feePercent, waived);

        var itemResponses = items.Select(i => new BillItemResponse(
            i.Id, BillQueryCoordinator.ItemName(i), i.TotalPrice, BillQueryCoordinator.IsStillCooking(i),
            itemToPerson.TryGetValue(i.Id, out var person) ? person : null)).ToList();

        var pendingItems = BillQueryCoordinator.BuildPendingItems(items);

        var response = new BillResponse(
            itemResponses,
            result.Subtotal,
            result.ServiceFeeNominal,
            result.Total,
            "BY_ITEM",
            result.Parts.Select(p => new BillSplitPartResponse(p.Person, p.Amount, p.ServiceFeeAmount, p.ServiceFeeWaived)).ToList(),
            pendingItems,
            pendingItems.Count > 0,
            AmountPaid: null,
            RemainingAmount: null,
            UnassignedItemIds: Array.Empty<Guid>());

        return Result<BillResponse>.Success(response);
    }
}
