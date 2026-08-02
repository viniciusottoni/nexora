namespace Nexora.Application.Auth.Shared;

/// <summary>Porta de hasPermission (packages/domain/src/auth/authorization.ts) — suporta curinga total ("*") e por recurso ("order:*").</summary>
internal static class PermissionEvaluator
{
    public static bool HasPermission(IReadOnlyList<string> permissions, string required) =>
        permissions.Any(permission =>
            permission == "*" ||
            permission == required ||
            (permission.EndsWith(":*", StringComparison.Ordinal) &&
             required.StartsWith(permission[..^1], StringComparison.Ordinal)));
}
