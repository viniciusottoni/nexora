using Awaken.Contracts.Inventory;
using MediatR;

namespace Awaken.Application.Shop.Commands.PurchaseShopItem;

/// Compra mock de item de loja (sem integracao de pagamento real - ver
/// ADR-022). Adiciona 1 unidade do item ao inventario do usuario.
public record PurchaseShopItemCommand(string ItemKey) : IRequest<InventoryItemResponse>;
