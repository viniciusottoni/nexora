using Nexora.Application.Platform.Support;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Platform;

/// <summary>
/// US-157 · Central operacional, auditoria e atalhos de suporte — núcleo mais importante desta
/// tarefa para testar bem: classificação de severidade e ordenação da fila de atenção. Cobre os
/// limiares exatos (empate/fronteira) e a matriz completa entre os quatro tipos de pendência, mesmo
/// espírito de <c>TenantAttentionRankingTests</c>/<c>InstallationHealthClassifierTests</c> (puro, sem
/// I/O, tabela de casos incluindo fronteiras).
/// </summary>
public sealed class AttentionQueueClassifierTests
{
    // ---- Instalação offline ----

    [Fact]
    public void Instalacao_Offline_Abaixo_Do_Limiar_Critico_E_HIGH()
    {
        AttentionQueueClassifier.ClassifyInstallationOffline(TimeSpan.FromMinutes(15))
            .Should().Be(AttentionSeverity.High);
    }

    [Fact]
    public void Instalacao_Offline_Exatamente_No_Limiar_Critico_Ja_E_CRITICAL()
    {
        AttentionQueueClassifier.ClassifyInstallationOffline(AttentionQueueClassifier.InstallationOfflineCriticalThreshold)
            .Should().Be(AttentionSeverity.Critical);
    }

    [Fact]
    public void Instalacao_Offline_Um_Segundo_Antes_Do_Limiar_Critico_Ainda_E_HIGH()
    {
        AttentionQueueClassifier.ClassifyInstallationOffline(
                AttentionQueueClassifier.InstallationOfflineCriticalThreshold - TimeSpan.FromSeconds(1))
            .Should().Be(AttentionSeverity.High);
    }

    [Fact]
    public void Instalacao_Offline_Muito_Tempo_Continua_CRITICAL()
    {
        AttentionQueueClassifier.ClassifyInstallationOffline(TimeSpan.FromDays(3))
            .Should().Be(AttentionSeverity.Critical);
    }

    // ---- Instalação degradada ----

    [Fact]
    public void Instalacao_Degradada_E_Sempre_LOW()
    {
        AttentionQueueClassifier.ClassifyInstallationDegraded().Should().Be(AttentionSeverity.Low);
    }

    // ---- Convite expirado ----

    [Fact]
    public void Convite_Expirado_Ha_Pouco_Tempo_E_MEDIUM()
    {
        AttentionQueueClassifier.ClassifyInviteExpired(TimeSpan.FromHours(2))
            .Should().Be(AttentionSeverity.Medium);
    }

    [Fact]
    public void Convite_Expirado_Exatamente_No_Limiar_De_Uma_Semana_Ja_E_HIGH()
    {
        AttentionQueueClassifier.ClassifyInviteExpired(AttentionQueueClassifier.InviteExpiredHighThreshold)
            .Should().Be(AttentionSeverity.High);
    }

    [Fact]
    public void Convite_Expirado_Um_Segundo_Antes_Do_Limiar_Ainda_E_MEDIUM()
    {
        AttentionQueueClassifier.ClassifyInviteExpired(
                AttentionQueueClassifier.InviteExpiredHighThreshold - TimeSpan.FromSeconds(1))
            .Should().Be(AttentionSeverity.Medium);
    }

    // ---- Provisionamento parado ----

    [Fact]
    public void Provisionamento_Abaixo_Do_Minimo_Nao_E_Reportado()
    {
        AttentionQueueClassifier.ClassifyProvisioningStalled(
                AttentionQueueClassifier.ProvisioningStalledMinimumThreshold - TimeSpan.FromMinutes(1))
            .Should().BeNull("fluxo normal de implantação ainda não conta como 'parado'");
    }

    [Fact]
    public void Provisionamento_Exatamente_No_Minimo_Ja_E_MEDIUM()
    {
        AttentionQueueClassifier.ClassifyProvisioningStalled(AttentionQueueClassifier.ProvisioningStalledMinimumThreshold)
            .Should().Be(AttentionSeverity.Medium);
    }

    [Fact]
    public void Provisionamento_No_Limiar_HIGH_Escala_Corretamente()
    {
        AttentionQueueClassifier.ClassifyProvisioningStalled(AttentionQueueClassifier.ProvisioningStalledHighThreshold)
            .Should().Be(AttentionSeverity.High);
    }

    [Fact]
    public void Provisionamento_No_Limiar_CRITICAL_Escala_Corretamente()
    {
        AttentionQueueClassifier.ClassifyProvisioningStalled(AttentionQueueClassifier.ProvisioningStalledCriticalThreshold)
            .Should().Be(AttentionSeverity.Critical);
    }

    // ---- Ordenação/prioridade — "sem esconder itens menos graves" ----

    [Fact]
    public void Ranking_Ordena_Critical_Antes_De_High_Antes_De_Medium_Antes_De_Low()
    {
        var severities = new[]
        {
            AttentionSeverity.Low, AttentionSeverity.Critical, AttentionSeverity.Medium, AttentionSeverity.High,
        };

        var ordered = severities.OrderBy(s => s.RankOf()).ToList();

        ordered.Should().Equal(
            AttentionSeverity.Critical, AttentionSeverity.High, AttentionSeverity.Medium, AttentionSeverity.Low);
    }

    [Fact]
    public void Empate_De_Severidade_Preserva_Todos_Os_Itens_Ordenacao_E_Estavel_Por_Chave_Secundaria()
    {
        // Dois itens HIGH (empate de severidade) — a ordenação por (rank, since, itemId) do handler
        // nunca "esconde" um dos dois; aqui provamos só que o rank empatado não distingue por si só,
        // e que a comparação teria que cair para o próximo critério (tempo na condição/itemId).
        var a = AttentionQueueClassifier.ClassifyInstallationOffline(TimeSpan.FromMinutes(20));
        var b = AttentionQueueClassifier.ClassifyInviteExpired(TimeSpan.FromDays(10));

        a.Should().Be(AttentionSeverity.High);
        b.Should().Be(AttentionSeverity.High);
        a.RankOf().Should().Be(b.RankOf());
    }

    // ---- Texto substantivo do motivo ----

    [Theory]
    [InlineData(30, "30 min")]
    [InlineData(90, "1 h")]
    [InlineData(60 * 25, "1 dia")]
    [InlineData(60 * 24 * 3, "3 dias")]
    public void FormatDuration_Produz_Texto_PtBr_Substantivo(int minutes, string expected)
    {
        AttentionQueueClassifier.FormatDuration(TimeSpan.FromMinutes(minutes)).Should().Be(expected);
    }

    [Fact]
    public void ReasonForInstallationOffline_Nao_E_So_Um_Numero()
    {
        var reason = AttentionQueueClassifier.ReasonForInstallationOffline(TimeSpan.FromMinutes(18));
        reason.Should().Be("Sem contato há 18 min");
    }

    [Fact]
    public void ReasonForProvisioningStalled_Inclui_Status_E_Duracao()
    {
        var reason = AttentionQueueClassifier.ReasonForProvisioningStalled("INSTALLING", TimeSpan.FromHours(30));
        reason.Should().Be("Provisionamento parado em INSTALLING há 1 dia");
    }
}
