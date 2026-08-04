using Nexora.Application.Alerts.Support;
using Nexora.Domain.Metrics;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Alerts;

/// <summary>US-083 §4, cenário "Rajada agrupada": "5 pedidos atrasados", nunca "múltiplos alertas" (US-083 §10).</summary>
public sealed class AlertMessagesTests
{
    [Fact]
    public void GroupMessage_Usa_Rotulo_Direto_Com_Contagem()
    {
        AlertMessages.GroupMessage(AlertTypes.OrderLate, 5).Should().Be("5 pedidos atrasados");
    }

    [Fact]
    public void GroupMessage_Nunca_Usa_O_Texto_Generico_Multiplos_Alertas()
    {
        AlertMessages.GroupMessage(AlertTypes.OrderLate, 5).Should().NotContain("múltiplos alertas");
    }
}
