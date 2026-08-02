using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.Modifiers.Commands.UpdateModifier;

/// <summary>
/// Atualiza o <c>price_delta</c> de um modificador já existente. Porta de
/// <c>PATCH /v1/catalog/modifier-groups/{groupId}/modifiers/{modifierId}</c> (US-012).
/// </summary>
public sealed record UpdateModifierCommand(Guid GroupId, Guid ModifierId, decimal PriceDelta) : ICommand<ModifierResponse>;
