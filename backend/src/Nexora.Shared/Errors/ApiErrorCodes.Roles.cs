namespace Nexora.Shared.Errors;

/// <summary>Códigos de erro do módulo de papéis (roles) e permissões (ADR-021, ADR-023).</summary>
public static partial class ApiErrorCodes
{
    public const string RoleNotFound = "ROLE_NOT_FOUND";
    public const string RoleCodeAlreadyExists = "ROLE_CODE_ALREADY_EXISTS";

    /// <summary>O papel OWNER precisa manter permissão "*" — regra que impede o estabelecimento de perder o dono do acesso total.</summary>
    public const string RoleOwnerMustKeepFullAccess = "ROLE_OWNER_MUST_KEEP_FULL_ACCESS";
}
