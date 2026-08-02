using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.Products.Commands.ActivateProduct;

/// <summary>Reativa um produto do tenant autenticado, voltando a exibi-lo nos canais de venda. Porta de <c>POST /v1/catalog/products/:id/activate</c>.</summary>
public sealed record ActivateProductCommand(Guid ProductId) : ICommand<ProductResponse>;
