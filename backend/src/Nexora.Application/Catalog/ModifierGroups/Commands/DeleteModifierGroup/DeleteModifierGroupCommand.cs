using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Catalog.ModifierGroups.Commands.DeleteModifierGroup;

/// <summary>
/// Remove (soft delete) um grupo de modificadores e cascateia para seus modificadores e vínculos
/// de produto. Porta de <c>DELETE /v1/catalog/modifier-groups/{id}</c> (US-012).
/// </summary>
public sealed record DeleteModifierGroupCommand(Guid GroupId) : ICommand;
