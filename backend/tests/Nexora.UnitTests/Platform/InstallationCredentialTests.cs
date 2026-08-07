using Nexora.Domain.Common;
using Nexora.Domain.Platform;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Platform;

/// <summary>
/// US-156 · Recuperação do provisionamento e token de instalação — regras de negócio puras (sem
/// banco) de <see cref="InstallationCredential"/>: validade, estado ativo/expirado/consumido/
/// revogado. A rotação/revogação atômica de verdade (contra Postgres real, com concorrência) é
/// coberta em <c>Nexora.IntegrationTests.ReissueInstallationTokenIntegrationTests</c>.
/// </summary>
public sealed class InstallationCredentialTests
{
    private static InstallationCredential CreateCredential(DateTimeOffset? expiresAt = null) =>
        InstallationCredential.Issue(
            tenantId: Guid.NewGuid(),
            installationId: Guid.NewGuid(),
            tokenHash: "hash-1",
            expiresAt: expiresAt ?? DateTimeOffset.UtcNow.AddHours(24),
            reason: "Comando original não foi exibido",
            actorId: Guid.NewGuid());

    [Fact]
    public void Issue_Define_Campos_E_Nasce_Ativa()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddHours(24);
        var credential = CreateCredential(expiresAt);

        credential.Id.Should().NotBeEmpty();
        credential.TokenHash.Should().Be("hash-1");
        credential.ExpiresAt.Should().Be(expiresAt);
        credential.ConsumedAt.Should().BeNull();
        credential.RevokedAt.Should().BeNull();
        credential.IsActive(DateTimeOffset.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void Issue_Sem_Hash_Lanca_DomainException()
    {
        var act = () => InstallationCredential.Issue(
            Guid.NewGuid(), Guid.NewGuid(), tokenHash: "", DateTimeOffset.UtcNow.AddHours(1), "motivo", null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Issue_Sem_Motivo_Lanca_DomainException()
    {
        var act = () => InstallationCredential.Issue(
            Guid.NewGuid(), Guid.NewGuid(), "hash-1", DateTimeOffset.UtcNow.AddHours(1), reason: "", null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void IsExpired_Apos_O_Prazo_E_Verdadeiro()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);
        var credential = CreateCredential(expiresAt);

        credential.IsExpired(expiresAt.AddSeconds(1)).Should().BeTrue();
        credential.IsActive(expiresAt.AddSeconds(1)).Should().BeFalse();
    }

    [Fact]
    public void MarkConsumed_Torna_Inativa_E_E_Idempotente()
    {
        var credential = CreateCredential();
        var at = DateTimeOffset.UtcNow;

        credential.MarkConsumed(at);

        credential.ConsumedAt.Should().Be(at);
        credential.IsActive(DateTimeOffset.UtcNow).Should().BeFalse();

        // Idempotente: uma segunda chamada não sobrescreve o instante original.
        credential.MarkConsumed(at.AddMinutes(5));
        credential.ConsumedAt.Should().Be(at);
    }

    [Fact]
    public void Revoke_Torna_Inativa_E_E_Idempotente()
    {
        var credential = CreateCredential();
        var at = DateTimeOffset.UtcNow;

        credential.Revoke(at);

        credential.RevokedAt.Should().Be(at);
        credential.IsActive(DateTimeOffset.UtcNow).Should().BeFalse();

        credential.Revoke(at.AddMinutes(5));
        credential.RevokedAt.Should().Be(at);
    }

    [Fact]
    public void Revoke_Nao_Sobrescreve_Reason_De_Emissao()
    {
        var credential = CreateCredential();

        credential.Revoke(DateTimeOffset.UtcNow);

        // O motivo da REVOGAÇÃO vive no AuditLog correspondente, não nesta entidade — Reason aqui
        // continua sendo o motivo da EMISSÃO (ver docstring da classe).
        credential.Reason.Should().Be("Comando original não foi exibido");
    }
}
