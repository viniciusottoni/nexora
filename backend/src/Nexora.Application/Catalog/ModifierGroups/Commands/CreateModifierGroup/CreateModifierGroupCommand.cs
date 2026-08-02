using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.ModifierGroups.Commands.CreateModifierGroup;

/// <summary>Cria um grupo de modificadores no tenant autenticado. Porta de <c>POST /v1/catalog/modifier-groups</c> (US-012).</summary>
public sealed record CreateModifierGroupCommand(
    string Name,
    short MinSelect,
    short MaxSelect,
    bool IsRequired,
    short SortOrder) : ICommand<ModifierGroupResponse>;
