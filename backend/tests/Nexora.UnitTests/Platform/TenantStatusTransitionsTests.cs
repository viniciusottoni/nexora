using Nexora.Domain.Platform;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Platform;

/// <summary>
/// US-153 §12 "Unitário: matriz completa de transições válidas e inválidas" —
/// <see cref="TenantStatusTransitions"/> isolada de banco/HTTP.
/// </summary>
public sealed class TenantStatusTransitionsTests
{
    [Theory]
    [InlineData(TenantStatus.Provisioned, TenantStatus.Installing)]
    [InlineData(TenantStatus.Provisioned, TenantStatus.Cancelled)]
    [InlineData(TenantStatus.Installing, TenantStatus.Active)]
    [InlineData(TenantStatus.Installing, TenantStatus.Cancelled)]
    [InlineData(TenantStatus.Active, TenantStatus.Suspended)]
    [InlineData(TenantStatus.Active, TenantStatus.Cancelled)]
    [InlineData(TenantStatus.Suspended, TenantStatus.Active)]
    [InlineData(TenantStatus.Suspended, TenantStatus.Cancelled)]
    public void IsValid_Aceita_Transicoes_Da_Matriz_Canonica(TenantStatus from, TenantStatus to)
    {
        TenantStatusTransitions.IsValid(from, to).Should().BeTrue();
    }

    [Theory]
    [InlineData(TenantStatus.Cancelled, TenantStatus.Active)]
    [InlineData(TenantStatus.Cancelled, TenantStatus.Provisioned)]
    [InlineData(TenantStatus.Cancelled, TenantStatus.Suspended)]
    [InlineData(TenantStatus.Provisioned, TenantStatus.Active)]
    [InlineData(TenantStatus.Active, TenantStatus.Provisioned)]
    [InlineData(TenantStatus.Active, TenantStatus.Installing)]
    [InlineData(TenantStatus.Suspended, TenantStatus.Installing)]
    public void IsValid_Recusa_Transicoes_Fora_Da_Matriz(TenantStatus from, TenantStatus to)
    {
        TenantStatusTransitions.IsValid(from, to).Should().BeFalse();
    }

    [Fact]
    public void IsValid_Recusa_Permanecer_No_Mesmo_Status()
    {
        TenantStatusTransitions.IsValid(TenantStatus.Active, TenantStatus.Active).Should().BeFalse();
    }

    [Fact]
    public void Cancelled_E_Terminal_Nenhuma_Transicao_Sai_Dele()
    {
        foreach (var target in Enum.GetValues<TenantStatus>())
        {
            TenantStatusTransitions.IsValid(TenantStatus.Cancelled, target).Should().BeFalse();
        }
    }

    [Fact]
    public void AdminTargetsFrom_Nunca_Inclui_Installing()
    {
        foreach (var status in Enum.GetValues<TenantStatus>())
        {
            TenantStatusTransitions.AdminTargetsFrom(status).Should().NotContain(TenantStatus.Installing);
        }
    }

    [Fact]
    public void AdminTargetsFrom_Active_Permite_Suspender_Ou_Cancelar()
    {
        TenantStatusTransitions.AdminTargetsFrom(TenantStatus.Active)
            .Should().BeEquivalentTo(new[] { TenantStatus.Suspended, TenantStatus.Cancelled });
    }

    [Fact]
    public void AdminTargetsFrom_Suspended_Permite_Reativar_Ou_Cancelar()
    {
        TenantStatusTransitions.AdminTargetsFrom(TenantStatus.Suspended)
            .Should().BeEquivalentTo(new[] { TenantStatus.Active, TenantStatus.Cancelled });
    }

    [Fact]
    public void AdminTargetsFrom_Cancelled_Nao_Permite_Nenhum_Alvo()
    {
        TenantStatusTransitions.AdminTargetsFrom(TenantStatus.Cancelled).Should().BeEmpty();
    }
}
