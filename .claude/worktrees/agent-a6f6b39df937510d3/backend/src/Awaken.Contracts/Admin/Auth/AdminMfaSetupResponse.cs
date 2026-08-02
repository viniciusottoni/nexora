namespace Awaken.Contracts.Admin.Auth;

public record AdminMfaSetupResponse(string QrCodeBase64, string SecretKey);
