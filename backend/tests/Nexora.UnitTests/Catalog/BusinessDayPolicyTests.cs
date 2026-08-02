using Nexora.Application.Catalog.Availability;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Catalog;

/// <summary>
/// US-015 §3.1 ("Retorno automático no novo dia operacional") + ADR-018 — cobre a função pura de
/// virada de dia operacional, sem tocar banco/HTTP. É a peça central do retorno automático: dado
/// que testar "2 segundos de propagação" ou "virou meia-noite" de ponta a ponta não é determinístico
/// num teste automatizado, esta suíte garante que a REGRA em si (o cálculo de fronteira) está
/// correta — <c>RestoreProductsPastBusinessDayCommandHandler</c> só orquestra banco em cima dela.
/// </summary>
public sealed class BusinessDayPolicyTests
{
    [Fact]
    public void ResolveStartHourUtc_Sem_Configuracao_Usa_O_Default_De_5h()
    {
        BusinessDayPolicy.ResolveStartHourUtc(null).Should().Be(5);
        BusinessDayPolicy.ResolveStartHourUtc("").Should().Be(5);
        BusinessDayPolicy.ResolveStartHourUtc("{}").Should().Be(5);
    }

    [Fact]
    public void ResolveStartHourUtc_Le_A_Chave_Configurada_Pelo_Tenant()
    {
        BusinessDayPolicy.ResolveStartHourUtc("""{"businessDayStartHour": 4}""").Should().Be(4);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(24)]
    [InlineData(100)]
    public void ResolveStartHourUtc_Com_Hora_Fora_Do_Intervalo_Cai_No_Default(int invalidHour)
    {
        BusinessDayPolicy.ResolveStartHourUtc($$"""{"businessDayStartHour": {{invalidHour}}}""").Should().Be(5);
    }

    [Fact]
    public void ResolveStartHourUtc_Com_Json_Malformado_Cai_No_Default()
    {
        BusinessDayPolicy.ResolveStartHourUtc("{ nao é json").Should().Be(5);
    }

    [Fact]
    public void CurrentBusinessDayStart_Antes_Da_Virada_Retorna_A_Virada_De_Ontem()
    {
        // Virada às 5h; agora são 03:00 de 10/03 — o "dia de ontem" ainda não virou.
        var now = new DateTimeOffset(2026, 3, 10, 3, 0, 0, TimeSpan.Zero);

        var boundary = BusinessDayPolicy.CurrentBusinessDayStart(now, startHourUtc: 5);

        boundary.Should().Be(new DateTimeOffset(2026, 3, 9, 5, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void CurrentBusinessDayStart_Depois_Da_Virada_Retorna_A_Virada_De_Hoje()
    {
        var now = new DateTimeOffset(2026, 3, 10, 8, 0, 0, TimeSpan.Zero);

        var boundary = BusinessDayPolicy.CurrentBusinessDayStart(now, startHourUtc: 5);

        boundary.Should().Be(new DateTimeOffset(2026, 3, 10, 5, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void CurrentBusinessDayStart_Exatamente_Na_Hora_Da_Virada_Conta_Como_Ja_Virado()
    {
        var now = new DateTimeOffset(2026, 3, 10, 5, 0, 0, TimeSpan.Zero);

        var boundary = BusinessDayPolicy.CurrentBusinessDayStart(now, startHourUtc: 5);

        boundary.Should().Be(now);
    }

    [Fact]
    public void HasCrossedBusinessDayBoundary_Produto_Marcado_Indisponivel_Ontem_Ja_Virou()
    {
        var unavailableSince = new DateTimeOffset(2026, 3, 9, 20, 0, 0, TimeSpan.Zero); // 20h de ontem
        var now = new DateTimeOffset(2026, 3, 10, 8, 0, 0, TimeSpan.Zero); // 08h de hoje, depois da virada das 5h

        BusinessDayPolicy.HasCrossedBusinessDayBoundary(unavailableSince, now, startHourUtc: 5).Should().BeTrue();
    }

    [Fact]
    public void HasCrossedBusinessDayBoundary_Produto_Marcado_Indisponivel_Dentro_Do_Dia_Operacional_Corrente_Nao_Virou()
    {
        var now = new DateTimeOffset(2026, 3, 10, 8, 0, 0, TimeSpan.Zero); // 08h de hoje
        var unavailableSince = new DateTimeOffset(2026, 3, 10, 6, 0, 0, TimeSpan.Zero); // 06h de hoje, já depois da virada das 5h

        BusinessDayPolicy.HasCrossedBusinessDayBoundary(unavailableSince, now, startHourUtc: 5).Should().BeFalse();
    }

    [Fact]
    public void HasCrossedBusinessDayBoundary_Marcado_Poucos_Minutos_Antes_Da_Virada_Ja_Conta_Como_Dia_Anterior()
    {
        var unavailableSince = new DateTimeOffset(2026, 3, 10, 4, 59, 0, TimeSpan.Zero); // 04:59, antes da virada das 5h
        var now = new DateTimeOffset(2026, 3, 10, 5, 1, 0, TimeSpan.Zero); // 05:01, um minuto depois da virada

        BusinessDayPolicy.HasCrossedBusinessDayBoundary(unavailableSince, now, startHourUtc: 5).Should().BeTrue();
    }
}
