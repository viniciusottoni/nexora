using Nexora.Contracts.Platform;
using Nexora.Domain.Platform;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Onboarding;

/// <summary>
/// US-141 §7 — prova, string por string, que o formato de fio bate EXATAMENTE com o contrato de API
/// da história (<c>"TENANT_CREATED"</c>, <c>"EDGE_INSTALL"</c>, <c>"PAYMENT_CONFIG"</c>, ...) —
/// não o nome do membro C# (<c>OnboardingStepKey.EdgeInstall</c>) nem o valor persistido no banco
/// (a coluna <c>onboarding_step.key</c> usa a conversão padrão do EF, que grava
/// <c>"EdgeInstall"</c>). Ver a docstring de <see cref="OnboardingStepKeyWireFormat"/> para o porquê
/// dessa distinção existir.
/// </summary>
public sealed class OnboardingStepKeyWireFormatTests
{
    [Theory]
    [InlineData(OnboardingStepKey.TenantCreated, "TENANT_CREATED")]
    [InlineData(OnboardingStepKey.Branding, "BRANDING")]
    [InlineData(OnboardingStepKey.Menu, "MENU")]
    [InlineData(OnboardingStepKey.Tables, "TABLES")]
    [InlineData(OnboardingStepKey.EdgeInstall, "EDGE_INSTALL")]
    [InlineData(OnboardingStepKey.PaymentConfig, "PAYMENT_CONFIG")]
    [InlineData(OnboardingStepKey.Training, "TRAINING")]
    [InlineData(OnboardingStepKey.Pilot, "PILOT")]
    [InlineData(OnboardingStepKey.Activation, "ACTIVATION")]
    public void ToWireKey_Bate_Com_O_Literal_Exato_Do_Contrato_Da_US(OnboardingStepKey key, string expectedWireKey)
    {
        OnboardingStepKeyWireFormat.ToWireKey(key).Should().Be(expectedWireKey);
    }

    [Theory]
    [InlineData("TENANT_CREATED", OnboardingStepKey.TenantCreated)]
    [InlineData("EDGE_INSTALL", OnboardingStepKey.EdgeInstall)]
    [InlineData("PAYMENT_CONFIG", OnboardingStepKey.PaymentConfig)]
    public void TryParseWireKey_Faz_O_Caminho_Inverso(string wireKey, OnboardingStepKey expectedKey)
    {
        OnboardingStepKeyWireFormat.TryParseWireKey(wireKey, out var key).Should().BeTrue();
        key.Should().Be(expectedKey);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("edge_install")]
    [InlineData("EdgeInstall")]
    [InlineData("UNKNOWN_STEP")]
    public void TryParseWireKey_Devolve_Falso_Para_Chave_Desconhecida_Ou_Com_Caixa_Errada(string? wireKey)
    {
        OnboardingStepKeyWireFormat.TryParseWireKey(wireKey, out _).Should().BeFalse();
    }

    [Theory]
    [InlineData(OnboardingStepStatus.Pending, "PENDING")]
    [InlineData(OnboardingStepStatus.InProgress, "IN_PROGRESS")]
    [InlineData(OnboardingStepStatus.Done, "DONE")]
    public void ToWireStatus_Bate_Com_O_Literal_Exato_Do_Contrato_Da_US(OnboardingStepStatus status, string expectedWireStatus)
    {
        OnboardingStepKeyWireFormat.ToWireStatus(status).Should().Be(expectedWireStatus);
    }

    [Fact]
    public void Todas_As_Nove_Chaves_Do_Enum_Tem_Mapeamento_De_Fio()
    {
        foreach (var key in Enum.GetValues<OnboardingStepKey>())
        {
            var wireKey = OnboardingStepKeyWireFormat.ToWireKey(key);
            wireKey.Should().MatchRegex("^[A-Z]+(_[A-Z]+)*$", "o contrato da US-141 §7 usa SCREAMING_SNAKE_CASE");
            OnboardingStepKeyWireFormat.TryParseWireKey(wireKey, out var roundTripped).Should().BeTrue();
            roundTripped.Should().Be(key);
        }
    }
}
