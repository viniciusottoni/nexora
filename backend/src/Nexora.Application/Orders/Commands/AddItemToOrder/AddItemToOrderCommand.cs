using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Orders.Commands.AddOrderItem;
using Nexora.Contracts.Operation;

namespace Nexora.Application.Orders.Commands.AddItemToOrder;

/// <summary>
/// US-030 §7, <c>POST /v1/orders/{id}/items</c> — acréscimo de item a um pedido JÁ CONFIRMADO
/// (<c>PLACED</c>/<c>IN_PRODUCTION</c>, cenário Gherkin "Acréscimo a pedido já confirmado"),
/// identificado pelo <c>orderId</c> em vez do <c>sessionId</c> de <see cref="AddOrderItemCommand"/>.
///
/// [DECISÃO] Comando NOVO em vez de estender <see cref="AddOrderItemCommand"/>: aquele é acoplado a
/// UMA sessão de mesa aberta (inclusive a regra "novo item reabre a comanda que pediu a conta",
/// US-026 §4) e sempre cria/reaproveita um pedido de canal <c>DineIn</c>; este endpoint recebe o
/// <c>orderId</c> diretamente e precisa funcionar para QUALQUER canal (o pedido já existe, pode ter
/// nascido sem mesa nenhuma via <c>POST /v1/orders</c>) — misturar as duas responsabilidades em um
/// único comando exigiria parâmetros opcionais mutuamente exclusivos e IFs cruzados. Reaproveita,
/// isso sim, os MESMOS blocos de validação (<see cref="Nexora.Application.Orders.Support.ModifierGroupValidator"/>,
/// <see cref="Nexora.Application.Orders.Support.OrderItemPriceResolver"/>) e o mesmo evento
/// <c>order.item.added</c> (EVT-003).
/// </summary>
public sealed record AddItemToOrderCommand(
    Guid OrderId,
    Guid VariantId,
    short Quantity,
    string? Notes,
    IReadOnlyList<AddOrderItemModifierInput>? Modifiers,
    IReadOnlyList<AddOrderItemFractionInput>? Fractions,
    DateTimeOffset? OccurredAt,
    Guid? RequestingSessionId) : ICommand<OrderItemResponse>;
