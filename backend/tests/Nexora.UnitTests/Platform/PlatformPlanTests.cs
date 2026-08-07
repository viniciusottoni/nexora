using Nexora.Domain.Common;
using Nexora.Domain.Platform;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Platform;

/// <summary>US-154 §12 "Unitário: resolução de capacidades e vigência" — cobre o catálogo <see cref="PlatformPlan"/> isolado de banco/HTTP.</summary>
public sealed class PlatformPlanTests
{
    [Fact]
    public void Create_Normaliza_Codigo_Em_Maiusculas_E_Comeca_Ativo_Na_Versao_1()
    {
        var plan = PlatformPlan.Create("completo", "Completo", """["online_ordering"]""", """{"maxStores":1}""");

        plan.Code.Should().Be("COMPLETO");
        plan.Version.Should().Be(1);
        plan.IsActive.Should().BeTrue();
        plan.CapabilitiesJson.Should().Be("""["online_ordering"]""");
    }

    [Fact]
    public void Create_Sem_Codigo_Lanca_DomainException()
    {
        var act = () => PlatformPlan.Create("  ", "Completo", "[]", "{}");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_Sem_Nome_Lanca_DomainException()
    {
        var act = () => PlatformPlan.Create("COMPLETO", "  ", "[]", "{}");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_Com_Capabilities_Vazio_Usa_Array_Vazio_Default()
    {
        var plan = PlatformPlan.Create("COMPLETO", "Completo", "", "");

        plan.CapabilitiesJson.Should().Be("[]");
        plan.LimitsJson.Should().Be("{}");
    }

    [Fact]
    public void Update_Incrementa_Versao_E_Atualiza_Capacidades()
    {
        var plan = PlatformPlan.Create("GESTAO", "Gestão", """["a"]""", "{}");

        plan.Update("Gestão Plus", """["a","b"]""", """{"maxStores":3}""");

        plan.Name.Should().Be("Gestão Plus");
        plan.Version.Should().Be(2);
        plan.CapabilitiesJson.Should().Be("""["a","b"]""");
        plan.LimitsJson.Should().Be("""{"maxStores":3}""");
    }

    [Fact]
    public void Deactivate_Torna_Plano_Indisponivel_Para_Nova_Atribuicao()
    {
        var plan = PlatformPlan.Create("LEGADO", "Legado", "[]", "{}");

        plan.Deactivate();

        plan.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_Reativa_Um_Plano_Desativado()
    {
        var plan = PlatformPlan.Create("LEGADO", "Legado", "[]", "{}");
        plan.Deactivate();

        plan.Activate();

        plan.IsActive.Should().BeTrue();
    }
}
