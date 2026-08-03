using Nexora.Application.Orders.Support;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Operation;

/// <summary>
/// ADR-034 (Relógio, sequência e tolerância a desvio) — cobre a regra "diferença ≤ 2 min → aceita
/// o do cliente; diferença > 2 min → usa o do edge + registra o desvio" isoladamente de qualquer
/// handler/HTTP (mesmo espírito de <c>BusinessDayPolicyTests</c>).
/// </summary>
public sealed class ClockSkewPolicyTests
{
    private static readonly DateTimeOffset EdgeNow = new(2026, 8, 3, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Resolve_Sem_Header_Usa_O_Relogio_Do_Edge_Sem_Marcar_Suspeito()
    {
        var resolution = ClockSkewPolicy.Resolve(deviceOccurredAt: null, EdgeNow);

        resolution.OccurredAt.Should().Be(EdgeNow);
        resolution.ClockSuspect.Should().BeFalse();
        resolution.Deviation.Should().BeNull();
    }

    [Fact]
    public void Resolve_Com_Desvio_Dentro_Da_Tolerancia_Aceita_O_Horario_Do_Dispositivo()
    {
        var deviceAt = EdgeNow.AddMinutes(1).AddSeconds(59); // 1min59s adiantado — dentro dos 2 min

        var resolution = ClockSkewPolicy.Resolve(deviceAt, EdgeNow);

        resolution.OccurredAt.Should().Be(deviceAt);
        resolution.ClockSuspect.Should().BeFalse();
    }

    [Fact]
    public void Resolve_Exatamente_Na_Tolerancia_De_2_Minutos_Ainda_Aceita_O_Dispositivo()
    {
        var deviceAt = EdgeNow.AddMinutes(2);

        var resolution = ClockSkewPolicy.Resolve(deviceAt, EdgeNow);

        resolution.OccurredAt.Should().Be(deviceAt);
        resolution.ClockSuspect.Should().BeFalse();
    }

    /// <summary>Cenário Gherkin "Relógio do dispositivo adiantado" (US-032 §4): 4 min à frente do servidor.</summary>
    [Fact]
    public void Resolve_Dispositivo_Adiantado_Alem_Da_Tolerancia_Usa_O_Relogio_Do_Edge_E_Marca_Suspeito()
    {
        var deviceAt = EdgeNow.AddMinutes(4);

        var resolution = ClockSkewPolicy.Resolve(deviceAt, EdgeNow);

        resolution.OccurredAt.Should().Be(EdgeNow);
        resolution.ClockSuspect.Should().BeTrue();
        resolution.Deviation.Should().Be(TimeSpan.FromMinutes(4));
    }

    [Fact]
    public void Resolve_Dispositivo_Atrasado_Alem_Da_Tolerancia_Usa_O_Relogio_Do_Edge_E_Marca_Suspeito()
    {
        var deviceAt = EdgeNow.AddMinutes(-10);

        var resolution = ClockSkewPolicy.Resolve(deviceAt, EdgeNow);

        resolution.OccurredAt.Should().Be(EdgeNow);
        resolution.ClockSuspect.Should().BeTrue();
        resolution.Deviation.Should().Be(TimeSpan.FromMinutes(-10));
    }
}
