using Nexora.Application.Auth.Shared;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Auth;

/// <summary>
/// US-004, gap "encerramento de sessão inativa configurável — 100% não implementado". Cobre a
/// leitura de <c>sessionInactivityMinutes</c> de <c>TenantConfig.Operation</c> — a MESMA chave que
/// <c>Nexora.Domain.Provisioning.ProvisioningTemplates</c> já grava (valor 30) para o template
/// "PIZZERIA".
/// </summary>
public sealed class SessionInactivityPolicyTests
{
    [Fact]
    public void Operation_Nulo_Ou_Vazio_Usa_O_Default()
    {
        SessionInactivityPolicy.ResolveMinutes(null).Should().Be(SessionInactivityPolicy.DefaultMinutes);
        SessionInactivityPolicy.ResolveMinutes(string.Empty).Should().Be(SessionInactivityPolicy.DefaultMinutes);
        SessionInactivityPolicy.ResolveMinutes("{}").Should().Be(SessionInactivityPolicy.DefaultMinutes);
    }

    [Fact]
    public void Chave_Presente_E_Valida_E_Respeitada()
    {
        SessionInactivityPolicy.ResolveMinutes("""{"sessionInactivityMinutes": 45}""").Should().Be(45);
    }

    [Fact]
    public void Chave_Presente_Junto_De_Outras_Chaves_De_Operation_E_Respeitada()
    {
        const string operationJson = """
            {"serviceFeePercent": 10, "sessionInactivityMinutes": 15, "businessDayStartHour": 5}
            """;

        SessionInactivityPolicy.ResolveMinutes(operationJson).Should().Be(15);
    }

    [Theory]
    [InlineData("""{"sessionInactivityMinutes": 0}""")]
    [InlineData("""{"sessionInactivityMinutes": -5}""")]
    [InlineData("""{"sessionInactivityMinutes": "trinta"}""")]
    [InlineData("""{"outraChave": 30}""")]
    [InlineData("json inválido")]
    [InlineData("[1,2,3]")]
    public void Valor_Invalido_Ausente_Ou_Nao_Numerico_Cai_No_Default(string operationJson)
    {
        SessionInactivityPolicy.ResolveMinutes(operationJson).Should().Be(SessionInactivityPolicy.DefaultMinutes);
    }
}
