namespace Nexora.Application.Abstractions.Security;

/// <summary>
/// Cifra/decifra o segredo MFA armazenado em repouso em <c>AppUser.MfaSecret</c> — porta de
/// <c>MfaSecretCipher</c> (apps/api-cloud/src/modules/auth/crypto-adapters.ts). O segredo TOTP
/// nunca é gravado em claro; o envelope cifrado é decifrado só no momento de verificar o OTP.
/// </summary>
public interface IMfaSecretCipher
{
    string Encrypt(string plaintext);

    string Decrypt(string envelope);
}
