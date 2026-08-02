namespace Awaken.Application.Common.Interfaces;

public interface ITotpService
{
    string GenerateSecret();
    string GetQrCodeUri(string email, string secret, string issuer = "AWAKEN Admin");
    bool VerifyCode(string secret, string code);
    string GenerateQrCodeBase64(string uri);
}
