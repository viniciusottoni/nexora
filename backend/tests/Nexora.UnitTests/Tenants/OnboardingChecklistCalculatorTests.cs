using Nexora.Application.Tenants.Support;
using Nexora.Domain.Platform;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Tenants;

/// <summary>
/// US-156 · Recuperação do provisionamento e token de instalação — <see cref="OnboardingChecklistCalculator"/>
/// (helper extraído para <c>GetTenantDeploymentStatusQueryHandler</c> não duplicar, sem testar, a
/// lógica de "completed/total/nextAction" já usada por <c>GetTenantOverviewQueryHandler</c>, US-152).
/// </summary>
public sealed class OnboardingChecklistCalculatorTests
{
    [Fact]
    public void Calculate_Com_Apenas_TenantCreated_Concluido_Aponta_Branding_Como_Proxima_Acao()
    {
        var tenantId = Guid.NewGuid();
        var steps = OnboardingStep.SeedAll(tenantId, DateTimeOffset.UtcNow);

        var (completed, total, nextAction) = OnboardingChecklistCalculator.Calculate(steps);

        completed.Should().Be(1); // só TenantCreated nasce Done (OnboardingStep.SeedAll)
        total.Should().Be(9);
        nextAction.Should().Be(OnboardingStepKey.Branding);
    }

    [Fact]
    public void Calculate_Sem_Nenhuma_Linha_Persistida_Trata_Tudo_Como_Pending()
    {
        var (completed, total, nextAction) = OnboardingChecklistCalculator.Calculate(
            Array.Empty<OnboardingStep>());

        completed.Should().Be(0);
        total.Should().Be(9);
        nextAction.Should().Be(OnboardingStepKey.TenantCreated);
    }

    [Fact]
    public void Calculate_Com_Todos_Concluidos_NextAction_E_Nulo()
    {
        var tenantId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var steps = OnboardingStep.SeedAll(tenantId, now);
        foreach (var step in steps)
        {
            step.Complete(now, completedBy: null);
        }

        var (completed, total, nextAction) = OnboardingChecklistCalculator.Calculate(steps);

        completed.Should().Be(total);
        nextAction.Should().BeNull();
    }
}
