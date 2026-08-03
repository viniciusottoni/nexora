using Nexora.Application.Audit.Support;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Platform;

/// <summary>
/// E-09/US-091 — trava contra desvio silencioso entre os códigos de ação REALMENTE emitidos pelos
/// handlers (ver <c>Nexora.IntegrationTests.AuditCoverageTests</c>) e o switch de
/// <see cref="AuditSummaryFormatter"/>: um código sem frase dedicada cai no resumo genérico sem
/// erro nenhum — este teste garante que isso não aconteça por acidente (renomear a ação num
/// handler sem atualizar o formatter, por exemplo).
/// </summary>
public sealed class AuditSummaryFormatterTests
{
    private const string GenericFallbackPrefix = "Ação sensível:";

    [Theory]
    [InlineData("ORDER_CANCELLED")]
    [InlineData("ORDER_ITEM_CANCELLED")]
    [InlineData("ORDER_CANCEL_DENIED")]
    [InlineData("ORDER_ITEM_CANCEL_DENIED")]
    [InlineData("PERMISSION_CHANGED")]
    [InlineData("ROLE_UPDATED")]
    [InlineData("VARIANT_PRICE_CHANGED")]
    [InlineData("PRICE_CHANGED")]
    [InlineData("PRICE_BULK_ADJUSTED")]
    [InlineData("SUPPORT_ACCESS_GRANTED")]
    [InlineData("tenant.cross_tenant_access_attempt")]
    public void Acao_Conhecida_Tem_Frase_Dedicada_Nao_Cai_No_Generico(string action)
    {
        var summary = AuditSummaryFormatter.Format(action, before: null, after: null);

        summary.Should().NotStartWith(GenericFallbackPrefix,
            $"a ação '{action}' já é emitida em produção e precisa de uma frase dedicada, não do resumo genérico.");
    }

    [Fact]
    public void Acao_Realmente_Desconhecida_Cai_No_Resumo_Generico_Sem_Quebrar()
    {
        var summary = AuditSummaryFormatter.Format("UM_CODIGO_QUE_NAO_EXISTE", before: null, after: null);

        summary.Should().StartWith(GenericFallbackPrefix);
    }
}
