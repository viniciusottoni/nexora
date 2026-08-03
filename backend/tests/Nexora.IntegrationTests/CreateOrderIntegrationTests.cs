using System.Text;
using System.Text.Json;
using Nexora.Api.Edge.Infrastructure.Idempotency;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Orders.Commands.AddOrderItem;
using Nexora.Application.Orders.Commands.CreateOrder;
using Nexora.Contracts.Operation;
using Nexora.Domain.Catalog;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.Infrastructure.Idempotency;
using Nexora.IntegrationTests.Fakes;
using Nexora.IntegrationTests.Fixtures;
using Nexora.Shared.Errors;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Nexora.IntegrationTests;

/// <summary>
/// US-030 (Criar pedido com itens, modificadores e frações) contra um PostgreSQL real
/// (Testcontainers) — mesmo pipeline MediatR/idempotência real de produção (ADR-037/ADR-020).
/// </summary>
[Collection("Postgres")]
public sealed class CreateOrderIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public CreateOrderIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Cenário Gherkin "Horário de ocorrência preservado" (US-030 §4/§9): occurredAt do dispositivo
    /// (X-Occurred-At) é gravado como T0 do pedido — não o relógio do edge no instante do
    /// SaveChanges. O atraso de sincronização em si (loja offline até 21h15, doc. 09 §9: "recordedAt
    /// deve ser 21h15") acontece uma camada abaixo, no worker de sync edge→nuvem (ADR-007): o valor
    /// de <c>DomainEvent.OccurredAt</c> já grava correto no INSERT local e nunca é reescrito depois
    /// — por isso a garantia relevante de testar AQUI é "o valor do header vence o relógio do
    /// servidor local", com um desvio DENTRO da tolerância de 2 min do ADR-034 (o dispositivo está
    /// na mesma rede local do edge; um desvio maior seria relógio de dispositivo suspeito, não
    /// atraso de sync — ver <c>ClockSkewPolicyTests</c>).
    /// </summary>
    [Fact]
    public async Task Pedido_Criado_Preserva_O_OccurredAt_Do_Dispositivo_Em_Vez_Do_Relogio_Do_Edge()
    {
        var world = await SeedWorldAsync();
        var (variantId, _) = await SeedProductAsync(world.TenantId, "Pizza Calabresa", "Broto", unitPrice: 40m);
        var sessionId = await OpenSessionAsync(world);

        // Precisão de milissegundo (formato real de X-Occurred-At, ex.: "2026-07-31T20:47:12.334Z")
        // — Postgres timestamptz só guarda microssegundo; DateTimeOffset.UtcNow tem tick (100ns) de
        // resolução, e a comparação pós-roundtrip do banco falharia por um artefato de precisão sem
        // relação nenhuma com a regra de negócio sendo testada aqui.
        var raw = DateTimeOffset.UtcNow.AddSeconds(-90);
        var occurredAt = new DateTimeOffset(raw.Year, raw.Month, raw.Day, raw.Hour, raw.Minute, raw.Second, raw.Millisecond, raw.Offset);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new CreateOrderCommand(
            "DineIn", sessionId, new[] { new CreateOrderItemInput(variantId, 1, null, null, null) }, occurredAt));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Order.PlacedAt.Should().Be(occurredAt, "T0 vem do X-Occurred-At do dispositivo, não do relógio do edge no momento da sincronização");

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var order = await verifyDb.Orders.SingleAsync(o => o.Id == result.Value.Order.Id);
        order.PlacedAt.Should().Be(occurredAt);

        var placedEvent = await verifyDb.DomainEvents.SingleAsync(e => e.AggregateId == order.Id && e.Type == "order.placed");
        placedEvent.OccurredAt.Should().Be(occurredAt);
    }

    /// <summary>US-030 §6/ADR-006/ADR-007 — o evento order.placed é gravado no outbox na MESMA transação do estado.</summary>
    [Fact]
    public async Task Evento_Order_Placed_E_Gravado_Na_Mesma_Transacao_Do_Estado()
    {
        var world = await SeedWorldAsync();
        var (variantId, _) = await SeedProductAsync(world.TenantId, "Refrigerante", "Lata", unitPrice: 8m);
        var sessionId = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new CreateOrderCommand(
            "DineIn", sessionId, new[] { new CreateOrderItemInput(variantId, 2, null, null, null) }, null));

        result.IsSuccess.Should().BeTrue();

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        var events = await verifyDb.DomainEvents.Where(e => e.AggregateId == result.Value!.Order.Id).ToListAsync();

        events.Should().Contain(e => e.Type == "order.created");
        events.Should().Contain(e => e.Type == "order.placed");
    }

    /// <summary>Cenário Gherkin "Produto indisponível no momento do envio" (US-030 §4): item inválido no meio da lista não deixa resíduo — nenhum pedido/item parcial é criado.</summary>
    [Fact]
    public async Task Item_Invalido_No_Meio_Da_Lista_Nao_Cria_Pedido_Incompleto()
    {
        var world = await SeedWorldAsync();
        var (validVariantId, _) = await SeedProductAsync(world.TenantId, "Suco", "300ml", unitPrice: 9m);
        var (unavailableVariantId, unavailableProductId) = await SeedProductAsync(world.TenantId, "Sorvete", "Casquinha", unitPrice: 12m);
        await MarkProductUnavailableAsync(world.TenantId, unavailableProductId);
        var sessionId = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new CreateOrderCommand(
            "DineIn",
            sessionId,
            new[]
            {
                new CreateOrderItemInput(validVariantId, 1, null, null, null),
                new CreateOrderItemInput(unavailableVariantId, 1, null, null, null),
            },
            null));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.ProductUnavailable);

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        (await verifyDb.Orders.CountAsync(o => o.SessionId == sessionId)).Should().Be(0, "nenhum pedido deve sobrar — nem o item válido do início da lista");
        (await verifyDb.OrderItems.CountAsync()).Should().Be(0);
    }

    /// <summary>Cenário Gherkin "Grupo de modificadores obrigatório pendente" (US-030 §4): 422 com o grupo pendente identificado, nenhum pedido criado.</summary>
    [Fact]
    public async Task Grupo_De_Modificador_Obrigatorio_Pendente_Bloqueia_A_Criacao_Do_Pedido()
    {
        var world = await SeedWorldAsync();
        var (variantId, productId) = await SeedProductAsync(world.TenantId, "Pizza Grande", "Única", unitPrice: 45m);
        var groupId = await LinkRequiredModifierGroupAsync(world.TenantId, productId, "Tamanho");
        var sessionId = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new CreateOrderCommand(
            "DineIn", sessionId, new[] { new CreateOrderItemInput(variantId, 1, null, null, null) }, null));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.ModifierGroupRequired);
        result.Errors.Should().ContainKey("groupId").WhoseValue.Should().Contain(groupId.ToString());

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        (await verifyDb.Orders.CountAsync(o => o.SessionId == sessionId)).Should().Be(0);
    }

    /// <summary>Cenário Gherkin "Preço aplicado por canal" (US-030 §4): DELIVERY usa o próprio preço, não o de DINE_IN.</summary>
    [Fact]
    public async Task Pedido_No_Canal_Delivery_Aplica_O_Preco_Proprio_Do_Canal()
    {
        var world = await SeedWorldAsync();
        var (variantId, _) = await SeedProductAsync(world.TenantId, "Pizza Portuguesa", "Grande", unitPrice: 45m);

        await using (var priceDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId)))
        {
            priceDb.Prices.Add(Price.Create(world.TenantId, variantId, Channel.Delivery, 52.00m));
            await priceDb.SaveChangesAsync();
        }

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new CreateOrderCommand(
            "Delivery", SessionId: null, new[] { new CreateOrderItemInput(variantId, 1, null, null, null) }, null));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Order.Items.Single().UnitPrice.Should().Be(52.00m);
        result.Value.Order.Total.Should().Be(52.00m);
        result.Value.Order.Status.Should().Be("PLACED");
    }

    /// <summary>Cenário Gherkin "Reenvio por instabilidade de rede" (US-030 §4) — via IdempotencyMiddleware/IdempotencyStore REAIS (mesmo padrão de TableSessionIdempotencyIntegrationTests).</summary>
    [Fact]
    public async Task Reenvio_Com_A_Mesma_Idempotency_Key_Retorna_O_Mesmo_Pedido_Sem_Duplicar()
    {
        var world = await SeedWorldAsync();
        var (variantId, _) = await SeedProductAsync(world.TenantId, "Água", "500ml", unitPrice: 6m);
        var sessionId = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();
        var idempotencyStore = new IdempotencyStore(db);

        var idempotencyKey = Guid.NewGuid().ToString("N");
        var executions = 0;

        var middleware = new IdempotencyMiddleware(async ctx =>
        {
            executions++;
            var result = await sender.Send(new CreateOrderCommand(
                "DineIn", sessionId, new[] { new CreateOrderItemInput(variantId, 1, null, null, null) }, null));
            ctx.Response.StatusCode = result.IsSuccess ? StatusCodes.Status201Created : StatusCodes.Status422UnprocessableEntity;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(result.IsSuccess ? (object)result.Value! : new { code = result.Code }));
        });

        var body = """{"channel":"DineIn","sessionId":null,"items":[]}""";

        var first = CreateContext(idempotencyKey, body);
        await middleware.InvokeAsync(first, idempotencyStore, tenantContext);

        var second = CreateContext(idempotencyKey, body);
        await middleware.InvokeAsync(second, idempotencyStore, tenantContext);

        executions.Should().Be(1, "o reenvio nunca deve chegar ao handler de negócio de novo");
        first.Response.StatusCode.Should().Be(StatusCodes.Status201Created);
        second.Response.StatusCode.Should().Be(StatusCodes.Status201Created);
        second.Response.Headers["Idempotent-Replay"].ToString().Should().Be("true");

        var firstBody = await ReadBodyAsync(first);
        var secondBody = await ReadBodyAsync(second);
        var firstOrder = JsonSerializer.Deserialize<CreateOrderResponse>(firstBody);
        var secondOrder = JsonSerializer.Deserialize<CreateOrderResponse>(secondBody);
        secondOrder.Should().BeEquivalentTo(firstOrder, "a segunda chamada devolve o MESMO pedido, sem reexecutar");

        await using var verifyDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));
        (await verifyDb.Orders.CountAsync(o => o.SessionId == sessionId)).Should().Be(1, "o duplo toque nunca deveria ter criado um segundo pedido");
    }

    private static DefaultHttpContext CreateContext(string idempotencyKey, string body)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider(),
        };
        context.Request.Method = "POST";
        context.Request.Path = "/v1/orders";
        context.Request.Headers["Idempotency-Key"] = idempotencyKey;
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Request.ContentLength = context.Request.Body.Length;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    private sealed record World(Guid TenantId, Guid StoreId, Guid AreaId);

    private async Task<World> SeedWorldAsync()
    {
        var tenantId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        await using (var db = _fixture.CreateAppDbContext(tenantContext: null))
        {
            db.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Tenant de teste"));
            await db.SaveChangesAsync();
        }

        await using var storeDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var store = Domain.Platform.Store.Create(storeId, tenantId, "Loja de teste", isDefault: true);
        storeDb.Stores.Add(store);
        var area = Area.Create(tenantId, storeId, "Salão de teste");
        storeDb.Areas.Add(area);
        await storeDb.SaveChangesAsync();

        return new World(tenantId, storeId, area.Id);
    }

    private async Task<(Guid VariantId, Guid ProductId)> SeedProductAsync(Guid tenantId, string productName, string variantName, decimal unitPrice)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));

        var category = Category.Create(tenantId, "Categoria de teste");
        db.Categories.Add(category);

        var product = Product.Create(tenantId, category.Id, productName);
        db.Products.Add(product);

        var variant = ProductVariant.Create(tenantId, product.Id, variantName, prepMinutes: 10);
        db.ProductVariants.Add(variant);

        var price = Price.Create(tenantId, variant.Id, Channel.DineIn, unitPrice);
        db.Prices.Add(price);

        await db.SaveChangesAsync();

        return (variant.Id, product.Id);
    }

    private async Task MarkProductUnavailableAsync(Guid tenantId, Guid productId)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var product = await db.Products.SingleAsync(p => p.Id == productId);
        product.MarkUnavailable("Acabou o insumo");
        await db.SaveChangesAsync();
    }

    private async Task<Guid> LinkRequiredModifierGroupAsync(Guid tenantId, Guid productId, string groupName)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var group = ModifierGroup.Create(tenantId, groupName, minSelect: 1, maxSelect: 1, isRequired: true);
        db.ModifierGroups.Add(group);
        db.ProductModifierGroups.Add(ProductModifierGroup.Create(tenantId, productId, group.Id));
        await db.SaveChangesAsync();
        return group.Id;
    }

    private async Task<Guid> OpenSessionAsync(World world, string tableLabel = "1", string qrToken = "qr-mesa-1")
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId));

        var uniqueLabel = $"{tableLabel}-{Guid.NewGuid():N}"[..12];
        var table = DiningTable.Create(world.TenantId, world.StoreId, world.AreaId, uniqueLabel, $"{qrToken}-{Guid.NewGuid():N}", seats: 4);
        db.DiningTables.Add(table);
        table.Occupy();

        var session = TableSession.Create(
            world.TenantId, world.StoreId, table.Id, DateOnly.FromDateTime(DateTime.UtcNow), guestCount: 2, openedSource: "WAITER");
        db.TableSessions.Add(session);

        await db.SaveChangesAsync();

        return session.Id;
    }
}
