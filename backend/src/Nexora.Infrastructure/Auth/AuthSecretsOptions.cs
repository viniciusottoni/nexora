namespace Nexora.Infrastructure.Auth;

/// <summary>
/// Pimentas (peppers) e chaves de digest/cifra do módulo Auth — porta de PIN_LOOKUP_PEPPER
/// (pin-lookup-digester.ts), DEVICE_HASH_PEPPER (device-secret.ts) e MFA_ENCRYPTION_KEY
/// (crypto-adapters.ts). <see cref="SecretPepper"/> é usado tanto para o segredo de dispositivo
/// (edge) quanto para o digest de refresh token (cloud) — unificação deliberada, ver
/// <see cref="HmacSecretDigester"/>.
/// </summary>
public sealed class AuthSecretsOptions
{
    public const string SectionName = "Auth:Secrets";

    public string PinLookupPepper { get; set; } = string.Empty;
    public string SecretPepper { get; set; } = string.Empty;
    public string MfaEncryptionKey { get; set; } = string.Empty;
}
