using Nexora.Application.Abstractions.Security;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;

namespace Nexora.Infrastructure.Installations;

/// <summary>
/// Verificação Ed25519 — porta de <c>createPublicKey({format:'der', type:'spki'}) + verify(...)</c>
/// (<c>installation-auth.guard.ts</c>). O BCL do .NET (checado até .NET 10) não expõe Ed25519
/// nativo, então usa-se BouncyCastle (gerenciado, sem dependência nativa de libsodium — mais
/// previsível para rodar no mini-PC da loja do que NSec/libsodium). <c>PublicKeyFactory</c>
/// entende diretamente o blob SPKI/DER armazenado em <c>EdgeInstallation.PublicKey</c> (base64).
/// </summary>
public sealed class InstallationSignatureVerifier : IInstallationSignatureVerifier
{
    public bool Verify(string message, string publicKeyBase64, string signatureBase64)
    {
        try
        {
            var keyInfo = Convert.FromBase64String(publicKeyBase64);
            if (PublicKeyFactory.CreateKey(keyInfo) is not Ed25519PublicKeyParameters publicKey)
            {
                return false;
            }

            var signer = new Ed25519Signer();
            signer.Init(forSigning: false, publicKey);

            var messageBytes = System.Text.Encoding.UTF8.GetBytes(message);
            signer.BlockUpdate(messageBytes, 0, messageBytes.Length);

            var signatureBytes = Convert.FromBase64String(signatureBase64);
            return signer.VerifySignature(signatureBytes);
        }
        catch
        {
            // Qualquer falha de parsing/formato é tratada como assinatura inválida — o handler
            // (AuthenticateInstallationRequestCommandHandler) já colapsa tudo num único código
            // de erro genérico, então não há necessidade de distinguir o motivo aqui.
            return false;
        }
    }
}
