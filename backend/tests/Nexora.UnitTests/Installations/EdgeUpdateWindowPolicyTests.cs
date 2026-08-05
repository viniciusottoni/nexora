using Nexora.Application.Installations.Support;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Installations;

/// <summary>US-146 §4 "Atualização na janela configurada" — leitura de <c>tenant_config.maintenance</c> e decisão pura de janela.</summary>
public sealed class EdgeUpdateWindowPolicyTests
{
    [Fact]
    public void ResolveWindow_Sem_Maintenance_Usa_Default_4h_6h()
    {
        var (start, end) = EdgeUpdateWindowPolicy.ResolveWindow(null);

        start.Should().Be(4);
        end.Should().Be(6);
    }

    [Fact]
    public void ResolveWindow_Le_Janela_Configurada()
    {
        var (start, end) = EdgeUpdateWindowPolicy.ResolveWindow("""{"updateWindowStartHour":22,"updateWindowEndHour":2}""");

        start.Should().Be(22);
        end.Should().Be(2);
    }

    [Fact]
    public void ResolveWindow_Malformado_Cai_No_Default_Seguro()
    {
        var (start, end) = EdgeUpdateWindowPolicy.ResolveWindow("{ not json");

        start.Should().Be(EdgeUpdateWindowPolicy.DefaultStartHourUtc);
        end.Should().Be(EdgeUpdateWindowPolicy.DefaultEndHourUtc);
    }

    [Theory]
    [InlineData(3, false)]
    [InlineData(4, true)]
    [InlineData(5, true)]
    [InlineData(6, false)]
    [InlineData(7, false)]
    public void IsWithinWindow_Janela_Normal_4h_6h(int hour, bool expected)
    {
        var now = new DateTimeOffset(2026, 8, 4, hour, 0, 0, TimeSpan.Zero);

        EdgeUpdateWindowPolicy.IsWithinWindow(now, 4, 6).Should().Be(expected);
    }

    [Theory]
    [InlineData(21, false)]
    [InlineData(22, true)]
    [InlineData(23, true)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public void IsWithinWindow_Janela_Atravessando_Meia_Noite_22h_2h(int hour, bool expected)
    {
        var now = new DateTimeOffset(2026, 8, 4, hour, 0, 0, TimeSpan.Zero);

        EdgeUpdateWindowPolicy.IsWithinWindow(now, 22, 2).Should().Be(expected);
    }
}
