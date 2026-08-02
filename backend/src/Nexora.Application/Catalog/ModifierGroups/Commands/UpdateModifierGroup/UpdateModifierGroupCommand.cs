using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.ModifierGroups.Commands.UpdateModifierGroup;

/// <summary>
/// Atualiza a regra de seleção (mínimo/máximo) de um grupo de modificadores já existente. Porta de
/// <c>PATCH /v1/catalog/modifier-groups/{id}</c> (US-012). Reflete automaticamente em todos os
/// produtos que reusam o grupo (RN "grupo vinculado a 12 produtos... alteração vale para os 12") —
/// natural pela normalização via FK, sem nenhuma cópia de dado por produto.
/// </summary>
public sealed record UpdateModifierGroupCommand(Guid GroupId, short MinSelect, short MaxSelect)
    : ICommand<ModifierGroupResponse>;
