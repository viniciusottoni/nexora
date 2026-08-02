using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Orders.Support;
using Nexora.Application.Tables.Billing;
using Nexora.Contracts.Operation;
using Nexora.Domain.Cashier;
using Nexora.Domain.Operation;
using Nexora.Shared.Errors;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tables.Sessions;

/// <summary>
/// Núcleo único de "consultar a divisão da conta" (US-027) — chamado tanto por
/// <c>GetBillQueryHandler</c> (staff, <c>GET /v1/sessions/{id}/bill</c>) quanto por
/// <c>GetCurrentSessionBillQueryHandler</c> (cliente, <c>GET /v1/public/sessions/current/bill</c>,
/// US-027 §10: "Cliente pode pré-visualizar a divisão"). Mesmo espírito de
/// <see cref="BillRequestCoordinator"/>: dois pontos de entrada, um único cálculo, para que os dois
/// nunca divirjam.
/// </summary>
internal static class BillQueryCoordinator
{
    private static readonly string[] StillCookingStatuses =
    {
        nameof(OrderItemStatus.Queued), nameof(OrderItemStatus.Fired),
        nameof(OrderItemStatus.InOven), nameof(OrderItemStatus.OutOfOven)
    };

    /// <summary>Itens (não cancelados e cancelados) de todos os pedidos da sessão, com variante/produto carregados — reaproveitado por <c>AssignBillItemsCommandHandler</c>.</summary>
    public static async Task<List<OrderItem>> LoadItemsAsync(IApplicationDbContext db, Guid sessionId, CancellationToken cancellationToken) =>
        await db.OrderItems
            .AsNoTracking()
            .Where(i => db.Orders.Any(o => o.Id == i.OrderId && o.SessionId == sessionId))
            .Include(i => i.Variant).ThenInclude(v => v.Product)
            .OrderBy(i => i.PlacedAt)
            .ToListAsync(cancellationToken);

