using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.ProductModifierGroups.Commands.LinkModifierGroupToProduct;

/// <summary>Vincula um grupo de modificadores a um produto (reuso N:N). Porta de <c>POST /v1/catalog/products/{productId}/modifier-groups</c> (US-012).</summary>
public sealed record LinkModifierGroupToProductCommand(Guid ProductId, Guid GroupId, short SortOrder)
    : ICommand<ProductModifierGroupResponse>;
