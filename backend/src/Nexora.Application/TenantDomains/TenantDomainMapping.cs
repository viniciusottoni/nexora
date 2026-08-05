using Nexora.Contracts.Platform;
using Nexora.Domain.Platform;

namespace Nexora.Application.TenantDomains;

/// <summary>Rótulos de fio (contrato JSON) para os enums de <see cref="TenantDomain"/> — caixa alta, como o resto do contrato (US-143 §7).</summary>
internal static class TenantDomainStatusMapping
{
    public static string ToWireLabel(this TenantDomainStatus status) => status switch
    {
        TenantDomainStatus.Active => "ACTIVE",
        _ => "PENDING_VERIFICATION"
    };

    public static string ToWireLabel(this TenantDomainCertStatus status) => status switch
    {
        TenantDomainCertStatus.Issued => "ISSUED",
        TenantDomainCertStatus.Failed => "FAILED",
        _ => "NONE"
    };
}

internal static class TenantDomainMapping
{
    public static TenantDomainResponse ToResponse(this TenantDomain domain) => new(
        domain.Id,
        domain.TenantId,
        domain.Domain,
        domain.Status.ToWireLabel(),
        domain.IsPrimary,
        domain.VerifiedAt,
        domain.CertStatus.ToWireLabel(),
        domain.CertIssuedAt,
        domain.CertExpiresAt,
        domain.CreatedAt);
}
