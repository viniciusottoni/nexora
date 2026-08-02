namespace Nexora.Shared.Errors;

/// <summary>
/// Códigos de erro do módulo Auth (ADR-021) — porta de packages/domain/src/auth/errors.ts.
/// Os valores de <see cref="AuthDeviceNotRegistered"/>, <see cref="AuthPinLocked"/> já são
/// tratados explicitamente em <c>ResultExtensions.MapStatusCode</c> (Api.Edge/Api.Cloud); os
/// demais seguem a convenção de sufixo/prefixo (ex.: "INVALID_CREDENTIALS" → 401).
/// </summary>
public static partial class ApiErrorCodes
{
    /// <summary>Credenciais inválidas (PIN ou senha) — nunca distingue "usuário não existe" de "senha errada" (RNF-SEG-15).</summary>
    public const string AuthInvalidCredentials = "AUTH_INVALID_CREDENTIALS";

    /// <summary>Dispositivo não cadastrado, inativo, ou sem X-Device-Secret válido (login por PIN, edge).</summary>
    public const string AuthDeviceNotRegistered = "DEVICE_NOT_REGISTERED";

    /// <summary>Bloqueio temporário por excesso de tentativas (5 tentativas / 15 min) — PIN ou senha.</summary>
    public const string AuthPinLocked = "PIN_LOCKED";

    /// <summary>Usuário bloqueado (AppUser.Status = Blocked) e ainda dentro da janela de bloqueio.</summary>
    public const string AuthUserBlocked = "AUTH_USER_BLOCKED";

    /// <summary>Usuário inativo (AppUser.Status = Inactive) — não pode autenticar.</summary>
    public const string AuthUserInactive = "AUTH_USER_INACTIVE";

    /// <summary>Ação sensível sem mapeamento de permissão conhecido (ADR-023) — configuração ausente, não erro do operador.</summary>
    public const string AuthPermissionDenied = "PERMISSION_DENIED";

    /// <summary>Elevação pontual solicitada fora de uma sessão operacional válida (sem tenant/loja/usuário/dispositivo no contexto).</summary>
    public const string AuthorizationContextInvalid = "AUTHORIZATION_CONTEXT_INVALID";

    /// <summary>
    /// <c>X-Authorization-Token</c> ausente, com assinatura/expiração inválida (mais de 120s), ou
    /// emitido para uma ação sensível diferente da que está sendo protegida — porta de
    /// <c>Nexora.Application.Abstractions.Security.IAuthorizationTokenValidator</c> (US-004, gap
    /// "autorização pontual é só emitida, nunca validada"). Literal do exemplo do ADR-021
    /// ("família AUTHORIZATION_REQUIRED -&gt; abre diálogo de PIN do gerente"); antes desta correção
    /// nenhum código emitia isto — <c>ResultExtensions.MapErrorCode</c> mapeava um valor morto.
    /// </summary>
    public const string AuthorizationRequired = "AUTHORIZATION_REQUIRED";

    /// <summary>Sessão sem atividade recente há mais tempo que o timeout configurado (TenantConfig.Operation.sessionInactivityMinutes) — exige nova autenticação.</summary>
    public const string AuthSessionIdleTimeout = "AUTH_SESSION_IDLE_TIMEOUT";
}
