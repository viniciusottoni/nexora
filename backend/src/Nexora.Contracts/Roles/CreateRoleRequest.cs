namespace Nexora.Contracts.Roles;

/// <summary>Corpo de <c>POST /v1/roles</c> — espelha <c>CreateRoleDto</c> do NestJS original.</summary>
public sealed record CreateRoleRequest(string Code, string Name, IReadOnlyList<string>? Permissions);
