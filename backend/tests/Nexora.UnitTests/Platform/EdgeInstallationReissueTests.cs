using Nexora.Domain.Platform;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Platform;

/// <summary>
/// US-156 · Recuperação do provisionamento e token de instalação — extensões NOVAS de
/// <see cref="EdgeInstallation"/> (<see cref="EdgeInstallation.CanReissueToken"/>/
/// <see cref="EdgeInstallation.InvalidateInstallToken"/>). Arquivo próprio, separado de
/// <c>EdgeInstallationTokenTests.cs</c> (US-002, não editado por esta tarefa) para não colidir com
/// o teste existente do protocolo de emissão/consumo original.
/// </summary>
public sealed class EdgeInstallationReissueTests
{
    private static EdgeInstallation CreateInstallation() =>
        EdgeInstallation.Create(Guid.NewGuid(), Guid.NewGuid(), "Servidor local — Matriz");

    [Fact]
    public void CanReissueToken_E_Verdadeiro_Antes_Do_Pareamento()
    {
        var installation = CreateInstallation();
        installation.IssueInstallToken("hash-1", DateTimeOffset.UtcNow.AddHours(24));

        installation.CanReissueToken.Should().BeTrue();
    }

    [Fact]
    public void CanReissueToken_E_Falso_Apos_CompleteRegistration()
    {
        var installation = CreateInstallation();
        installation.IssueInstallToken("hash-1", DateTimeOffset.UtcNow.AddHours(24));

        installation.CompleteRegistration(publicKey: "chave-publica-ed25519", version: "1.0.0", label: null);

        installation.IsInstalled.Should().BeTrue();
        installation.CanReissueToken.Should().BeFalse();
    }

    [Fact]
    public void CanReissueToken_E_Falso_Apos_MarkInstalled()
    {
        var installation = CreateInstallation();

        installation.MarkInstalled("chave-publica-ed25519");

        installation.CanReissueToken.Should().BeFalse();
    }

    [Fact]
    public void IssueInstallToken_Reemitido_Invalida_O_Hash_Anterior_Imediatamente()
    {
        var installation = CreateInstallation();
        installation.IssueInstallToken("hash-original", DateTimeOffset.UtcNow.AddHours(24));

        installation.IssueInstallToken("hash-reemitido", DateTimeOffset.UtcNow.AddHours(24));

        // Gherkin "Reemissão segura": só o hash mais recente é reconhecido — o campo é único
        // (EdgeInstallation guarda só "o corrente"), então o anterior já não bate com nada.
        installation.InstallTokenHash.Should().Be("hash-reemitido");
        installation.InstallTokenHash.Should().NotBe("hash-original");
        installation.IsTokenConsumed.Should().BeFalse();
    }

    [Fact]
    public void InvalidateInstallToken_Marca_Como_Indisponivel_Sem_Instalar()
    {
        var installation = CreateInstallation();
        installation.IssueInstallToken("hash-1", DateTimeOffset.UtcNow.AddHours(24));

        installation.InvalidateInstallToken();

        installation.IsTokenConsumed.Should().BeTrue();
        installation.IsInstalled.Should().BeFalse();
    }

    [Fact]
    public void InvalidateInstallToken_E_Idempotente()
    {
        var installation = CreateInstallation();
        installation.IssueInstallToken("hash-1", DateTimeOffset.UtcNow.AddHours(24));

        installation.InvalidateInstallToken();
        var firstConsumedAt = installation.TokenConsumedAt;

        installation.InvalidateInstallToken();

        installation.TokenConsumedAt.Should().Be(firstConsumedAt);
    }
}
