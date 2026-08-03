using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Orders.Commands.AddOrderItem;
using Nexora.Contracts.Operation;

namespace Nexora.Application.Orders.Commands.CreateOrder;

/// <summary>Item de <see cref="CreateOrderCommand"/> — mesma forma de <see cref="AddOrderItemCommand"/>, sem <c>SessionId</c> (pertence ao pedido, não ao item).</summary>
public sealed record CreateOrderItemInput(
    Guid VariantId,
    short Quantity,
    string? Notes,
    IReadOnlyList<AddOrderItemModifierInput>? Modifiers,
    IReadOnlyList<AddOrderItemFractionInput>? Fractions);

/// <summary>
/// US-030 — cria um pedido de ponta a ponta (canal, comanda opcional, lista de itens) já
/// confirmado (<c>PLACED</c>, T0), com validação de TODOS os itens antes de criar QUALQUER coisa
/// (cenário Gherkin "Produto indisponível no momento do envio": "os demais itens não devem ser
/// criados parcialmente"). Reusado por dois controllers: <c>OrdersController.Create</c> (staff,
/// <c>channel</c>/<c>sessionId</c> vêm do corpo) e <c>OrdersController.CreatePublic</c> (cliente via
/// QR, <c>channel</c> fixo <c>DineIn</c> e <c>sessionId</c> resolvido das claims do token — nunca do
/// corpo, RN-015) — a distinção de quem chamou é resolvida no controller, nunca aqui (ADR-039:
/// Application não conhece HttpContext).
/// </summary>
public sealed record CreateOrderCommand(
    string Channel,
    Guid? SessionId,
    IReadOnlyList<CreateOrderItemInput> Items,
    DateTimeOffset? OccurredAt) : ICommand<CreateOrderResponse>;
