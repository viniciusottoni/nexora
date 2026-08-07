using Nexora.Domain.Common;
using Nexora.Domain.Platform;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Platform;

/// <summary>
/// US-154 §12 "Unitário: resolução de capacidades e vigência" — cobre os métodos de plano
/// comercial de <see cref="Tenant"/> (<see cref="Tenant.SetPlan"/>/<see cref="Tenant.SchedulePlanChange"/>/
/// <see cref="Tenant.ApplyScheduledPlan"/>), separado de <c>TenantTests.cs</c> (ciclo de vida de
/// status, US-153) para isolar os dois arquivos entre histórias que evoluem em paralelo.
/// </summary>
public sealed class TenantPlanTests
{
    [Fact]
    public void SetPlan_Persiste_Plano_Confirmado_Sem_Substituir_Por_Default()
    {
        var tenant = Tenant.Create("pizzaria-teste", "Pizzaria Teste");

        tenant.SetPlan("completo");

        tenant.Plan.Should().Be("COMPLETO", "o plano confirmado no formulário nunca deve ser substituído silenciosamente");
    }

    [Fact]
    public void SetPlan_Vazio_Lanca_DomainException()
    {
        var tenant = Tenant.Create("pizzaria-teste", "Pizzaria Teste");

        var act = () => tenant.SetPlan("  ");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void SchedulePlanChange_Com_Vigencia_Ja_Passada_Aplica_Imediatamente()
    {
        var tenant = Tenant.Create("pizzaria-teste", "Pizzaria Teste");
        tenant.SetPlan("GESTAO");
        var now = DateTimeOffset.UtcNow;

        var (previous, appliedImmediately) = tenant.SchedulePlanChange("COMPLETO", now.AddMinutes(-1), now);

        previous.Should().Be("GESTAO");
        appliedImmediately.Should().BeTrue();
        tenant.Plan.Should().Be("COMPLETO");
        tenant.PlanVersion.Should().Be(2);
    }

    [Fact]
    public void SchedulePlanChange_Com_Vigencia_Futura_Nao_Muda_Plano_Ainda()
    {
        var tenant = Tenant.Create("pizzaria-teste", "Pizzaria Teste");
        tenant.SetPlan("GESTAO");
        var now = DateTimeOffset.UtcNow;

        var (previous, appliedImmediately) = tenant.SchedulePlanChange("COMPLETO", now.AddDays(7), now);

        previous.Should().Be("GESTAO");
        appliedImmediately.Should().BeFalse();
        tenant.Plan.Should().Be("GESTAO", "o plano atual deve permanecer até a vigência (US-154 §4)");
        tenant.PlanVersion.Should().Be(2, "o agendamento em si já é uma decisão administrativa contada na concorrência otimista");
    }

    [Fact]
    public void ApplyScheduledPlan_Efetiva_Mudanca_Agendada_Sem_Incrementar_PlanVersion_De_Novo()
    {
        var tenant = Tenant.Create("pizzaria-teste", "Pizzaria Teste");
        tenant.SetPlan("GESTAO");
        var now = DateTimeOffset.UtcNow;
        tenant.SchedulePlanChange("COMPLETO", now.AddDays(7), now);
        var versionAfterScheduling = tenant.PlanVersion;

        var previous = tenant.ApplyScheduledPlan("COMPLETO", now.AddDays(7));

        previous.Should().Be("GESTAO");
        tenant.Plan.Should().Be("COMPLETO");
        tenant.PlanVersion.Should().Be(versionAfterScheduling, "a efetivação tardia não é uma nova decisão administrativa");
    }

    [Fact]
    public void SchedulePlanChange_Vazio_Lanca_DomainException()
    {
        var tenant = Tenant.Create("pizzaria-teste", "Pizzaria Teste");

        var act = () => tenant.SchedulePlanChange("  ", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>();
    }
}
