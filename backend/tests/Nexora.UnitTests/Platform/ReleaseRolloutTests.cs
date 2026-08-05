using Nexora.Domain.Common;
using Nexora.Domain.Platform;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Platform;

/// <summary>
/// US-146 §4 "Liberação gradual" — <see cref="Release.IsEligibleFor"/> (bucketing determinístico e
/// estável) e <see cref="Release.ExpandRollout"/> ("nunca reduz"), sem banco/I/O.
/// </summary>
public sealed class ReleaseRolloutTests
{
    [Fact]
    public void Publish_Com_Percentual_Fora_De_0_100_Lanca_DomainException()
    {
        var act1 = () => Release.Publish("1.5.0", -1, null, null);
        var act2 = () => Release.Publish("1.5.0", 101, null, null);

        act1.Should().Throw<DomainException>();
        act2.Should().Throw<DomainException>();
    }

    [Fact]
    public void Publish_Sem_Versao_Lanca_DomainException()
    {
        var act = () => Release.Publish("   ", 10, null, null);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void IsEligibleFor_Com_RolloutPercent_Zero_Nunca_E_Elegivel()
    {
        var release = Release.Publish("1.5.0", 0, null, null);

        for (var i = 0; i < 50; i++)
        {
            release.IsEligibleFor(Guid.NewGuid()).Should().BeFalse();
        }
    }

    [Fact]
    public void IsEligibleFor_Com_RolloutPercent_100_Sempre_E_Elegivel()
    {
        var release = Release.Publish("1.5.0", 100, null, null);

        for (var i = 0; i < 50; i++)
        {
            release.IsEligibleFor(Guid.NewGuid()).Should().BeTrue();
        }
    }

    [Fact]
    public void IsEligibleFor_E_Deterministico_Para_O_Mesmo_Par_Release_Instalacao()
    {
        var release = Release.Publish("1.5.0", 37, null, null);
        var installationId = Guid.NewGuid();

        var first = release.IsEligibleFor(installationId);
        var second = release.IsEligibleFor(installationId);
        var third = release.IsEligibleFor(installationId);

        second.Should().Be(first);
        third.Should().Be(first);
    }

    [Fact]
    public void IsEligibleFor_Distribui_Aproximadamente_O_Percentual_Configurado()
    {
        var release = Release.Publish("1.5.0", 30, null, null);

        var eligible = 0;
        const int sampleSize = 2000;

        for (var i = 0; i < sampleSize; i++)
        {
            if (release.IsEligibleFor(Guid.NewGuid()))
            {
                eligible++;
            }
        }

        var ratio = (double)eligible / sampleSize;

        // Bucketing por hash não é uma amostra perfeitamente uniforme — margem generosa (20%-40%
        // para um alvo de 30%) evita um teste flutuante (flaky) por variação estatística normal,
        // mantendo a garantia que importa: NEM zero, NEM todo mundo é elegível.
        ratio.Should().BeInRange(0.20, 0.40);
    }

    [Fact]
    public void ExpandRollout_Nunca_Reduz_O_Percentual_Ja_Liberado()
    {
        var release = Release.Publish("1.5.0", 50, null, null);

        release.ExpandRollout(20);

        release.RolloutPercent.Should().Be(50, "ExpandRollout nunca reduz — US-146 §3.1");
    }

    [Fact]
    public void ExpandRollout_Amplia_Quando_O_Novo_Percentual_E_Maior()
    {
        var release = Release.Publish("1.5.0", 10, null, null);

        release.ExpandRollout(50);

        release.RolloutPercent.Should().Be(50);
    }

    [Fact]
    public void ExpandRollout_Com_O_Mesmo_Percentual_E_Um_NoOp_Sem_Erro()
    {
        var release = Release.Publish("1.5.0", 50, null, null);

        release.ExpandRollout(50);

        release.RolloutPercent.Should().Be(50);
    }
}
