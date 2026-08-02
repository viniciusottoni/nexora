using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.Modifiers.Commands.CreateModifier;

/// <summary>Cria um modificador (opção) dentro de um grupo. Porta de <c>POST /v1/catalog/modifier-groups/{groupId}/modifiers</c> (US-012).</summary>
public sealed record CreateModifierCommand(
    Guid GroupId,
    string Name,
    decimal PriceDelta,
    Guid? IngredientId,
    decimal? Quantity,
    short SortOrder) : ICommand<ModifierResponse>;
