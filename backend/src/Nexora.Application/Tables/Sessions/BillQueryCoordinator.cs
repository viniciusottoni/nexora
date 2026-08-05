using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Orders.Support;
using Nexora.Application.Tables.Billing;
using Nexora.Application.Tables.Support;
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
    // US-035 §8: "não entregue" também cobre READY (pronto, mas ainda não levado à mesa) — antes
    // desta história só as fases de cozinha propriamente ditas contavam como pendente aqui; RÉADY
    // adicionado para que este mesmo indicador (usado por US-027 na tela de divisão) já reflita a
    // regra de bloqueio de fechamento sem duplicar uma segunda lista de "ainda não servido" à parte
    // (ver PendingItemsClosePolicy.FindPendingForClose, que usa a definição completa SERVED/CANCELLED
    // diretamente da entidade — aqui o filtro é o mesmo, só que espelhado por nome de status).
    private static readonly string[] StillCookingStatuses =
    {
        nameof(OrderItemStatus.Queued), nameof(OrderItemStatus.Fired),
        nameof(OrderItemStatus.InOven), nameof(OrderItemStatus.OutOfOven), nameof(OrderItemStatus.Ready)
    };

    /// <summary>
    /// Itens (não cancelados e cancelados) de todos os pedidos da sessão, com variante/produto,
    /// modificadores e frações carregados — reaproveitado por <c>AssignBillItemsCommandHandler</c>.
    /// US-051 §4 ("Conta completa"/"Item cancelado excluído") precisa dos modificadores e frações
    /// discriminados no detalhamento, e do item cancelado presente na lista (riscado, não oculto).
    /// </summary>
    public static async Task<List<OrderItem>> LoadItemsAsync(IApplicationDbContext db, Guid sessionId, CancellationToken cancellationToken) =>
        await db.OrderItems
            .AsNoTracking()
            .Where(i => db.Orders.Any(o => o.Id == i.OrderId && o.SessionId == sessionId))
            .Include(i => i.Variant).ThenInclude(v => v.Product)
            .Include(i => i.Modifiers)
            .Include(i => i.Fractions).ThenInclude(f => f.Variant).ThenInclude(v => v.Product)
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

    /// <summary>
    /// US-051 §4/§7 — item da conta com quantidade, preço unitário, modificadores, frações do meio
    /// a meio e sinalização de cancelado, além dos campos herdados de US-027. Item CANCELADO
    /// aparece aqui (nunca filtrado da lista) para conferência do caixa — só não compõe o subtotal
    /// (ver <see cref="BuildAsync"/>, que soma apenas <c>activeItems</c>).
    /// </summary>
    public static BillItemResponse BuildItemResponse(OrderItem item, int? assignedPerson = null) =>
        new(
            item.Id,
            ItemName(item),
            item.TotalPrice,
            Pending: IsStillCooking(item),
            AssignedPerson: assignedPerson,
            Quantity: item.Quantity,
            UnitPrice: item.UnitPrice,
            Modifiers: item.Modifiers.Select(m => new BillItemModifierResponse(m.NameSnapshot, m.PriceDelta)).ToList(),
            Fractions: item.Fractions
                .OrderBy(f => f.SortOrder)
                .Select(f => new BillItemFractionResponse($"{f.Variant.Product.Name} {f.Variant.Name}".Trim(), f.Weight))
                .ToList(),
            Cancelled: item.Status == OrderItemStatus.Cancelled,
            Discount: item.Discount);

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
        var pendingItemsMode = await PendingItemsClosePolicy.ResolveModeAsync(db, session.TenantId, cancellationToken);

        var activeItems = items.Where(i => i.Status != OrderItemStatus.Cancelled).ToList();
        // "subtotal" já é líquido de desconto por ITEM (US-054 escopo ITEM — OrderItem.TotalPrice
        // subtrai OrderItem.Discount na origem, ver OrderItem.RecalculateTotal). O desconto de
        // SESSÃO (US-054 escopo SESSION) é aplicado aqui, por cima, antes de dividir/tarifar.
        var subtotal = activeItems.Sum(i => i.TotalPrice);
        var sessionDiscountAmount = Math.Round(subtotal * session.DiscountPercent / 100m, 2, MidpointRounding.AwayFromZero);
        var netSubtotal = subtotal - sessionDiscountAmount;

        // US-053 §4 — retirada da taxa no nível da SESSÃO (escopo FULL) zera a taxa para a conta
        // inteira; a retirada por PESSOA (escopo PARTIAL) continua resolvida pelo mecanismo efêmero
        // de US-027 (parâmetro `waived` abaixo), sem alterar o percentual nominal aqui.
        var serviceFeeWaivedAtSession = session.ServiceFeeWaived && session.ServiceFeeWaiveScope == "FULL";
        var effectiveFeePercent = serviceFeeWaivedAtSession ? 0m : feePercent;

        var splitMode = string.IsNullOrWhiteSpace(requestedSplitMode) ? (session.SplitMode ?? "BY_PERSON") : requestedSplitMode!;
        var waived = ParseWaived(waivedCsv);

        var itemResponses = items.Select(i => BuildItemResponse(i)).ToList();
        var pendingItems = BuildPendingItems(items);
        var now = DateTimeOffset.UtcNow;
        var sessionResponse = new BillSessionResponse(
            session.OpenedAt, (int)Math.Max(0, (now - session.OpenedAt).TotalMinutes), session.GuestCount);

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

                var result = BillSplitCalculator.CalculateByPerson(netSubtotal, effectiveFeePercent, people, waived);
                return Result<BillResponse>.Success(
                    ToResponse(
                        itemResponses, pendingItems, splitMode, result, amountPaid: null, remaining: null, pendingItemsMode,
                        subtotal, sessionDiscountAmount, feePercent, serviceFeeWaivedAtSession, sessionResponse));
            }

            case "BY_ITEM":
            {
                // GET sem atribuições (US-027 §7): lista os itens para a UI montar a atribuição por
                // toque; o cálculo de fato só acontece em POST .../assign-items (ver
                // AssignBillItemsCommandHandler) — todos os itens não cancelados aparecem como
                // "não atribuídos" aqui, por design (ver docstring de AssignBillItemsRequest).
                var unassigned = activeItems.Select(i => i.Id).ToArray();
                var serviceFeeNominal = Math.Round(netSubtotal * effectiveFeePercent / 100m, 2, MidpointRounding.AwayFromZero);
                var response = new BillResponse(
                    itemResponses, subtotal, serviceFeeNominal, netSubtotal + serviceFeeNominal, splitMode,
                    Split: Array.Empty<BillSplitPartResponse>(), pendingItems, pendingItems.Count > 0,
                    AmountPaid: null, RemainingAmount: null, UnassignedItemIds: unassigned, PendingItemsMode: pendingItemsMode,
                    Discount: sessionDiscountAmount, ServiceFeePercent: feePercent, ServiceFeeOptional: true,
                    ServiceFeeWaived: serviceFeeWaivedAtSession, Session: sessionResponse);
                return Result<BillResponse>.Success(response);
            }

            case "BY_AMOUNT":
            {
                if (requestedAmount is not { } amount || amount <= 0)
                {
                    return Result<BillResponse>.Failure(
                        "Informe um valor maior que zero para calcular o pagamento parcial.", ApiErrorCodes.BillInvalidAmount);
                }

                var serviceFeeNominal = Math.Round(netSubtotal * effectiveFeePercent / 100m, 2, MidpointRounding.AwayFromZero);
                var total = netSubtotal + serviceFeeNominal;
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
                    AmountPaid: amountSplit.AmountNow, RemainingAmount: amountSplit.Remaining, UnassignedItemIds: Array.Empty<Guid>(),
                    PendingItemsMode: pendingItemsMode, Discount: sessionDiscountAmount, ServiceFeePercent: feePercent,
                    ServiceFeeOptional: true, ServiceFeeWaived: serviceFeeWaivedAtSession, Session: sessionResponse);
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
        decimal? remaining,
        string pendingItemsMode,
        // US-051/US-053/US-054 — Subtotal/Total de `result` já refletem o subtotal LÍQUIDO de
        // desconto de sessão (ver chamador); aqui só se sobrepõe `Subtotal` para o valor BRUTO
        // (antes do desconto de sessão), que é o que a US-051 §7 chama de "subtotal" na resposta.
        decimal? grossSubtotal = null,
        decimal discount = 0,
        decimal serviceFeePercent = 0,
        bool serviceFeeWaived = false,
        BillSessionResponse? session = null) =>
        new(
            items,
            grossSubtotal ?? result.Subtotal,
            result.ServiceFeeNominal,
            result.Total,
            splitMode,
            result.Parts.Select(p => new BillSplitPartResponse(p.Person, p.Amount, p.ServiceFeeAmount, p.ServiceFeeWaived)).ToList(),
            pendingItems,
            pendingItems.Count > 0,
            amountPaid,
            remaining,
            result.UnassignedItemIds,
            pendingItemsMode,
            discount,
            serviceFeePercent,
            ServiceFeeOptional: true,
            serviceFeeWaived,
            session);
}
