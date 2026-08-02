using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Roles;

namespace Nexora.Application.Roles.Commands.UpdateRole;

/// <summary>Atualiza nome e/ou permissões de um papel do tenant autenticado. Porta de <c>PATCH /v1/roles/:id</c>.</summary>
public sealed record UpdateRoleCommand(Guid RoleId, string? Name, IReadOnlyList<string>? Permissions) : ICommand<RoleResponse>;
