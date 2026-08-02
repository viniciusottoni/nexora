using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.OpenSsl;

namespace Nexora.Infrastructure.Installation;

/// <summary>Porta de <c>installationRequestHeaders</c> — assinatura Ed25519 (ver nota de escolha do BouncyCastle em <c>InstallationSignatureVerifier</c>, lado nuvem).</summary>
public sealed class InstallationRequestSigner : IInstallationRequestSigner
{
    private readonly EdgeInstallationIdentityOptions _options;
    private Ed25519PrivateKeyParameters? _cachedPrivateKey;

    public InstallationRequestSigner(IOptions<EdgeInstallationIdentityOptions> options)
    {
        _options = options.Value;
    }

    public async Task<IReadOnlyDictionary<string, string>> SignAsync(
        string method, Uri requestUri, CancellationToken cancellationToken)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        var requestPath = requestUri.PathAndQuery;
        var message = $"{method}\n{requestPath}\n{timestamp}\n{nonce}";

        var privateKey = await LoadPrivateKeyAsync(cancellationToken);

        var signer = new Ed25519Signer();
        signer.Init(forSigning: true, privateKey);
        var messageBytes = Encoding.UTF8.GetBytes(message);
        signer.BlockUpdate(messageBytes, 0, messageBytes.Length);
        var signature = Convert.ToBase64String(signer.GenerateSignature());

        return new Dictionary<string, string>
        {
            ["X-Installation-Id"] = _options.InstallationId.ToString(),
            ["X-Installation-Timestamp"] = timestamp,
            ["X-Installation-Nonce"] = nonce,
            ["X-Installation-Signature"] = signature,
        };
    }

    private async Task<Ed25519PrivateKeyParameters> LoadPrivateKeyAsync(CancellationToken cancellationToken)
    {
        if (_cachedPrivateKey is not null)
        {
            return _cachedPrivateKey;
        }

        var pem = await File.ReadAllTextAsync(_options.PrivateKeyPath, cancellationToken);
        using var reader = new StringReader(pem);
        var pemObject = new PemReader(reader).ReadObject();

        var privateKey = pemObject switch
        {
            AsymmetricCipherKeyPair pair => (Ed25519PrivateKeyParameters)pair.Private,
            Ed25519PrivateKeyParameters key => key,
            _ => throw new InvalidOperationException(
                $"Chave privada Ed25519 inválida em '{_options.PrivateKeyPath}'."),
        };

        _cachedPrivateKey = privateKey;
        return privateKey;
    }
}
