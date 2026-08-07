using Nexora.Domain.Platform;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Platform;

/// <summary>US-154 §12 — reconciliação de capacidades efetivas (<see cref="TenantConfig.ApplyPlanCapabilities"/>), isolada de banco/HTTP.</summary>
public sealed class TenantConfigPlanCapabilitiesTests
{
    [Fact]
    public void CreateWithConfig_Sem_Plano_Informado_Comeca_Com_Capacidades_Vazias()
    {
        var config = TenantConfig.CreateWithConfig(
            Guid.NewGuid(), "{}", "{}", "{}", "{}", "{}", "[]", "{}", "{}");

        config.PlanCapabilitiesJson.Should().Be("[]");
        config.AppliedPlanVersion.Should().BeNull();
    }

    [Fact]
    public void CreateWithConfig_Com_Plano_Reconciliado_Nao_Diverge_Ao_Nascer()
    {
        var config = TenantConfig.CreateWithConfig(
            Guid.NewGuid(), "{}", "{}", "{}", "{}", "{}", "[]", "{}", "{}",
            planCapabilitiesJson: """["online_ordering","kds"]""",
            appliedPlanVersion: 1);

        config.PlanCapabilitiesJson.Should().Be("""["online_ordering","kds"]""");
        config.AppliedPlanVersion.Should().Be(1);
        config.ConfigVersion.Should().Be(1, "a reconciliação inicial não deve contar como uma mudança de configuração adicional");
    }

    [Fact]
    public void ApplyPlanCapabilities_Atualiza_Capacidades_E_Incrementa_ConfigVersion()
    {
        var config = TenantConfig.Create(Guid.NewGuid());

        config.ApplyPlanCapabilities("""["online_ordering","delivery"]""", planVersion: 2);

        config.PlanCapabilitiesJson.Should().Be("""["online_ordering","delivery"]""");
        config.AppliedPlanVersion.Should().Be(2);
        config.ConfigVersion.Should().Be(2);
    }

    [Fact]
    public void ApplyPlanCapabilities_Com_Json_Vazio_Usa_Array_Vazio()
    {
        var config = TenantConfig.Create(Guid.NewGuid());

        config.ApplyPlanCapabilities("", planVersion: 1);

        config.PlanCapabilitiesJson.Should().Be("[]");
    }
}
