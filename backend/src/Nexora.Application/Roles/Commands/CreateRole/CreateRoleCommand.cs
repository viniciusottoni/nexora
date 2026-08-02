using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Roles;

namespace Nexora.Application.Roles.Commands.CreateRole;

/// <summary>Cria um papel customizado no tenant autenticado. Porta de <c>POST /v1/roles</c>.</summary>
public sealed record CreateRoleCommand(string Code, string Name, IReadOnlyList<string> Permissions) : ICommand<RoleResponse>;
