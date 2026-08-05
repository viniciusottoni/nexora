using Nexora.Domain.Common;
using Nexora.Domain.Platform;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Platform;

/// <summary>
/// US-146 — transições puras de <see cref="EdgeInstallation.ScheduleUpdate"/>/
/// <see cref="EdgeInstallation.RecordUpdateResult"/>, sem banco/I/O (o que acontece ANTES/DEPOIS —
/// avaliação de elegibilidade de release, passos de backup/download/migration/health check — é
/// responsabilidade da Application, coberta por testes de integração).
/// </summary>
public sealed class EdgeInstallationUpdateCycleTests
{
    private static EdgeInstallation CreateInstallation() =>
        EdgeInstallation.CreateInstalled(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Servidor local — Matriz",
            publicKey: "pk-teste", version: "1.4.0");

    [Fact]
    public void ScheduleUpdate_Sem_Versao_Lanca_DomainException()
    {
        var installation = CreateInstallation();

        var act = () => installation.ScheduleUpdate("   ");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ScheduleUpdate_Define_TargetVersion()
    {
        var installation = CreateInstallation();

        installation.ScheduleUpdate("1.5.0");

        installation.TargetVersion.Should().Be("1.5.0");
    }

    [Fact]
    public void RecordUpdateResult_Succeeded_Atualiza_Version_E_Limpa_TargetVersion()
    {
        var installation = CreateInstallation();
        installation.ScheduleUpdate("1.5.0");

        installation.RecordUpdateResult(EdgeUpdateStatus.Succeeded, DateTimeOffset.UtcNow, "1.5.0");

        installation.Version.Should().Be("1.5.0");
        installation.TargetVersion.Should().BeNull();
        installation.LastUpdateStatus.Should().Be(nameof(EdgeUpdateStatus.Succeeded));
    }

    [Fact]
    public void RecordUpdateResult_Failed_Preserva_Version_Anterior_E_TargetVersion()
    {
        var installation = CreateInstallation();
        installation.ScheduleUpdate("1.5.0");

        installation.RecordUpdateResult(EdgeUpdateStatus.Failed, DateTimeOffset.UtcNow);

        installation.Version.Should().Be("1.4.0", "atualização que falhou não pode ter alterado a versão instalada");
        installation.TargetVersion.Should().Be("1.5.0", "a versão-alvo continua pendente até um novo ciclo tentar de novo ou a release mudar");
        installation.LastUpdateStatus.Should().Be(nameof(EdgeUpdateStatus.Failed));
    }

    [Fact]
    public void RecordUpdateResult_RolledBack_Preserva_Version_Anterior()
    {
        var installation = CreateInstallation();
        installation.ScheduleUpdate("1.5.0");

        installation.RecordUpdateResult(EdgeUpdateStatus.RolledBack, DateTimeOffset.UtcNow);

        installation.Version.Should().Be("1.4.0", "rollback automático garante que a instalação continua na versão anterior (US-146 §4)");
        installation.LastUpdateStatus.Should().Be(nameof(EdgeUpdateStatus.RolledBack));
    }

    [Fact]
    public void RecordUpdateResult_Deferred_Preserva_TargetVersion_Para_Tentar_Na_Proxima_Janela()
    {
        var installation = CreateInstallation();
        installation.ScheduleUpdate("1.5.0");

        installation.RecordUpdateResult(EdgeUpdateStatus.Deferred, DateTimeOffset.UtcNow);

        installation.TargetVersion.Should().Be("1.5.0");
        installation.LastUpdateStatus.Should().Be(nameof(EdgeUpdateStatus.Deferred));
    }

    [Fact]
    public void RecordUpdateResult_Sempre_Atualiza_LastUpdateAt()
    {
        var installation = CreateInstallation();
        var at = new DateTimeOffset(2026, 8, 4, 5, 0, 0, TimeSpan.Zero);

        installation.RecordUpdateResult(EdgeUpdateStatus.Failed, at);

        installation.LastUpdateAt.Should().Be(at);
    }
}
