namespace Nexora.Contracts.Auth;

/// <summary>
/// Corpo de POST /v1/auth/pin (edge) — porta de pinLoginSchema (packages/contracts/src/auth.ts).
/// O segredo do dispositivo vem do header <c>X-Device-Secret</c>, não do corpo.
/// </summary>
public sealed record PinLoginRequest(Guid DeviceId, string Pin);

/// <summary>Resposta de login por PIN — porta de PinLoginResult (authentication-service.ts). Sem refresh token (sessão de terminal).</summary>
public sealed record PinLoginResponse(
    string AccessToken,
    AuthenticatedUserSummary User,
    IReadOnlyList<string> Permissions);
