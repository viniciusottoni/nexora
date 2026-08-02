namespace Nexora.Application.Auth.Shared;

/// <summary>Mensagens em português — porta literal de packages/domain/src/auth/errors.ts (ADR-021: erro ao usuário final é em pt-BR).</summary>
internal static class AuthErrorMessages
{
    public const string InvalidCredentials = "Credenciais inválidas";
    public const string DeviceNotRegistered = "Dispositivo não autorizado";
    public const string PinLocked = "Acesso temporariamente bloqueado. Aguarde para tentar novamente.";
    public const string PermissionDenied = "Você não tem permissão para executar esta ação.";
    public const string UserBlocked = "Este usuário está bloqueado. Aguarde para tentar novamente ou contate um administrador.";
    public const string UserInactive = "Este usuário está inativo.";
    public const string AuthorizationContextInvalid = "A autorização exige uma sessão operacional vinculada ao dispositivo.";
    public const string AuthorizationTokenInvalid = "Token de autorização ausente, expirado ou inválido para esta ação.";
    public const string SessionIdleTimeout = "Sessão expirada por inatividade. Autentique-se novamente.";
}
