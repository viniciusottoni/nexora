using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Roles;

namespace Nexora.Application.Roles.Queries.ListRoles;

/// <summary>Porta de <c>GET /v1/roles</c> — lista os papéis do tenant autenticado e o catálogo de permissões disponíveis.</summary>
public sealed record ListRolesQuery : IQuery<RoleListResponse>;