    public static async Task<decimal> ResolveFeePercentAsync(IApplicationDbContext db, Guid tenantId, CancellationToken cancellationToken)
    {
        var tenantConfig = await db.TenantConfigs
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.TenantId == tenantId, cancellationToken);
        return ServiceFeePolicy.ResolvePercent(tenantConfig?.Operation);
    }

    public static bool IsStillCooking(OrderItem item) => StillCookingStatuses.Contains(item.Status.ToString());

    public static string ItemName(OrderItem item) => $"{item.Variant.Product.Name} {item.Variant.Name}".Trim();

    public static List<BillPendingItemResponse> BuildPendingItems(IReadOnlyList<OrderItem> items) =>
        items.Where(IsStillCooking)
            .Select(i => new BillPendingItemResponse(i.Id, ItemName(i), OrderItemStatusLabels.ToWireStatus(i.Status)))
            .ToList();

    public static async Task<Result<BillResponse>> BuildAsync(
        IApplicationDbContext db,
        TableSession session,
        string? requestedSplitMode,
        short? requestedPeople,
        decimal? requestedAmount,
        string? waivedCsv,
        CancellationToken cancellationToken)
    {
        var items = await LoadItemsAsync(db, session.Id, cancellationToken);
        var feePercent = await ResolveFeePercentAsync(db, session.TenantId, cancellationToken);

        var activeItems = items.Where(i => i.Status != OrderItemStatus.Cancelled).ToList();
        var subtotal = activeItems.Sum(i => i.TotalPrice);

        var splitMode = string.IsNullOrWhiteSpace(requestedSplitMode) ? (session.SplitMode ?? "BY_PERSON") : requestedSplitMode!;
        var waived = ParseWaived(waivedCsv);

        var itemResponses = items.Select(i => new BillItemResponse(
            i.Id, ItemName(i), i.TotalPrice, Pending: IsStillCooking(i), AssignedPerson: null)).ToList();

        var pendingItems = BuildPendingItems(items);

        switch (splitMode)
        {
            case "BY_PERSON":
            case "SINGLE":
            {
                var people = splitMode == "SINGLE" ? 1 : (int)(requestedPeople ?? session.SplitPeople ?? 1);
                if (people < 1)
                {
                    return Result<BillResponse>.Failure(
                        "A quantidade de pessoas precisa ser maior que zero.", ApiErrorCodes.BillInvalidAmount);
                }

                var result = BillSplitCalculator.CalculateByPerson(subtotal, feePercent, people, waived);
                return Result<BillResponse>.Success(ToResponse(itemResponses, pendingItems, splitMode, result, amountPaid: null, remaining: null));
            }

            case "BY_ITEM":
            {
                // GET sem atribuições (US-027 §7): lista os itens para a UI montar a atribuição por
                // toque; o cálculo de fato só acontece em POST .../assign-items (ver
                // AssignBillItemsCommandHandler) — todos os itens não cancelados aparecem como
                // "não atribuídos" aqui, por design (ver docstring de AssignBillItemsRequest).
                var unassigned = activeItems.Select(i => i.Id).ToArray();
                var serviceFeeNominal = Math.Round(subtotal * feePercent / 100m, 2, MidpointRounding.AwayFromZero);
                var response = new BillResponse(
                    itemResponses, subtotal, serviceFeeNominal, subtotal + serviceFeeNominal, splitMode,
                    Split: Array.Empty<BillSplitPartResponse>(), pendingItems, pendingItems.Count > 0,
                    AmountPaid: null, RemainingAmount: null, UnassignedItemIds: unassigned);
                return Result<BillResponse>.Success(response);
            }

            case "BY_AMOUNT":
            {
                if (requestedAmount is not { } amount || amount <= 0)
                {
                    return Result<BillResponse>.Failure(
                        "Informe um valor maior que zero para calcular o pagamento parcial.", ApiErrorCodes.BillInvalidAmount);
                }

                var serviceFeeNominal = Math.Round(subtotal * feePercent / 100m, 2, MidpointRounding.AwayFromZero);
                var total = subtotal + serviceFeeNominal;
                var alreadyPaid = await SumPaidAsync(db, session.Id, cancellationToken);

                if (amount > total - alreadyPaid)
                {
                    return Result<BillResponse>.Failure(
                        "O valor informado é maior que o saldo em aberto da conta.", ApiErrorCodes.BillInvalidAmount);
                }

                var amountSplit = BillSplitCalculator.CalculateByAmount(total, alreadyPaid, amount);
                var response = new BillResponse(
                    itemResponses, subtotal, serviceFeeNominal, total, splitMode,
                    Split: Array.Empty<BillSplitPartResponse>(), pendingItems, pendingItems.Count > 0,
                    AmountPaid: amountSplit.AmountNow, RemainingAmount: amountSplit.Remaining, UnassignedItemIds: Array.Empty<Guid>());
                return Result<BillResponse>.Success(response);
            }

            default:
                return Result<BillResponse>.Failure(
                    "Modo de divisão inválido. Escolha por pessoa, por item ou por valor.", ApiErrorCodes.BillInvalidAmount);
        }
    }

    /// <summary>Soma pagamentos já registrados para a sessão que efetivamente contam como recebidos (Paid/Authorized) — Cancelled/Failed nunca compõem o saldo pago.</summary>
    public static async Task<decimal> SumPaidAsync(IApplicationDbContext db, Guid sessionId, CancellationToken cancellationToken) =>
        await db.Payments
            .AsNoTracking()
            .Where(p => p.SessionId == sessionId && (p.Status == PaymentStatus.Paid || p.Status == PaymentStatus.Authorized))
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

    public static HashSet<int> ParseWaived(string? csv)
    {
        var result = new HashSet<int>();
        if (string.IsNullOrWhiteSpace(csv))
        {
            return result;
        }

        foreach (var part in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(part, out var person))
            {
                result.Add(person);
            }
        }

        return result;
    }

    private static BillResponse ToResponse(
        IReadOnlyList<BillItemResponse> items,
        IReadOnlyList<BillPendingItemResponse> pendingItems,
        string splitMode,
        BillSplitResult result,
        decimal? amountPaid,
        decimal? remaining) =>
        new(
            items,
            result.Subtotal,
            result.ServiceFeeNominal,
            result.Total,
            splitMode,
            result.Parts.Select(p => new BillSplitPartResponse(p.Person, p.Amount, p.ServiceFeeAmount, p.ServiceFeeWaived)).ToList(),
            pendingItems,
            pendingItems.Count > 0,
            amountPaid,
            remaining,
            result.UnassignedItemIds);
}
