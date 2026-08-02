using Nexora.Domain.Platform;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Platform;

/// <summary>
/// US-002, doc. 12 §12 ("Unitário: geração de slug, validação de unicidade e expiração de
/// token") — a parte de expiração/uso único do token de instalação, no nível de domínio (sem
/// banco). A unicidade de fato (segundo uso recusado) é coberta contra Postgres real em
/// <c>Nexora.IntegrationTests.ProvisionTenantIntegrationTests</c>, porque depende do handler
/// completo (<c>ConsumeInstallationTokenCommandHandler</c>); aqui só a regra de negócio pura de
/// <see cref="EdgeInstallation"/>.
/// </summary>
public sealed class EdgeInstallationTokenTests
{
    private static EdgeInstallation CreateInstallation() =>
        EdgeInstallation.Create(Guid.NewGuid(), Guid.NewGuid(), "Servidor local — Matriz");

    [Fact]
    public void IssueInstallToken_Define_Hash_E_Expiracao_E_Limpa_Consumo_Anterior()
    {
        var installation = CreateInstallation();
        var expiresAt = DateTimeOffset.UtcNow.AddHours(24);

        installation.IssueInstallToken("hash-1", expiresAt);

        installation.InstallTokenHash.Should().Be("hash-1");
        installation.TokenExpiresAt.Should().Be(expiresAt);
        installation.IsTokenConsumed.Should().BeFalse();
    }

    [Fact]
    public void IsTokenExpired_Antes_Do_Prazo_E_Falso()
    {
        var installation = CreateInstallation();
        installation.IssueInstallToken("hash-1", DateTimeOffset.UtcNow.AddHours(24));

        installation.IsTokenExpired(DateTimeOffset.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void IsTokenExpired_Apos_O_Prazo_E_Verdadeiro()
    {
        var installation = CreateInstallation();
        var expiresAt = DateTimeOffset.UtcNow.AddHours(24);
        installation.IssueInstallToken("hash-1", expiresAt);

        installation.IsTokenExpired(expiresAt.AddSeconds(1)).Should().BeTrue();
    }

    [Fact]
    public void IsTokenExpired_Sem_Token_Emitido_E_Verdadeiro()
    {
        var installation = CreateInstallation();

        installation.IsTokenExpired(DateTimeOffset.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void ReserveInstallToken_Marca_Como_Consumido_Uma_Unica_Vez()
    {
        var installation = CreateInstallation();
        installation.IssueInstallToken("hash-1", DateTimeOffset.UtcNow.AddHours(24));

        installation.IsTokenConsumed.Should().BeFalse();

        installation.ReserveInstallToken();

        installation.IsTokenConsumed.Should().BeTrue();
        installation.TokenConsumedAt.Should().NotBeNull();
    }
}
