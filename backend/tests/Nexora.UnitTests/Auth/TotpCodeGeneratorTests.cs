using FluentAssertions;
using Nexora.Infrastructure.Auth;
using Xunit;

namespace Nexora.UnitTests.Auth;

public sealed class TotpCodeGeneratorTests
{
    [Fact]
    public void Current_Gera_Codigo_Compatível_Com_O_Secret_De_Testes()
    {
        var instant = DateTimeOffset.FromUnixTimeSeconds(1_785_721_974);

        var otp = TotpCodeGenerator.Current("JBSWY3DPEHPK3PXP", instant);

        otp.Should().Be("000484");
    }

    [Fact]
    public void SecondsRemaining_Fica_Entre_1_e_30_Segundos()
    {
        var instant = DateTimeOffset.FromUnixTimeSeconds(1_785_721_974);

        var remaining = TotpCodeGenerator.SecondsRemaining(instant);

        remaining.Should().BeInRange(1, 30);
    }
}
