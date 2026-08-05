namespace Nexora.Application.Abstractions.Platform;

/// <summary>
/// Resultado da emissão/renovação de certificado TLS (US-143 §3.1) — <see cref="Succeeded"/>
/// falso significa que <c>VerifyTenantDomainCommandHandler</c>/
/// <c>RenewTenantDomainCertificateCommandHandler</c> devem chamar
/// <c>TenantDomain.MarkCertificateFailed()</c> em vez de <c>IssueCertificate</c>.
/// </summary>
public sealed record CertificateIssuanceResult(bool Succeeded, DateTimeOffset? IssuedAt, DateTimeOffset? ExpiresAt)
{
    public static CertificateIssuanceResult Failed { get; } = new(false, null, null);
}

/// <summary>
/// Emissão/renovação automática de certificado TLS para um domínio próprio verificado (US-143
/// §3.1, cenário "Certificado automático") — porta usada tanto por
/// <c>VerifyTenantDomainCommandHandler</c> (emissão inicial, logo após a verificação de posse)
/// quanto por <c>RenewTenantDomainCertificateCommandHandler</c> (renovação, disparada pelo
/// <c>TenantDomainCertificateRenewalWorker</c> quando <c>TenantDomain.IsCertificateExpiringSoon</c>
/// vira verdadeiro).
/// </summary>
public interface ICertificateIssuer
{
    Task<CertificateIssuanceResult> IssueAsync(string domain, CancellationToken cancellationToken);
}
