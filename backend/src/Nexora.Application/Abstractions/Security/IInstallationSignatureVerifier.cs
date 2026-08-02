namespace Nexora.Application.Abstractions.Security;

/// <summary>
/// Verifica a assinatura Ed25519 de uma requisição assinada pelo edge (protocolo de
/// autenticação da instalação — ver ADR-031 "chave pública Ed25519" e o guard original
/// <c>installation-auth.guard.ts</c>). Implementado em Infrastructure para manter o mecanismo
/// criptográfico concreto (BCL ou pacote dedicado) fora de Application (ADR-039).
/// </summary>
public interface IInstallationSignatureVerifier
{
    /// <summary>
    /// <paramref name="message"/> é sempre <c>{método}\n{caminho+query}\n{timestamp}\n{nonce}</c>,
    /// <paramref name="publicKeyBase64"/> a chave pública SPKI (DER) em base64 armazenada em
    /// <c>EdgeInstallation.PublicKey</c>, <paramref name="signatureBase64"/> o header
    /// <c>X-Installation-Signature</c>.
    /// </summary>
    bool Verify(string message, string publicKeyBase64, string signatureBase64);
}
