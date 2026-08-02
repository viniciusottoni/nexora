using Awaken.Contracts.Inventory;
using MediatR;

namespace Awaken.Application.Inventory.Commands.UseItem;

/// <summary>
/// US-230: comando para usar (consumir/aplicar) um item do inventário do usuário.
/// </summary>
public record UseItemCommand(
    string ItemKey,
    string? ContextType,
    string? ContextId,
    string UseRequestId,  // chave de idempotência
    string? PayloadJson = null
) : IRequest<UseItemResponse>;
