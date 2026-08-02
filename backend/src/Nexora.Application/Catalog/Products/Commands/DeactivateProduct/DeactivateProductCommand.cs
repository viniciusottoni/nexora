using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.Products.Commands.DeactivateProduct;

/// <summary>
/// Desativa (nunca exclui fisicamente) um produto do tenant autenticado — some dos canais de
/// venda, mas pedidos históricos continuam exibindo-o corretamente (US-010 §4, cenário
/// "Desativação de produto"). Distinto de <c>MarkUnavailable</c> (US-015, indisponibilidade
/// operacional temporária — ex.: acabou o insumo). Porta de
/// <c>POST /v1/catalog/products/:id/deactivate</c>.
/// </summary>
public sealed record DeactivateProductCommand(Guid ProductId) : ICommand<ProductResponse>;
