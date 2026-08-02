namespace Nexora.Contracts.Auth;

/// <summary>Corpo de POST /v1/auth/refresh (cloud) — porta de refreshRequestSchema.</summary>
public sealed record RefreshTokenRequest(string RefreshToken);
