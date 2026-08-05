using Nexora.Domain.Common;

namespace Nexora.Domain.Platform;

/// <summary>
/// Domínio próprio cadastrado por um tenant (US-143) — completa o white-label: um cardápio em
/// <c>cardapio.donabetinha.com.br</c> em vez de <c>donabetinha.plataforma.com.br</c>. Suporta
/// múltiplos domínios por tenant (histórico, redirecionamento do padrão), diferente da coluna
/// simples <c>tenant.domain</c> (ADR-010) que só guarda o domínio primário resolvido em runtime.
/// Cada domínio resolve exatamente um tenant (RN-015) — a unicidade é garantida por índice
/// único em <c>Domain</c> na configuração EF.
/// </summary>
public sealed class TenantDomain
{
    private TenantDomain() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Domain { get; private set; } = string.Empty;
    public TenantDomainStatus Status { get; private set; } = TenantDomainStatus.PendingVerification;
    public string VerificationToken { get; private set; } = string.Empty;
    public bool IsPrimary { get; private set; }
    public DateTimeOffset? VerifiedAt { get; private set; }
    public TenantDomainCertStatus CertStatus { get; private set; } = TenantDomainCertStatus.None;
    public DateTimeOffset? CertIssuedAt { get; private set; }
    public DateTimeOffset? CertExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public static TenantDomain Register(Guid tenantId, string domain, string verificationToken)
    {
        if (string.IsNullOrWhiteSpace(domain))
            throw new DomainException("O domínio é obrigatório.");

        if (string.IsNullOrWhiteSpace(verificationToken))
            throw new DomainException("O token de verificação é obrigatório.");

        var now = DateTimeOffset.UtcNow;

        return new TenantDomain
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            Domain = domain.Trim().ToLowerInvariant(),
            Status = TenantDomainStatus.PendingVerification,
            VerificationToken = verificationToken,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>Nome do registro TXT que o cliente precisa criar (US-143 §10, instruções de DNS).</summary>
    public string VerificationRecordName => $"_verify.{Domain}";

    public void MarkVerified(DateTimeOffset verifiedAt)
    {
        Status = TenantDomainStatus.Active;
        VerifiedAt = verifiedAt;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkVerificationFailed()
    {
        Status = TenantDomainStatus.PendingVerification;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void IssueCertificate(DateTimeOffset issuedAt, DateTimeOffset expiresAt)
    {
        CertStatus = TenantDomainCertStatus.Issued;
        CertIssuedAt = issuedAt;
        CertExpiresAt = expiresAt;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkCertificateFailed()
    {
        CertStatus = TenantDomainCertStatus.Failed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsPrimary()
    {
        IsPrimary = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ClearPrimary()
    {
        IsPrimary = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Limiar de alerta de renovação (US-143 §11) — 15 dias antes do vencimento.</summary>
    public bool IsCertificateExpiringSoon(DateTimeOffset now) =>
        CertStatus == TenantDomainCertStatus.Issued && CertExpiresAt is not null && CertExpiresAt.Value <= now.AddDays(15);

    public void SoftDelete()
    {
        DeletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public enum TenantDomainStatus
{
    PendingVerification = 0,
    Active = 1
}

public enum TenantDomainCertStatus
{
    None = 0,
    Issued = 1,
    Failed = 2
}
