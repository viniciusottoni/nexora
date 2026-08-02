namespace Nexora.Contracts.Auth;

/// <summary>
/// Corpo de POST /v1/auth/login (cloud) — porta de passwordLoginSchema. <c>Otp</c> só é exigido
/// quando o usuário tem MFA ativo ou é PLATFORM_ADMIN (password-authentication.ts).
/// </summary>
public sealed record PasswordLoginRequest(string Email, string Password, string? Otp);

/// <summary>Resposta comum a login por senha e a refresh — porta de PasswordAuthResponseDto (auth.dto.ts).</summary>
public sealed record PasswordAuthResponse(
    string AccessToken,
    string RefreshToken,
    AuthenticatedUserSummary User,
    AuthenticatedTenantSummary Tenant,
    IReadOnlyList<string> Permissions);
