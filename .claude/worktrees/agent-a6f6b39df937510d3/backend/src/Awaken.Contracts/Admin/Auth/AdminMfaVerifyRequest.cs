namespace Awaken.Contracts.Admin.Auth;

public record AdminMfaVerifyRequest(Guid AdminUserId, string Code);
