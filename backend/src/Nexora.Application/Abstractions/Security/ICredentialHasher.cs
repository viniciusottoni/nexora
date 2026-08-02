namespace Nexora.Application.Abstractions.Security;

/// <summary>Hash de PIN/senha via Argon2 (implementado em Infrastructure) — porta do argon2 usado no NestJS.</summary>
public interface ICredentialHasher
{
    string Hash(string plainText);

    bool Verify(string hash, string plainText);
}

/// <summary>Digest determinístico e não reversível usado só para lookup por PIN (nunca para verificação de senha).</summary>
public interface IPinLookupDigester
{
    string Digest(string pin);
}

/// <summary>Digest de refresh token / segredo de dispositivo para armazenamento (nunca guardar o valor em claro).</summary>
public interface ISecretDigester
{
    string Digest(string value);
}

/// <summary>
/// Verificação de código TOTP (RFC 6238) — porta do <c>OtpVerifier</c>/<c>TotpVerifier</c>
/// (packages/domain/src/auth/password-authentication.ts, apps/api-cloud/.../crypto-adapters.ts).
/// Usado quando o usuário tem MFA ativo ou é <c>PLATFORM_ADMIN</c>.
/// </summary>
public interface IOtpVerifier
{
    bool Verify(string secret, string otp);
}
