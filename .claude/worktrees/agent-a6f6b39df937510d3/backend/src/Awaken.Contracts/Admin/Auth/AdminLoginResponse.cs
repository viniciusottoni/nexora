namespace Awaken.Contracts.Admin.Auth;

public record AdminLoginResponse(
    string? AccessToken,
    bool RequiresMfaSetup,
    bool RequiresMfaChallenge,
    Guid? AdminUserId);
