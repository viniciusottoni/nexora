using Nexora.Application.Orders.Support;
using Nexora.Domain.Operation;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Operation;

/// <summary>
/// US-033 (Cancelar item ou pedido com autorização) §12 — "Máquina de estados: quais transições
/// exigem autorização". Matriz completa: <c>QUEUED</c> cancela livre (cenário "Cancelamento antes
/// do início da produção"); qualquer estado a partir de <c>FIRED</c> (a máquina de estados do
/// documento 04 trata como "IN_PRODUCTION → CANCELLED") exige elevação pontual (ADR-023).
/// </summary>
public sealed class OrderCancellationPolicyTests
{
    [Fact]
    public void Item_Em_Fila_Nao_Exige_Autorizacao()
    {
        OrderCancellationPolicy.RequiresAuthorization(OrderItemStatus.Queued).Should().BeFalse();
    }

    [Theory]
    [InlineData(OrderItemStatus.Fired)]
    [InlineData(OrderItemStatus.InOven)]
    [InlineData(OrderItemStatus.OutOfOven)]
    [InlineData(OrderItemStatus.Ready)]
    public void Item_Ja_Iniciado_Exige_Autorizacao(OrderItemStatus statusBeforeCancel)
    {
        OrderCancellationPolicy.RequiresAuthorization(statusBeforeCancel).Should().BeTrue();
    }
}
