using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Operation;

namespace Nexora.Application.Orders.Commands.AddOrderItem;

/// <summary>Modificador pedido junto do item — ver docstring de <see cref="AddOrderItemCommand"/>.</summary>
public sealed record AddOrderItemModifierInput(Guid ModifierId, short Quantity);

/// <summary>Fração (meio a meio) pedida junto do item — ver docstring de <see cref="AddOrderItemCommand"/>.</summary>
public sealed record AddOrderItemFractionInput(Guid VariantId, decimal Weight);

/// <summary>
/// Comando interno MÍNIMO de "lançar item no pedido de uma sessão" — ver docstring completa do
/// gap de escopo em <see cref="AddOrderItemCommandHandler"/>.
/// </summary>
public sealed record AddOrderItemCommand(
    Guid SessionId,
    Guid VariantId,
    short Quantity,
    string? Notes,
    IReadOnlyList<AddOrderItemModifierInput>? Modifiers,
    IReadOnlyList<AddOrderItemFractionInput>? Fractions) : ICommand<OrderItemResponse>;
