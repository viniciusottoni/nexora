using Nexora.Application.Onboarding.Support;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Onboarding;

/// <summary>
/// US-141 §7/§11 (<c>elapsedBusinessDays</c>, meta "≤ 5 dias úteis") — cobre a contagem pura de dias
/// úteis fechados entre o início da implantação e agora, sem banco.
/// </summary>
public sealed class OnboardingElapsedBusinessDaysTests
{
    private static readonly DateTimeOffset Monday = new(2026, 8, 3, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Mesmo_Dia_Nao_Conta_Como_Decorrido()
    {
        OnboardingElapsedBusinessDays.Calculate(Monday, Monday.AddHours(6)).Should().Be(0);
    }

    [Fact]
    public void Um_Dia_Util_Inteiro_Decorrido_Conta_Um()
    {
        // Começou segunda, agora é terça — a segunda inteira já fechou.
        OnboardingElapsedBusinessDays.Calculate(Monday, Monday.AddDays(1)).Should().Be(1);
    }

    [Fact]
    public void Fim_De_Semana_Nao_Conta_Como_Dia_Util()
    {
        // Começou segunda, agora é a segunda seguinte (7 dias corridos) — seg/ter/qua/qui/sex da
        // primeira semana contam (5), sábado e domingo não.
        OnboardingElapsedBusinessDays.Calculate(Monday, Monday.AddDays(7)).Should().Be(5);
    }

    [Fact]
    public void Comecando_No_Sabado_Nao_Conta_O_Proprio_Fim_De_Semana()
    {
        var saturday = Monday.AddDays(5);
        // Sábado -> segunda seguinte: sábado e domingo não são úteis, então 0 dias úteis decorridos.
        OnboardingElapsedBusinessDays.Calculate(saturday, saturday.AddDays(2)).Should().Be(0);
    }

    [Fact]
    public void Agora_Antes_Do_Inicio_Devolve_Zero_Em_Vez_De_Negativo()
    {
        OnboardingElapsedBusinessDays.Calculate(Monday, Monday.AddDays(-3)).Should().Be(0);
    }
}
