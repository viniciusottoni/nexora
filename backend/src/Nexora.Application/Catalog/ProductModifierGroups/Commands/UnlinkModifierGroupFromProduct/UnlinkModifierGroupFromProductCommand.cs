using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Catalog.ProductModifierGroups.Commands.UnlinkModifierGroupFromProduct;

/// <summary>Remove o vínculo entre um grupo de modificadores e um produto. Porta de <c>DELETE /v1/catalog/products/{productId}/modifier-groups/{groupId}</c> (US-012).</summary>
public sealed record UnlinkModifierGroupFromProductCommand(Guid ProductId, Guid GroupId) : ICommand;
