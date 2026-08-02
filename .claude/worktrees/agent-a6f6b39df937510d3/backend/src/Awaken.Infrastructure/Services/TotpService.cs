using Awaken.Application.Common.Interfaces;
using OtpNet;
using QRCoder;

namespace Awaken.Infrastructure.Services;

public class TotpService : ITotpService
{
    public string GenerateSecret() =>
        Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));

    public string GetQrCodeUri(string email, string secret, string issuer = "AWAKEN Admin") =>
        $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits=6&period=30";

    public bool VerifyCode(string secret, string code)
    {
        var keyBytes = Base32Encoding.ToBytes(secret);
        var totp = new Totp(keyBytes);
        return totp.VerifyTotp(code, out _, new VerificationWindow(2, 2));
    }

    public string GenerateQrCodeBase64(string uri)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var bytes = qrCode.GetGraphic(5);
        return Convert.ToBase64String(bytes);
    }
}
