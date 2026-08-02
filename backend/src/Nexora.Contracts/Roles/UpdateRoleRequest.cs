namespace Nexora.Contracts.Roles;

/// <summary>Corpo de <c>PATCH /v1/roles/:id</c> — espelha <c>UpdateRoleDto</c> do NestJS original.</summary>
public sealed record UpdateRoleRequest(string? Name, IReadOnlyList<string>? Permissions);
