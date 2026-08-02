namespace Nexora.Shared.Security;

/// <summary>
/// Checagem de permissão por código, com suporte a curinga total (<c>"*"</c>) e por recurso
/// (<c>"device:*"</c>) — usada para montar <c>AuthorizationPolicy</c> nos <c>Program.cs</c> de
/// <c>Api.Edge</c>/<c>Api.Cloud</c> (ex.: política <c>DeviceManage</c> exigida por
/// <c>DevicesController</c>, US-005).
/// </summary>
/// <remarks>
/// Mesma semântica de <c>Nexora.Application.Auth.Shared.PermissionEvaluator</c> (ADR-023),
/// deliberadamente duplicada aqui em vez de reaproveitada: aquele helper é <c>internal</c> ao
/// módulo de Auth (fora de escopo desta US — US-004 vai corrigir RBAC completo depois) e a
/// checagem de policy precisa rodar em <c>Program.cs</c>, na composição da autorização, antes de
/// qualquer handler MediatR existir. Consolidar num único lugar quando a US-004 definir onde a
/// checagem de permissão granular deve viver — não antes, para não acoplar esta US a uma decisão
/// de outra.
/// </remarks>
public static class PermissionAuthorization
{
    /// <summary>Claim que carrega os códigos de permissão do usuário autenticado (ver <c>ICurrentTenantContext.Permissions</c>).</summary>
    public const string PermissionClaimType = "perms";

    public static bool HasPermission(IEnumerable<string> permissions, string required) =>
        permissions.Any(permission =>
            permission == "*" ||
            permission == required ||
            (permission.EndsWith(":*", StringComparison.Ordinal) &&
             required.StartsWith(permission[..^1], StringComparison.Ordinal)));
}
