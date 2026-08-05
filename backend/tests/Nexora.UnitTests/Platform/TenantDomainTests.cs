using Nexora.Domain.Common;
using Nexora.Domain.Platform;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Platform;

/// <summary>
/// US-143 §12 "Unitário" — transições de estado de <see cref="TenantDomain"/> e o limiar de
/// alerta de renovação (<see cref="TenantDomain.IsCertificateExpiringSoon"/>, 15 dias), sem banco.
/// O fluxo completo (registro -> verificação -> emissão de certificado, contra Postgres real) é
/// coberto em <c>Nexora.IntegrationTests.TenantDomainIntegrationTests</c>.
/// </summary>
public sealed class TenantDomainTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static TenantDomain CreateDomain(string domain = "cardapio.donabetinha.com.br") =>
        TenantDomain.Register(TenantId, domain, "verification-token-123");

    [Fact]
    public void Register_Normaliza_O_Dominio_Para_Minusculo_E_Comeca_Pendente()
    {
        var domain = TenantDomain.Register(TenantId, "  Cardapio.DonaBetinha.COM.BR  ", "token");

        domain.Domain.Should().Be("cardapio.donabetinha.com.br");
        domain.Status.Should().Be(TenantDomainStatus.PendingVerification);
        domain.CertStatus.Should().Be(TenantDomainCertStatus.None);
        domain.IsPrimary.Should().BeFalse();
    }

    [Fact]
    public void Register_Sem_Dominio_Lanca_DomainException()
    {
        var act = () => TenantDomain.Register(TenantId, "  ", "token");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Register_Sem_Token_De_Verificacao_Lanca_DomainException()
    {
        var act = () => TenantDomain.Register(TenantId, "cardapio.donabetinha.com.br", "  ");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void VerificationRecordName_Prefixa_Com_Underline_Verify()
    {
        var domain = CreateDomain("cardapio.donabetinha.com.br");

        domain.VerificationRecordName.Should().Be("_verify.cardapio.donabetinha.com.br");
    }

    [Fact]
    public void MarkVerified_Ativa_E_Registra_O_Instante()
    {
        var domain = CreateDomain();
        var verifiedAt = DateTimeOffset.UtcNow;

        domain.MarkVerified(verifiedAt);

        domain.Status.Should().Be(TenantDomainStatus.Active);
        domain.VerifiedAt.Should().Be(verifiedAt);
    }

    [Fact]
    public void MarkVerificationFailed_Mantem_Pendente()
    {
        var domain = CreateDomain();

        domain.MarkVerificationFailed();

        domain.Status.Should().Be(TenantDomainStatus.PendingVerification);
        domain.VerifiedAt.Should().BeNull();
    }

    [Fact]
    public void IssueCertificate_Marca_Emitido_Com_Datas()
    {
        var domain = CreateDomain();
        var issuedAt = DateTimeOffset.UtcNow;
        var expiresAt = issuedAt.AddDays(90);

        domain.IssueCertificate(issuedAt, expiresAt);

        domain.CertStatus.Should().Be(TenantDomainCertStatus.Issued);
        domain.CertIssuedAt.Should().Be(issuedAt);
        domain.CertExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public void MarkCertificateFailed_Marca_Falha()
    {
        var domain = CreateDomain();

        domain.MarkCertificateFailed();

        domain.CertStatus.Should().Be(TenantDomainCertStatus.Failed);
    }

    [Fact]
    public void MarkAsPrimary_E_ClearPrimary_Alternam_A_Flag()
    {
        var domain = CreateDomain();

        domain.MarkAsPrimary();
        domain.IsPrimary.Should().BeTrue();

        domain.ClearPrimary();
        domain.IsPrimary.Should().BeFalse();
    }

    [Fact]
    public void SoftDelete_Marca_DeletedAt()
    {
        var domain = CreateDomain();

        domain.SoftDelete();

        domain.DeletedAt.Should().NotBeNull();
    }

    // --- IsCertificateExpiringSoon: limiar de 15 dias (US-143 §11) ---

    [Fact]
    public void IsCertificateExpiringSoon_Sem_Certificado_Emitido_E_Falso()
    {
        var domain = CreateDomain();
        var now = DateTimeOffset.UtcNow;

        domain.IsCertificateExpiringSoon(now).Should().BeFalse();
    }

    [Fact]
    public void IsCertificateExpiringSoon_Com_Certificado_Com_Falha_E_Falso_Mesmo_Com_Data_Proxima()
    {
        var domain = CreateDomain();
        var now = DateTimeOffset.UtcNow;
        domain.IssueCertificate(now.AddDays(-75), now.AddDays(1));
        domain.MarkCertificateFailed(); // some CertExpiresAt continua setado do IssueCertificate anterior

        domain.IsCertificateExpiringSoon(now).Should().BeFalse();
    }

    [Fact]
    public void IsCertificateExpiringSoon_Exatamente_15_Dias_Antes_E_Verdadeiro_Limite_Inclusivo()
    {
        var domain = CreateDomain();
        var now = DateTimeOffset.UtcNow;
        domain.IssueCertificate(now.AddDays(-75), now.AddDays(15));

        domain.IsCertificateExpiringSoon(now).Should().BeTrue();
    }

    [Fact]
    public void IsCertificateExpiringSoon_16_Dias_Antes_E_Falso()
    {
        var domain = CreateDomain();
        var now = DateTimeOffset.UtcNow;
        domain.IssueCertificate(now.AddDays(-74), now.AddDays(16));

        domain.IsCertificateExpiringSoon(now).Should().BeFalse();
    }

    [Fact]
    public void IsCertificateExpiringSoon_Ja_Vencido_E_Verdadeiro()
    {
        var domain = CreateDomain();
        var now = DateTimeOffset.UtcNow;
        domain.IssueCertificate(now.AddDays(-91), now.AddDays(-1));

        domain.IsCertificateExpiringSoon(now).Should().BeTrue();
    }
}
