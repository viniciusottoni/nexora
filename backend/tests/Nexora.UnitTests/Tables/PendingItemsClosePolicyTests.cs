using Nexora.Application.Tables.Support;
using Nexora.Contracts.Operation;
using Nexora.Domain.Operation;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Tables;

/// <summary>
/// US-035 (Bloquear fechamento com item pendente) — cobre a função pura de resolução do modo
/// configurado (<c>pendingItemsOnClose</c>) e a extração dos itens que impedem o fechamento, sem
/// tocar banco/HTTP. Mesmo espírito de <c>BusinessDayPolicyTests</c>: a REGRA em si (não a
/// orquestração de handlers, cobertos pelos testes de integração).
/// </summary>
public sealed class PendingItemsClosePolicyTests
{
    [Fact]
    public void ResolveMode_Sem_Configuracao_Usa_O_Default_Warn()
    {
        PendingItemsClosePolicy.ResolveMode(null).Should().Be(PendingItemsClosePolicy.Warn);
        PendingItemsClosePolicy.ResolveMode("").Should().Be(PendingItemsClosePolicy.Warn);
        PendingItemsClosePolicy.ResolveMode("{}").Should().Be(PendingItemsClosePolicy.Warn);
    }

    [Theory]
    [InlineData("BLOCK", PendingItemsClosePolicy.Block)]
    [InlineData("WARN", PendingItemsClosePolicy.Warn)]
    [InlineData("IGNORE", PendingItemsClosePolicy.Ignore)]
    [InlineData("block", PendingItemsClosePolicy.Block)]
    public void ResolveMode_Le_A_Chave_Configurada_Pelo_Tenant(string configured, string expected)
    {
        PendingItemsClosePolicy.ResolveMode($$"""{"pendingItemsOnClose": "{{configured}}"}""").Should().Be(expected);
    }

    [Fact]
    public void ResolveMode_Com_Valor_Invalido_Cai_No_Default()
    {
        PendingItemsClosePolicy.ResolveMode("""{"pendingItemsOnClose": "ALWAYS"}""").Should().Be(PendingItemsClosePolicy.Warn);
    }

    [Fact]
    public void ResolveMode_Com_Json_Malformado_Cai_No_Default()
    {
        PendingItemsClosePolicy.ResolveMode("{ nao é json").Should().Be(PendingItemsClosePolicy.Warn);
    }

    [Fact]
    public void ResolveMode_Ignora_O_Booleano_Legado_Dos_Documentos_De_Dominio()
    {
        // Docs genéricos (03-Modelo-de-Dados.md, Domain/01, Domain/12) mostram
        // "blockCloseWithPendingItems": true — booleano de uma passada anterior. A US-035 usa
        // pendingItemsOnClose (string enum), não esse booleano — ver docstring de PendingItemsClosePolicy.
        PendingItemsClosePolicy.ResolveMode("""{"blockCloseWithPendingItems": true}""").Should().Be(PendingItemsClosePolicy.Warn);
    }

    [Fact]
    public void FindPendingForClose_Exclui_Served_E_Cancelled_Inclui_Ready()
    {
        var items = new[]
        {
            BuildItem(OrderItemStatus.Queued),
            BuildItem(OrderItemStatus.Ready),
            BuildItem(OrderItemStatus.Served),
            BuildItem(OrderItemStatus.Cancelled),
        };

        var pending = PendingItemsClosePolicy.FindPendingForClose(items);

        pending.Should().HaveCount(2);
        pending.Select(p => p.Status).Should().BeEquivalentTo(new[] { "QUEUED", "READY" });
    }

    [Fact]
    public void FindPendingForClose_Sem_Item_Pendente_Devolve_Lista_Vazia()
    {
        var items = new[] { BuildItem(OrderItemStatus.Served), BuildItem(OrderItemStatus.Cancelled) };

        PendingItemsClosePolicy.FindPendingForClose(items).Should().BeEmpty();
    }

    [Fact]
    public void BuildMetaErrors_Empacota_A_Lista_Como_Json_Na_Chave_Reservada()
    {
        var pending = new List<BillPendingItemResponse> { new(Guid.NewGuid(), "Petit Gateau", "READY") };

        var errors = PendingItemsClosePolicy.BuildMetaErrors(pending);

        errors.Should().ContainKey(PendingItemsClosePolicy.MetaErrorsKey);
        errors[PendingItemsClosePolicy.MetaErrorsKey].Single().Should().Contain("Petit Gateau").And.Contain("READY");
    }

    private static OrderItem BuildItem(OrderItemStatus status)
    {
        var category = Nexora.Domain.Catalog.Category.Create(Guid.NewGuid(), "Categoria de teste");
        var product = Nexora.Domain.Catalog.Product.Create(Guid.NewGuid(), category.Id, "Produto de teste");
        var variant = Nexora.Domain.Catalog.ProductVariant.Create(Guid.NewGuid(), product.Id, "Variante de teste");

        var item = OrderItem.Create(Guid.NewGuid(), Guid.NewGuid(), variant.Id, unitPrice: 10m);

        // Avança até o status desejado pelos métodos de domínio (nunca setter direto) — MarkReady
        // aceita direto de QUEUED (o guard só bloqueia SERVED/CANCELLED, ver OrderItem.MarkReady),
        // então não é preciso passar por Fire/SendToOven/TakeOutOfOven para este teste.
        switch (status)
        {
            case OrderItemStatus.Queued:
                break;
            case OrderItemStatus.Ready:
                item.MarkReady(Guid.NewGuid());
                break;
            case OrderItemStatus.Served:
                item.MarkReady(Guid.NewGuid());
                item.MarkServed(Guid.NewGuid());
                break;
            case OrderItemStatus.Cancelled:
                item.Cancel("Motivo de teste", Guid.NewGuid());
                break;
        }

        SetVariantForTest(item, variant, product);
        return item;
    }

    /// <summary>
    /// <see cref="OrderItem.Variant"/> normalmente vem de <c>Include</c> do EF — aqui, sem banco,
    /// atribuído via reflexão só para o teste ter <c>BillQueryCoordinator.ItemName</c> funcionando
    /// (produto+variante), sem exigir um <see cref="Microsoft.EntityFrameworkCore.DbContext"/> real.
    /// </summary>
    private static void SetVariantForTest(OrderItem item, Nexora.Domain.Catalog.ProductVariant variant, Nexora.Domain.Catalog.Product product)
    {
        SetProductForTest(variant, product);
        typeof(OrderItem).GetProperty(nameof(OrderItem.Variant))!.SetValue(item, variant);
    }

    private static void SetProductForTest(Nexora.Domain.Catalog.ProductVariant variant, Nexora.Domain.Catalog.Product product)
    {
        typeof(Nexora.Domain.Catalog.ProductVariant).GetProperty(nameof(Nexora.Domain.Catalog.ProductVariant.Product))!.SetValue(variant, product);
    }
}
