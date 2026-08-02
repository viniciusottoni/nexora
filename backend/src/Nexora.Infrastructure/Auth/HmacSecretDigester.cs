using System.Security.Cryptography;
using System.Text;
using Nexora.Application.Abstractions.Security;
using Microsoft.Extensions.Options;

namespace Nexora.Infrastructure.Auth;

/// <summary>
/// Digest determinístico de segredo de dispositivo (edge, <c>Device.SecretHash</c>) e de refresh
/// token (cloud, <c>AuthSession.RefreshHash</c>) — porta unificada de device-secret.ts
/// (<c>validDeviceSecret</c>) e de crypto-adapters.ts (<c>Sha256SecretDigester</c>). Decisão de
/// design: ambos os digests usam o mesmo algoritmo HMAC-SHA256 peperado aqui, em vez do SHA-256
/// puro (sem pimenta) do refresh token no TS original — mais forte e mais simples de manter uma
/// única implementação, sem alterar o comportamento observável (ambos continuam sendo digests
/// determinísticos usados só para lookup por igualdade, nunca reversíveis).
/// </summary>
public sealed class HmacSecretDigester : ISecretDigester
{
    private readonly byte[] _pepper;

    public HmacSecretDigester(IOptions<AuthSecretsOptions> options)
    {
        var pepper = options.Value.SecretPepper;
        if (string.IsNullOrEmpty(pepper))
        {
            throw new InvalidOperationException("Auth:Secrets:SecretPepper não configurado.");
        }

        _pepper = Encoding.UTF8.GetBytes(pepper);
    }

    public string Digest(string value)
    {
        var hash = HMACSHA256.HashData(_pepper, Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
