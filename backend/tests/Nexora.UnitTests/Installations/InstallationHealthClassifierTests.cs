using Nexora.Application.Installations.Support;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Installations;

/// <summary>
/// US-140 §4, cenários "Instalação fora do ar" e "Instalação degradada" — classificação pura,
/// sem tocar banco (a decisão de abrir/fechar <c>InstallationIncident</c> é testada por
/// integração em <c>EvaluateInstallationHealthIntegrationTests</c>).
/// </summary>
public sealed class InstallationHealthClassifierTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void LastSeenAt_Nulo_E_Sempre_Down()
    {
        InstallationHealthClassifier.Classify(Now, null).Should().Be(InstallationHealthStatus.Down);
    }

    [Fact]
    public void Contato_Recente_E_Ok()
    {
        var lastSeenAt = Now - TimeSpan.FromMinutes(1);

        InstallationHealthClassifier.Classify(Now, lastSeenAt).Should().Be(InstallationHealthStatus.Ok);
    }

    [Fact]
    public void Exatamente_No_Limiar_De_Degradacao_Ja_E_Degraded()
    {
        var lastSeenAt = Now - InstallationHealthClassifier.DegradedThreshold;

        InstallationHealthClassifier.Classify(Now, lastSeenAt).Should().Be(InstallationHealthStatus.Degraded);
    }

    [Fact]
    public void Um_Segundo_Antes_Do_Limiar_De_Degradacao_Ainda_E_Ok()
    {
        var lastSeenAt = Now - InstallationHealthClassifier.DegradedThreshold + TimeSpan.FromSeconds(1);

        InstallationHealthClassifier.Classify(Now, lastSeenAt).Should().Be(InstallationHealthStatus.Ok);
    }

    [Fact]
    public void Entre_Os_Dois_Limiares_E_Degraded()
    {
        var lastSeenAt = Now - TimeSpan.FromMinutes(10);

        InstallationHealthClassifier.Classify(Now, lastSeenAt).Should().Be(InstallationHealthStatus.Degraded);
    }

    [Fact]
    public void Exatamente_No_Limiar_De_Queda_Ja_E_Down()
    {
        var lastSeenAt = Now - InstallationHealthClassifier.DownThreshold;

        InstallationHealthClassifier.Classify(Now, lastSeenAt).Should().Be(InstallationHealthStatus.Down);
    }

    [Fact]
    public void Um_Segundo_Antes_Do_Limiar_De_Queda_Ainda_E_Degraded()
    {
        var lastSeenAt = Now - InstallationHealthClassifier.DownThreshold + TimeSpan.FromSeconds(1);

        InstallationHealthClassifier.Classify(Now, lastSeenAt).Should().Be(InstallationHealthStatus.Degraded);
    }

    [Fact]
    public void Muito_Alem_Do_Limiar_De_Queda_E_Down()
    {
        var lastSeenAt = Now - TimeSpan.FromHours(6);

        InstallationHealthClassifier.Classify(Now, lastSeenAt).Should().Be(InstallationHealthStatus.Down);
    }

    [Theory]
    [InlineData(InstallationHealthStatus.Ok, "OK")]
    [InlineData(InstallationHealthStatus.Degraded, "DEGRADED")]
    [InlineData(InstallationHealthStatus.Down, "DOWN")]
    public void ToWireLabel_Usa_O_Rotulo_Do_Contrato_De_API(InstallationHealthStatus status, string expected)
    {
        status.ToWireLabel().Should().Be(expected);
    }
}
