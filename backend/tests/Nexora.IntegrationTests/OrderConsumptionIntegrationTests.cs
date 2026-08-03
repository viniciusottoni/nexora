using Nexora.Application.Abstractions.Behaviors;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Orders.Commands.AddOrderItem;
using Nexora.Application.Orders.Commands.AdvanceOrderItemStatus;
using Nexora.Application.Orders.Commands.RepeatOrderItem;
using Nexora.Application.Orders.Queries.GetCurrentSessionConsumption;
using Nexora.Domain.Catalog;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.Infrastructure.Devices;
using Nexora.Infrastructure.Persistence;
using Nexora.IntegrationTests.Fakes;
using Nexora.IntegrationTests.Fixtures;
using Nexora.Shared.Errors;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Nexora.IntegrationTests;

/// <summary>
/// Cenários Gherkin da US-024 (Consumo da mesa em tempo real) e US-028 (Repetir item com um toque)
/// contra um PostgreSQL real (Testcontainers) — mesmo pipeline MediatR de produção (ADR-037).
/// Cobre a capacidade mínima de lançamento de item (<c>AddOrderItemCommand</c>, gap de US-030 —
/// ver docstring do handler) usada aqui só para gerar dados de consumo reais.
/// </summary>
[Collection("Postgres")]
public sealed class OrderConsumptionIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public OrderConsumptionIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Cenário Gherkin "Visualização do consumo" (US-024 §4): total bate com a soma de itens (com modificador) + taxa de serviço.</summary>
    [Fact]
    public async Task Total_Do_Consumo_Bate_Com_Soma_Dos_Itens_Mais_Modificador_Mais_Taxa()
    {
        var world = await SeedWorldAsync();
        var (variantId, modifierId) = await SeedProductWithModifierAsync(world.TenantId, "Pizza Calabresa", "Broto", unitPrice: 40m, modifierPriceDelta: 5m);
        await SeedTenantServiceFeeAsync(world.TenantId, percent: 10m);

        var sessionId = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        var broadcaster = new RecordingOrderConsumptionBroadcaster();
        await using var provider = BuildContainer(db, tenantContext, broadcaster);
        var sender = provider.GetRequiredService<ISender>();

        var added = await sender.Send(new AddOrderItemCommand(
            sessionId, variantId, Quantity: 2, Notes: null,
            Modifiers: new[] { new AddOrderItemModifierInput(modifierId, 1) },
            Fractions: null));

        added.IsSuccess.Should().BeTrue();
        // unitário 40, qtd 2 => 80 + modificador 5 (qty 1) => 85
        added.Value!.TotalPrice.Should().Be(85m);

        var consumerContext = new StaticTenantContext(world.TenantId, world.StoreId, sessionId: sessionId);
        await using var consumerDb = _fixture.CreateAppDbContext(consumerContext);
        await using var consumerProvider = BuildContainer(consumerDb, consumerContext, new RecordingOrderConsumptionBroadcaster());
        var consumption = await consumerProvider.GetRequiredService<ISender>().Send(new GetCurrentSessionConsumptionQuery());

        consumption.IsSuccess.Should().BeTrue();
        consumption.Value!.Items.Should().ContainSingle();
        consumption.Value.Subtotal.Should().Be(85m);
        consumption.Value.ServiceFee.Should().Be(8.5m, "10% de 85");
        consumption.Value.ServiceFeeOptional.Should().BeTrue();
        consumption.Value.Total.Should().Be(93.5m);
    }

    /// <summary>Cenário Gherkin "Item cancelado" (US-024 §4): aparece na lista mas não compõe subtotal/total.</summary>
    [Fact]
    public async Task Item_Cancelado_Nao_Compoe_O_Total_Mas_Continua_Visivel()
    {
        var world = await SeedWorldAsync();
        var (variantId, _) = await SeedProductWithModifierAsync(world.TenantId, "Refrigerante", "Lata", unitPrice: 8m);
        var sessionId = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext, new RecordingOrderConsumptionBroadcaster());
        var sender = provider.GetRequiredService<ISender>();

        var kept = await sender.Send(new AddOrderItemCommand(sessionId, variantId, 1, null, null, null));
        var cancelled = await sender.Send(new AddOrderItemCommand(sessionId, variantId, 1, null, null, null));
        kept.IsSuccess.Should().BeTrue();
        cancelled.IsSuccess.Should().BeTrue();

        await using (var cancelDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId)))
        {
            var item = await cancelDb.OrderItems.SingleAsync(i => i.Id == cancelled.Value!.Id);
            item.Cancel("Cliente desistiu", Guid.NewGuid());
            await cancelDb.SaveChangesAsync();
        }

        var consumerContext = new StaticTenantContext(world.TenantId, world.StoreId, sessionId: sessionId);
        await using var consumerDb = _fixture.CreateAppDbContext(consumerContext);
        await using var consumerProvider = BuildContainer(consumerDb, consumerContext, new RecordingOrderConsumptionBroadcaster());
        var consumption = await consumerProvider.GetRequiredService<ISender>().Send(new GetCurrentSessionConsumptionQuery());

        consumption.IsSuccess.Should().BeTrue();
        consumption.Value!.Items.Should().HaveCount(2, "o item cancelado continua visível (riscado no frontend)");
        consumption.Value.Items.Should().ContainSingle(i => i.OrderItemId == cancelled.Value!.Id && i.Cancelled);
        consumption.Value.Subtotal.Should().Be(8m, "só o item não cancelado compõe o subtotal");
        consumption.Value.Total.Should().Be(consumption.Value.Subtotal + consumption.Value.ServiceFee);
    }

    /// <summary>Cenário Gherkin "Item indisponível" (US-028 §4): repetição bloqueada com 422 PRODUCT_UNAVAILABLE.</summary>
    [Fact]
    public async Task Repetir_Item_De_Produto_Indisponivel_E_Bloqueado()
    {
        var world = await SeedWorldAsync();
        var (variantId, _) = await SeedProductWithModifierAsync(world.TenantId, "Pizza Frango", "Grande", unitPrice: 45m);
        var sessionId = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext, new RecordingOrderConsumptionBroadcaster());
        var sender = provider.GetRequiredService<ISender>();

        var added = await sender.Send(new AddOrderItemCommand(sessionId, variantId, 1, null, null, null));
        added.IsSuccess.Should().BeTrue();

        await using (var unavailableDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId)))
        {
            var product = await unavailableDb.Products.SingleAsync(p => p.Id != Guid.Empty && p.TenantId == world.TenantId);
            product.MarkUnavailable("Acabou o frango");
            await unavailableDb.SaveChangesAsync();
        }

        // Novo DbContext/provider para a repetição — o "db"/"sender" acima já tem o Product
        // rastreado (identity map) com o valor ANTIGO de IsAvailable, lido pelo próprio
        // AddOrderItemCommand; reconsultar com a MESMA instância devolveria o objeto em cache, sem
        // refletir a indisponibilidade gravada por outro contexto (mesmo cuidado dos demais testes
        // desta suíte que fazem uma escrita "fora de banda" antes de reagir a ela).
        await using var repeatDb = _fixture.CreateAppDbContext(tenantContext);
        await using var repeatProvider = BuildContainer(repeatDb, tenantContext, new RecordingOrderConsumptionBroadcaster());
        var repeatResult = await repeatProvider.GetRequiredService<ISender>()
            .Send(new RepeatOrderItemCommand(added.Value!.OrderId, added.Value.Id));

        repeatResult.IsSuccess.Should().BeFalse();
        repeatResult.Code.Should().Be(ApiErrorCodes.ProductUnavailable);
    }

    /// <summary>Cenário Gherkin "Preço atualizado" (US-028 §4): usa o preço VIGENTE, não o do item original.</summary>
    [Fact]
    public async Task Repetir_Item_Usa_Preco_Vigente_Nao_O_Preco_Original()
    {
        var world = await SeedWorldAsync();
        var (variantId, _) = await SeedProductWithModifierAsync(world.TenantId, "Cerveja", "600ml", unitPrice: 18m);
        var sessionId = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext, new RecordingOrderConsumptionBroadcaster());
        var sender = provider.GetRequiredService<ISender>();

        var added = await sender.Send(new AddOrderItemCommand(sessionId, variantId, 1, null, null, null));
        added.Value!.UnitPrice.Should().Be(18m);

        // Reajuste de preço (fecha o antigo, abre um novo vigente) — mesmo padrão de Price.Close/Create.
        await using (var priceDb = _fixture.CreateAppDbContext(new StaticTenantContext(world.TenantId, world.StoreId)))
        {
            var oldPrice = await priceDb.Prices.SingleAsync(p => p.VariantId == variantId && p.ValidTo == null);
            oldPrice.Close(DateTimeOffset.UtcNow);
            priceDb.Prices.Add(Price.Create(world.TenantId, variantId, Channel.DineIn, amount: 22m));
            await priceDb.SaveChangesAsync();
        }

        var repeated = await sender.Send(new RepeatOrderItemCommand(added.Value.OrderId, added.Value.Id));

        repeated.IsSuccess.Should().BeTrue();
        repeated.Value!.Item.UnitPrice.Should().Be(22m, "preço vigente no momento da repetição, não os 18 do item original");
        repeated.Value.Item.RepeatedFromItemId.Should().Be(added.Value.Id);
    }

    /// <summary>Cenário Gherkin "Repetição de item composto" (US-028 §4): frações, modificadores e observações copiados fielmente (round-trip via banco).</summary>
    [Fact]
    public async Task Repetir_Item_Composto_Copia_Fracoes_Modificadores_E_Observacoes()
    {
        var world = await SeedWorldAsync();
        var (variantId, modifierId) = await SeedProductWithModifierAsync(world.TenantId, "Pizza Meio a Meio", "Grande", unitPrice: 50m, modifierPriceDelta: 3m);
        var (fractionVariantId1, _) = await SeedProductWithModifierAsync(world.TenantId, "Pizza Portuguesa", "Grande", unitPrice: 52m, allowsFractions: true, fractionGroup: "PIZZA");
        var (fractionVariantId2, _) = await SeedProductWithModifierAsync(world.TenantId, "Pizza Calabresa", "Grande", unitPrice: 48m, allowsFractions: true, fractionGroup: "PIZZA");
        var sessionId = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext, new RecordingOrderConsumptionBroadcaster());
        var sender = provider.GetRequiredService<ISender>();

        // Meio a meio (ck_...): a soma dos pesos das frações do item precisa ser exatamente 1.0.
        var added = await sender.Send(new AddOrderItemCommand(
            sessionId, variantId, 1, "Borda recheada, por favor",
            Modifiers: new[] { new AddOrderItemModifierInput(modifierId, 1) },
            Fractions: new[]
            {
                new AddOrderItemFractionInput(fractionVariantId1, 0.5m),
                new AddOrderItemFractionInput(fractionVariantId2, 0.5m),
            }));
        added.IsSuccess.Should().BeTrue();

        var repeated = await sender.Send(new RepeatOrderItemCommand(added.Value!.OrderId, added.Value.Id));

        repeated.IsSuccess.Should().BeTrue();
        repeated.Value!.Item.Notes.Should().Be("Borda recheada, por favor");
        repeated.Value.Item.Modifiers.Should().ContainSingle(m => m.ModifierId == modifierId);
        repeated.Value.Item.Fractions.Should().HaveCount(2);
        repeated.Value.Item.Fractions.Should().ContainSingle(f => f.VariantId == fractionVariantId1 && f.Weight == 0.5m);
        repeated.Value.Item.Fractions.Should().ContainSingle(f => f.VariantId == fractionVariantId2 && f.Weight == 0.5m);
    }

    /// <summary>Segurança (RN-015/ADR-021): token de uma sessão não repete item de OUTRA sessão — 404, nunca 403.</summary>
    [Fact]
    public async Task Repetir_Item_De_Outra_Sessao_Retorna_Nao_Encontrado_Nunca_Autorizacao_Negada()
    {
        var world = await SeedWorldAsync();
        var (variantId, _) = await SeedProductWithModifierAsync(world.TenantId, "Suco", "300ml", unitPrice: 9m);
        var sessionA = await OpenSessionAsync(world, tableLabel: "A1", qrToken: "qr-mesa-a1");
        var sessionB = await OpenSessionAsync(world, tableLabel: "B2", qrToken: "qr-mesa-b2");

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db, tenantContext, new RecordingOrderConsumptionBroadcaster());
        var sender = provider.GetRequiredService<ISender>();

        var addedToSessionA = await sender.Send(new AddOrderItemCommand(sessionA, variantId, 1, null, null, null));
        addedToSessionA.IsSuccess.Should().BeTrue();

        // O "cliente" da mesa B tenta repetir um item que pertence ao pedido da mesa A —
        // RequestingSessionId é resolvido pelo controller a partir da claim "ses" do token da
        // mesa B, nunca de um valor que o cliente escolhe.
        var result = await sender.Send(new RepeatOrderItemCommand(addedToSessionA.Value!.OrderId, addedToSessionA.Value.Id, RequestingSessionId: sessionB));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.OrderNotFound, "nunca revela que o pedido existe em outra mesa — mesmo código de \"não encontrado\"");
    }

    /// <summary>Segurança (RN-015): a consulta pública de consumo só enxerga a PRÓPRIA sessão (claim "ses" do token corrente).</summary>
    [Fact]
    public async Task Consumo_Publico_So_Enxerga_A_Propria_Sessao()
    {
        var world = await SeedWorldAsync();
        var (variantId, _) = await SeedProductWithModifierAsync(world.TenantId, "Água", "500ml", unitPrice: 6m);
        var sessionA = await OpenSessionAsync(world, tableLabel: "C1", qrToken: "qr-mesa-c1");
        var sessionB = await OpenSessionAsync(world, tableLabel: "D2", qrToken: "qr-mesa-d2");

        var writerContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var writerDb = _fixture.CreateAppDbContext(writerContext);
        await using var writerProvider = BuildContainer(writerDb, writerContext, new RecordingOrderConsumptionBroadcaster());
        await writerProvider.GetRequiredService<ISender>().Send(new AddOrderItemCommand(sessionA, variantId, 1, null, null, null));

        var contextB = new StaticTenantContext(world.TenantId, world.StoreId, sessionId: sessionB);
        await using var dbB = _fixture.CreateAppDbContext(contextB);
        await using var providerB = BuildContainer(dbB, contextB, new RecordingOrderConsumptionBroadcaster());

        var consumptionB = await providerB.GetRequiredService<ISender>().Send(new GetCurrentSessionConsumptionQuery());

        consumptionB.IsSuccess.Should().BeTrue();
        consumptionB.Value!.Items.Should().BeEmpty("a mesa D não lançou nada — nunca deve ver o item da mesa C");
    }

    /// <summary>US-024 §9 (Atualização automática): o avanço de status de item propaga via broadcaster de forma síncrona.</summary>
    [Fact]
    public async Task Avancar_Status_Do_Item_Propaga_Via_Broadcaster_De_Forma_Sincrona()
    {
        var world = await SeedWorldAsync();
        var (variantId, _) = await SeedProductWithModifierAsync(world.TenantId, "Pizza Quatro Queijos", "Grande", unitPrice: 48m);
        var sessionId = await OpenSessionAsync(world);

        var tenantContext = new StaticTenantContext(world.TenantId, world.StoreId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        var broadcaster = new RecordingOrderConsumptionBroadcaster();
        await using var provider = BuildContainer(db, tenantContext, broadcaster);
        var sender = provider.GetRequiredService<ISender>();

        var added = await sender.Send(new AddOrderItemCommand(sessionId, variantId, 1, null, null, null));

        broadcaster.ItemAddedCalls.Should().ContainSingle(call => call.OrderItemId == added.Value!.Id);

        var advanced = await sender.Send(new AdvanceOrderItemStatusCommand(added.Value!.OrderId, added.Value.Id));

        advanced.IsSuccess.Should().BeTrue();
        advanced.Value!.Status.Should().Be("FIRED");
        broadcaster.ItemStatusChangedCalls.Should().ContainSingle(call => call.OrderItemId == added.Value.Id && call.Status == "FIRED");
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

    private async Task<(Guid VariantId, Guid ModifierId)> SeedProductWithModifierAsync(
        Guid tenantId, string productName, string variantName, decimal unitPrice, decimal? modifierPriceDelta = null,
        bool allowsFractions = false, string? fractionGroup = null)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));

        var category = Category.Create(tenantId, "Categoria de teste");
        db.Categories.Add(category);

        // US-030/US-013: fração só é aceita quando o produto permite fracionamento e compartilha o
        // MESMO grupo de fração das demais variantes combinadas — sem isto, o item meio a meio
        // seria recusado com 422 FRACTION_NOT_ALLOWED/FRACTION_GROUP_MISMATCH pela validação real
        // de OrderItemFractionPricing (antes desta wave, o mock de AddOrderItemCommandHandler não
        // validava isso — ver relatório da US-030).
        var product = Product.Create(tenantId, category.Id, productName, allowsFractions: allowsFractions, maxFractions: allowsFractions ? (short)2 : (short)1);
        if (allowsFractions && fractionGroup is not null)
        {
            product.SetFractionGroup(fractionGroup);
        }

        db.Products.Add(product);

        var variant = ProductVariant.Create(tenantId, product.Id, variantName);
        db.ProductVariants.Add(variant);

        var price = Price.Create(tenantId, variant.Id, Channel.DineIn, unitPrice);
        db.Prices.Add(price);

        var modifierGroup = ModifierGroup.Create(tenantId, "Adicionais de teste");
        db.ModifierGroups.Add(modifierGroup);
        var modifier = Modifier.Create(tenantId, modifierGroup.Id, "Modificador de teste", modifierPriceDelta ?? 0m);
        db.Modifiers.Add(modifier);

        await db.SaveChangesAsync();

        return (variant.Id, modifier.Id);
    }

    private async Task SeedTenantServiceFeeAsync(Guid tenantId, decimal percent)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var config = await db.TenantConfigs.SingleOrDefaultAsync(c => c.TenantId == tenantId);
        if (config is null)
        {
            config = TenantConfig.Create(tenantId);
            db.TenantConfigs.Add(config);
        }

        config.UpdateOperation($"{{\"serviceFeePercent\": {percent.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}");
        await db.SaveChangesAsync();
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

    private static ServiceProvider BuildContainer(
        IApplicationDbContext db, ICurrentTenantContext tenantContext, IOrderConsumptionBroadcaster broadcaster)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(db);
        services.AddSingleton(tenantContext);
        services.AddSingleton<IEventOriginProvider, EdgeEventOriginProvider>();
        services.AddSingleton(broadcaster);
        // AddOrderItemCommandHandler (US-026 §4 "Novo pedido após solicitar a conta") também
        // depende de IAlertsBroadcaster — nenhum teste desta classe inspeciona essas chamadas
        // (isso é coberto por BillRequestIntegrationTests), então um duplo simples basta aqui.
        services.AddSingleton<IAlertsBroadcaster>(new RecordingAlertsBroadcaster());
        // US-031: AddOrderItemCommand/AdvanceOrderItemStatusCommand também dependem de IStationBroadcaster
        // — nenhum teste desta classe inspeciona essas chamadas (isso é coberto por
        // KdsRoutingIntegrationTests), então um duplo simples basta aqui.
        services.AddSingleton<IStationBroadcaster>(new RecordingStationBroadcaster());
        if (db is AppDbContext appDbContext)
        {
            services.AddSingleton<IOrderShortCodeAllocator>(new OrderShortCodeAllocator(appDbContext));
        }

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ICommand).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });
        services.AddValidatorsFromAssembly(typeof(ICommand).Assembly);

        return services.BuildServiceProvider();
    }
}
